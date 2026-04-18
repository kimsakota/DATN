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

        public FallEventController()
        {
            _database = new Database("FallDetectionDB");
            _database.Connect("DataStore");
            _fallCol = _database.GetCollection("FallEvents");
        }

        [HttpGet("user/{userId}")]
        public IActionResult GetHistory(string userId)
        {
            // Trong h? th?ng th?c t?, FallEvent nên liên k?t t?i User theo Device. 
            // Gi? s? ta l?y tr?c ti?p ð? ðõn gi?n:
            var history = _fallCol.Select()
                                  .OrderByDescending(f => f.SelectPath("DetectedAt"))
                                  .Select(f => new FallEvent
                                  {
                                      Id = f.ObjectId,
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
    }
}