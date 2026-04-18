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

        public MqttBackgroundService(ILogger<MqttBackgroundService> logger)
        {
            _logger = logger;
            // Create a pseudo database from Infrastructure
            _database = new Database("FallDetectionDB");
            _database.Connect("DataStore");

            // TODO: Ensure you configure the real MQTT broker endpoint!
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
                    // For example: save raw data
                    // var sensorCol = _database.GetCollection("SensorData");
                    // sensorCol.Add(payload);
                }
                else if (topic.StartsWith("sensor/alert"))
                {
                    // Detect fall event payload
                    var fallEvent = JsonSerializer.Deserialize<FallEvent>(payload);
                    if (fallEvent != null)
                    {
                        var fallCol = _database.GetCollection("FallEvents");
                        fallCol.Insert(new Document
                        {
                            ["DeviceId"] = fallEvent.DeviceId,
                            ["DetectedAt"] = fallEvent.DetectedAt,
                            ["Latitude"] = fallEvent.Latitude,
                            ["Longitude"] = fallEvent.Longitude,
                            ["Status"] = fallEvent.Status
                        });

                        _logger.LogWarning($"[ALERT] Fall Detected for Device {fallEvent.DeviceId}!");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT] Failed to process message");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting MQTT Background Service...");
            Task.Run(() =>
            {
                // Connect to broker
                try
                {
                    // Subscribe to base topics
                    _mqttClient.Subscribe("sensor/#");
                    
                    // Maintain connection
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
             //_mqttClient.Disconnect(); // If supported
            _database.Disconnect();
            return Task.CompletedTask;
        }
    }
}