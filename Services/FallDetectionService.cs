using BsonData;
using DATN.Models;
using Microsoft.Extensions.Logging;

namespace DATN.Services
{
    /// <summary>
    /// Core Fall Detection Algorithm Service.
    /// Analyzes motion patterns from sensor data frames to detect falls.
    /// Uses threshold-based approach: checks for sudden acceleration spikes
    /// followed by a period of stillness (typical fall pattern).
    /// </summary>
    public class FallDetectionService
    {
        private readonly ILogger<FallDetectionService> _logger;
        private readonly Database _database;
        
        // Threshold configuration for fall detection algorithm
        private const double ACCEL_MAGNITUDE_THRESHOLD = 2.5;  // g-force spike threshold
        private const double GYRO_MAGNITUDE_THRESHOLD = 250.0; // deg/s rotation spike
        private const double POST_FALL_STILLNESS = 0.3;        // Low motion after fall

        // Buffer: stores recent sensor frames per device for pattern analysis
        private readonly Dictionary<string, List<SensorData>> _sensorBuffers = new();
        private const int BUFFER_SIZE = 20; // Number of frames to analyze

        public FallDetectionService(ILogger<FallDetectionService> logger, Database database)
        {
            _logger = logger;
            _database = database;
        }

        /// <summary>
        /// Process a single sensor data frame. Buffers data and runs 
        /// fall detection when enough frames are collected.
        /// </summary>
        public FallDetectionResult ProcessSensorFrame(SensorData frame)
        {
            if (string.IsNullOrEmpty(frame.DeviceId))
                return FallDetectionResult.NoFall();

            // Add frame to device buffer
            if (!_sensorBuffers.ContainsKey(frame.DeviceId))
                _sensorBuffers[frame.DeviceId] = new List<SensorData>();

            var buffer = _sensorBuffers[frame.DeviceId];
            buffer.Add(frame);

            // Keep buffer size manageable
            if (buffer.Count > BUFFER_SIZE)
                buffer.RemoveAt(0);

            // Need at least 10 frames to analyze
            if (buffer.Count < 10)
                return FallDetectionResult.NoFall();

            // Run fall detection algorithm
            return AnalyzeMotionPattern(frame.DeviceId, buffer);
        }

        /// <summary>
        /// Core motion analysis algorithm.
        /// Pattern: High acceleration spike -> High gyro rotation -> Stillness period
        /// This is the typical pattern observed during a fall event.
        /// </summary>
        private FallDetectionResult AnalyzeMotionPattern(string deviceId, List<SensorData> frames)
        {
            // Step 1: Extract motion features from recent frames
            var features = frames.Select(f => new
            {
                AccelMagnitude = Math.Sqrt(f.AccelX * f.AccelX + f.AccelY * f.AccelY + f.AccelZ * f.AccelZ),
                GyroMagnitude = Math.Sqrt(f.GyroX * f.GyroX + f.GyroY * f.GyroY + f.GyroZ * f.GyroZ),
                f.Timestamp
            }).ToList();

            // Step 2: Check for acceleration spike (sudden impact)
            double maxAccel = features.Max(f => f.AccelMagnitude);
            bool hasAccelSpike = maxAccel > ACCEL_MAGNITUDE_THRESHOLD;

            if (!hasAccelSpike)
                return FallDetectionResult.NoFall();

            // Step 3: Check for gyroscope rotation spike (body rotation during fall)
            double maxGyro = features.Max(f => f.GyroMagnitude);
            bool hasGyroSpike = maxGyro > GYRO_MAGNITUDE_THRESHOLD;

            // Step 4: Check for post-fall stillness (last 3 frames should be relatively still)
            var lastFrames = features.TakeLast(3).ToList();
            double avgRecentAccel = lastFrames.Average(f => f.AccelMagnitude);
            bool hasPostFallStillness = avgRecentAccel < (1.0 + POST_FALL_STILLNESS); // Near 1g (gravity)

            // Step 5: Decision - Fall detected if spike + (rotation OR stillness)
            bool isFallDetected = hasAccelSpike && (hasGyroSpike || hasPostFallStillness);

            if (isFallDetected)
            {
                _logger.LogWarning($"[FALL DETECTED] Device: {deviceId}, MaxAccel: {maxAccel:F2}g, MaxGyro: {maxGyro:F2}deg/s");
                
                // Clear buffer after detection to avoid repeated alerts
                _sensorBuffers[deviceId].Clear();

                return new FallDetectionResult
                {
                    IsFallDetected = true,
                    DeviceId = deviceId,
                    Confidence = CalculateConfidence(maxAccel, maxGyro, hasPostFallStillness),
                    MaxAcceleration = maxAccel,
                    MaxGyroscope = maxGyro,
                    DetectedAt = DateTime.UtcNow
                };
            }

            return FallDetectionResult.NoFall();
        }

        private double CalculateConfidence(double maxAccel, double maxGyro, bool hasStillness)
        {
            double score = 0;
            // Acceleration contribution (0-40%)
            score += Math.Min(40, (maxAccel / ACCEL_MAGNITUDE_THRESHOLD) * 20);
            // Gyroscope contribution (0-35%)
            score += Math.Min(35, (maxGyro / GYRO_MAGNITUDE_THRESHOLD) * 17.5);
            // Stillness contribution (0-25%)
            if (hasStillness) score += 25;
            return Math.Min(100, score);
        }

        /// <summary>
        /// Saves a detected fall event to the database.
        /// </summary>
        public string SaveFallEvent(FallDetectionResult result, string? userId = null, double? latitude = null, double? longitude = null)
        {
            var fallCol = _database.GetCollection("FallEvents");
            var doc = new Document
            {
                ["DeviceId"] = result.DeviceId,
                ["UserId"] = userId ?? "",
                ["DetectedAt"] = result.DetectedAt,
                ["Latitude"] = latitude,
                ["Longitude"] = longitude,
                ["Status"] = "Detected",
                ["FalseAlarm"] = false,
                ["Source"] = "Algorithm",
                ["Confidence"] = result.Confidence
            };

            fallCol.Insert(doc);
            _logger.LogInformation($"Fall event saved with ID: {doc.ObjectId}");
            return doc.ObjectId;
        }
    }

    public class FallDetectionResult
    {
        public bool IsFallDetected { get; set; }
        public string DeviceId { get; set; } = "";
        public double Confidence { get; set; }
        public double MaxAcceleration { get; set; }
        public double MaxGyroscope { get; set; }
        public DateTime DetectedAt { get; set; }

        public static FallDetectionResult NoFall() => new() { IsFallDetected = false };
    }
}