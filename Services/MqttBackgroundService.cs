using System.Text.Json;
using BsonData;
using MQTT;
using DATN.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DATN.Services
{
    public class MqttBackgroundService : IHostedService
    {
        private readonly ILogger<MqttBackgroundService> _logger;
        private readonly Client _mqttClient;
        private readonly Database _database;
        private readonly FallDetectionService _fallDetectionService;
        private readonly NotificationService _notificationService;

        public MqttBackgroundService(
            ILogger<MqttBackgroundService> logger, 
            Database database,
            FallDetectionService fallDetectionService,
            NotificationService notificationService)
        {
            _logger = logger;
            _database = database;
            _fallDetectionService = fallDetectionService;
            _notificationService = notificationService;

            _mqttClient = new Client("backend-worker", "broker.emqx.io", 1883);
            _mqttClient.DataReceived += MqttClient_DataReceived;
        }

        private void MqttClient_DataReceived(string topic, byte[] message)
        {
            var payload = System.Text.Encoding.UTF8.GetString(message);
            _logger.LogInformation($"[MQTT] Message received on topic '{topic}': {payload}");

            try
            {
                if (topic.StartsWith("sensor/data"))
                {
                    HandleSensorData(payload);
                }
                else if (topic.StartsWith("sensor/alert"))
                {
                    HandleDirectAlert(payload);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT] Failed to process message");
            }
        }

        /// <summary>
        /// Data Ingestion Service: validates, normalizes, stores raw data,
        /// then forwards to Fall Detection for analysis.
        /// </summary>
        private void HandleSensorData(string payload)
        {
            var sensorData = JsonSerializer.Deserialize<SensorData>(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (sensorData == null || string.IsNullOrEmpty(sensorData.DeviceId))
            {
                _logger.LogWarning("[MQTT] Invalid sensor data payload - rejected");
                return;
            }

            // Step 1: Store raw sensor data
            var sensorCol = _database.GetCollection("SensorData");
            sensorCol.Insert(new Document
            {
                ["DeviceId"] = sensorData.DeviceId,
                ["Timestamp"] = sensorData.Timestamp == default ? DateTime.UtcNow : sensorData.Timestamp,
                ["AccelX"] = sensorData.AccelX,
                ["AccelY"] = sensorData.AccelY,
                ["AccelZ"] = sensorData.AccelZ,
                ["GyroX"] = sensorData.GyroX,
                ["GyroY"] = sensorData.GyroY,
                ["GyroZ"] = sensorData.GyroZ,
                ["HeartRate"] = sensorData.HeartRate
            });

            // Step 2: Update device last connected time
            UpdateDeviceLastConnected(sensorData.DeviceId);

            // Step 3: Forward to Fall Detection Service for analysis
            var result = _fallDetectionService.ProcessSensorFrame(sensorData);

            if (result.IsFallDetected)
            {
                _logger.LogWarning($"[FALL DETECTED] Device {result.DeviceId} - Confidence: {result.Confidence:F1}%");

                // Save fall event (with userId for data consistency)
                var userId = FindUserByDeviceId(result.DeviceId);
                var fallEventId = _fallDetectionService.SaveFallEvent(result, userId);

                if (!string.IsNullOrEmpty(userId))
                {
                    Task.Run(async () =>
                    {
                        // Step 1: Immediately notify user via SignalR (no delay)
                        await _notificationService.PushFallAlertAsync(userId, fallEventId, result.DeviceId, result.DetectedAt);

                        // Step 2: Notify Emergency Contacts per Plan §4.5
                        await _notificationService.NotifyEmergencyContactsAsync(userId, fallEventId, result.DeviceId, result.DetectedAt);

                        // Step 3: Check Auto Emergency setting
                        var autoEmergency = CheckAutoEmergencySetting(userId);
                        if (!autoEmergency) return;

                        // Step 4: Read CountdownSeconds from user DB and wait (Plan §3.1, §4.2)
                        var countdownSeconds = GetCountdownSeconds(userId);
                        _logger.LogWarning($"[AUTO EMERGENCY] Countdown: {countdownSeconds}s before dispatch for user {userId}");
                        await Task.Delay(TimeSpan.FromSeconds(countdownSeconds));

                        // Step 5: Auto-create ambulance request after countdown
                        var ambulanceCol = _database.GetCollection("AmbulanceRequests");
                        var doc = new Document
                        {
                            ["FallEventId"] = fallEventId,
                            ["UserId"] = userId,
                            ["RequestTime"] = DateTime.UtcNow,
                            ["Location"] = "",
                            ["Status"] = "Pending",
                            ["DispatchNotes"] = $"Auto-generated after {countdownSeconds}s countdown"
                        };
                        ambulanceCol.Insert(doc);

                        // Step 6: Notify Hospital
                        await _notificationService.NotifyHospitalAsync(doc.ObjectId, userId, "");
                        _logger.LogWarning($"[AUTO EMERGENCY] Ambulance request created for user {userId} after {countdownSeconds}s countdown");
                    });
                }
            }
        }

        /// <summary>
        /// Handle direct alert from device (device-side fall detection).
        /// </summary>
        private void HandleDirectAlert(string payload)
        {
            var fallEvent = JsonSerializer.Deserialize<FallEvent>(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (fallEvent != null)
            {
                var fallCol = _database.GetCollection("FallEvents");
                fallCol.Insert(new Document
                {
                    ["DeviceId"] = fallEvent.DeviceId,
                    ["DetectedAt"] = fallEvent.DetectedAt == default ? DateTime.UtcNow : fallEvent.DetectedAt,
                    ["Latitude"] = fallEvent.Latitude,
                    ["Longitude"] = fallEvent.Longitude,
                    ["Status"] = fallEvent.Status ?? "Detected",
                    ["FalseAlarm"] = false,
                    ["Source"] = "Device"
                });

                _logger.LogWarning($"[ALERT] Direct fall alert from Device {fallEvent.DeviceId}!");

                var userId = FindUserByDeviceId(fallEvent.DeviceId);
                if (!string.IsNullOrEmpty(userId))
                {
                    Task.Run(async () =>
                    {
                        await _notificationService.PushFallAlertAsync(userId, "", fallEvent.DeviceId, DateTime.UtcNow);
                    });
                }
            }
        }

        private void UpdateDeviceLastConnected(string deviceId)
        {
            try
            {
                var deviceCol = _database.GetCollection("Devices");
                var device = deviceCol.Select().FirstOrDefault(d => d.SelectPath("DeviceId")?.ToString() == deviceId);
                if (device != null)
                {
                    device["LastConnectedAt"] = DateTime.UtcNow;
                    deviceCol.Update(device.ObjectId, device);
                }
            }
            catch { /* Non-critical */ }
        }

        private string? FindUserByDeviceId(string deviceId)
        {
            var deviceCol = _database.GetCollection("Devices");
            var device = deviceCol.Select().FirstOrDefault(d => d.SelectPath("DeviceId")?.ToString() == deviceId);
            return device?.SelectPath("UserId")?.ToString();
        }

        private bool CheckAutoEmergencySetting(string userId)
        {
            var userCol = _database.GetCollection("Users");
            var user = userCol.Select().FirstOrDefault(u => u.ObjectId == userId);
            return (bool?)user?.SelectPath("AutoEmergencyEnabled") ?? true;
        }

        private int GetCountdownSeconds(string userId)
        {
            var userCol = _database.GetCollection("Users");
            var user = userCol.Select().FirstOrDefault(u => u.ObjectId == userId);
            var val = user?.SelectPath("CountdownSeconds");
            if (val is int i) return i > 0 ? i : 30;
            if (val != null && int.TryParse(val.ToString(), out int parsed)) return parsed > 0 ? parsed : 30;
            return 30; // default 30 seconds
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting MQTT Background Service...");
            Task.Run(() =>
            {
                try
                {
                    _mqttClient.Connect();
                    _mqttClient.Subscribe("sensor/#");
                    _mqttClient.Ping(60); 
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to MQTT broker on start.");
                }

            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping MQTT Background Service...");
            _database.Disconnect();
            return Task.CompletedTask;
        }
    }
}