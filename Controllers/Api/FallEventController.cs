using Microsoft.AspNetCore.Mvc;
using BsonData;
using DATN.Models;

namespace DATN.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class FallEventController : ControllerBase
    {
        private readonly Database _database;
        private readonly Collection _fallCol;
        private readonly Collection _deviceCol;

        public FallEventController(Database database)
        {
            _database = database;
            _fallCol = _database.GetCollection("FallEvents");
            _deviceCol = _database.GetCollection("Devices");
        }

        [HttpGet("user/{userId}")]
        public IActionResult GetHistory(string userId)
        {
            var userDeviceIds = _deviceCol.Select()
                .Where(d => d.SelectPath("UserId")?.ToString() == userId)
                .Select(d => d.SelectPath("DeviceId")?.ToString())
                .Where(id => id != null)
                .ToList();

            var history = _fallCol.Select()
                                  .Where(f => f.SelectPath("UserId")?.ToString() == userId 
                                           || userDeviceIds.Contains(f.SelectPath("DeviceId")?.ToString()))
                                  .OrderByDescending(f => f.SelectPath("DetectedAt"))
                                  .Select(f => new FallEvent
                                  {
                                      Id = f.ObjectId,
                                      UserId = f.SelectPath("UserId")?.ToString(),
                                      DeviceId = f.SelectPath("DeviceId")?.ToString(),
                                      DetectedAt = (DateTime?)f.SelectPath("DetectedAt") ?? DateTime.UtcNow,
                                      Latitude = (double?)f.SelectPath("Latitude"),
                                      Longitude = (double?)f.SelectPath("Longitude"),
                                      Status = f.SelectPath("Status")?.ToString() ?? "Detected",
                                      FalseAlarm = (bool?)f.SelectPath("FalseAlarm") ?? false,
                                      Source = (string?)f.SelectPath("Source") ?? "Sensor"
                                  }).ToList();

            return Ok(history);
        }

        [HttpGet("{id}")]
        public IActionResult GetFallEventDetail(string id)
        {
            var doc = _fallCol.Select().FirstOrDefault(f => f.ObjectId == id);
            if (doc == null) return NotFound("Fall event not found.");

            var fallEvent = new FallEvent
            {
                Id = doc.ObjectId,
                UserId = doc.SelectPath("UserId")?.ToString(),
                DeviceId = doc.SelectPath("DeviceId")?.ToString(),
                DetectedAt = (DateTime?)doc.SelectPath("DetectedAt") ?? DateTime.UtcNow,
                Latitude = (double?)doc.SelectPath("Latitude"),
                Longitude = (double?)doc.SelectPath("Longitude"),
                Status = doc.SelectPath("Status")?.ToString() ?? "Detected",
                FalseAlarm = (bool?)doc.SelectPath("FalseAlarm") ?? false,
                Source = (string?)doc.SelectPath("Source") ?? "Sensor"
            };

            return Ok(fallEvent);
        }

        [HttpPut("{id}/false-alarm")]
        public IActionResult MarkFalseAlarm(string id)
        {
            var existing = _fallCol.Select().FirstOrDefault(f => f.ObjectId == id);
            if (existing == null) return NotFound("Fall event not found.");

            existing["FalseAlarm"] = true;
            existing["Status"] = "FalseAlarm";
            _fallCol.Update(id, existing);

            return Ok(new { success = true, message = "Marked as false alarm" });
        }

        /// <summary>
        /// MAUI app calls this after receiving a fall alert and getting device GPS.
        /// Updates the FallEvent with the real location from the phone's geolocation.
        /// </summary>
        [HttpPut("{id}/location")]
        public IActionResult UpdateLocation(string id, [FromBody] LocationUpdate request)
        {
            var existing = _fallCol.Select().FirstOrDefault(f => f.ObjectId == id);
            if (existing == null) return NotFound("Fall event not found.");

            existing["Latitude"] = request.Latitude;
            existing["Longitude"] = request.Longitude;
            _fallCol.Update(id, existing);

            return Ok(new { success = true });
        }
    }

    public class LocationUpdate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}