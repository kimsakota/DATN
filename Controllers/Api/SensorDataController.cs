using Microsoft.AspNetCore.Mvc;
using BsonData;
using DATN.Models;

namespace DATN.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class SensorDataController : ControllerBase
    {
        private readonly Database _database;
        private readonly Collection _sensorCol;

        public SensorDataController(Database database)
        {
            _database = database;
            _sensorCol = _database.GetCollection("SensorData");
        }

        [HttpPost]
        public IActionResult StoreSensorData([FromBody] SensorData data)
        {
            if (string.IsNullOrEmpty(data.DeviceId))
                return BadRequest("DeviceId is required.");

            var doc = new Document
            {
                ["DeviceId"] = data.DeviceId,
                ["Timestamp"] = data.Timestamp == default ? DateTime.UtcNow : data.Timestamp,
                ["AccelX"] = data.AccelX,
                ["AccelY"] = data.AccelY,
                ["AccelZ"] = data.AccelZ,
                ["GyroX"] = data.GyroX,
                ["GyroY"] = data.GyroY,
                ["GyroZ"] = data.GyroZ,
                ["HeartRate"] = data.HeartRate
            };

            _sensorCol.Insert(doc);
            return Ok(new { success = true });
        }

        [HttpGet("device/{deviceId}/latest")]
        public IActionResult GetLatestData(string deviceId, [FromQuery] int count = 50)
        {
            var data = _sensorCol.Select()
                .Where(s => s.SelectPath("DeviceId")?.ToString() == deviceId)
                .OrderByDescending(s => s.SelectPath("Timestamp"))
                .Take(count)
                .Select(s => new SensorData
                {
                    Id = s.ObjectId,
                    DeviceId = s.SelectPath("DeviceId")?.ToString() ?? "",
                    Timestamp = (DateTime?)s.SelectPath("Timestamp") ?? DateTime.UtcNow,
                    AccelX = (double?)s.SelectPath("AccelX") ?? 0,
                    AccelY = (double?)s.SelectPath("AccelY") ?? 0,
                    AccelZ = (double?)s.SelectPath("AccelZ") ?? 0,
                    GyroX = (double?)s.SelectPath("GyroX") ?? 0,
                    GyroY = (double?)s.SelectPath("GyroY") ?? 0,
                    GyroZ = (double?)s.SelectPath("GyroZ") ?? 0,
                    HeartRate = (double?)s.SelectPath("HeartRate") ?? 0
                }).ToList();

            return Ok(data);
        }
    }
}