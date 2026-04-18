using Microsoft.AspNetCore.Mvc;
using BsonData;
using DATN.Models;

namespace DATN.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        private readonly Database _database;
        private readonly Collection _deviceCol;

        public DeviceController()
        {
            _database = new Database("FallDetectionDB");
            _database.Connect("DataStore");
            _deviceCol = _database.GetCollection("Devices");
        }

        [HttpPost("register")]
        public IActionResult RegisterDevice([FromBody] Device device)
        {
            if (string.IsNullOrEmpty(device.DeviceId) || string.IsNullOrEmpty(device.UserId))
                return BadRequest("DeviceId and UserId are required.");

            // Kiểm tra xem thiết bị đã đăng ký chưa
            var existing = _deviceCol.Select().FirstOrDefault(d => d.SelectPath("DeviceId")?.ToString() == device.DeviceId);
            if (existing != null)
                return BadRequest("Device already registered.");

            var doc = new Document
            {
                ["DeviceId"] = device.DeviceId,
                ["UserId"] = device.UserId,
                ["Name"] = device.Name ?? "My Device",
                ["IsActive"] = true,
                ["RegisteredAt"] = DateTime.UtcNow
            };

            _deviceCol.Insert(doc);
            return Ok(new { success = true, id = doc.ObjectId, message = "Device registered" });
        }
 
        [HttpGet("user/{userId}")]
        public IActionResult GetUserDevices(string userId)
        {
            var devices = _deviceCol.Select().Where(d => d.SelectPath("UserId")?.ToString() == userId)
                .Select(d => new {
                    Id = d.ObjectId,
                    DeviceId = d.SelectPath("DeviceId")?.ToString(),
                    Name = d.SelectPath("Name")?.ToString(),
                    IsActive = (bool?)d.SelectPath("IsActive") ?? false
                }).ToList();

            return Ok(devices);
        }
    }
}