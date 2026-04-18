using Microsoft.AspNetCore.Mvc;
using BsonData;
using DATN.Models;

namespace DATN.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly Database _database;
        private readonly Collection _userCol;

        public UserController(Database database)
        {
            _database = database;
            _userCol = _database.GetCollection("Users");
        }

        [HttpPut("{id}/settings")]
        public IActionResult UpdateSettings(string id, [FromBody] UserSettingsUpdate request)
        {
            var existing = _userCol.Select().FirstOrDefault(u => u.ObjectId == id);
            if (existing == null) return NotFound("User not found");

            existing["AutoEmergencyEnabled"] = request.AutoEmergencyEnabled;
            existing["CountdownSeconds"] = request.CountdownSeconds > 0 ? request.CountdownSeconds : 30;
            _userCol.Update(id, existing);
            return Ok(new { success = true });
        }
    }

    public class UserSettingsUpdate
    {
        public bool AutoEmergencyEnabled { get; set; }
        public int CountdownSeconds { get; set; } = 30;
    }
}