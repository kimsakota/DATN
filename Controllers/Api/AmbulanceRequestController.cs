using Microsoft.AspNetCore.Mvc;
using BsonData;
using DATN.Models;

namespace DATN.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmbulanceRequestController : ControllerBase
    {
        private readonly Database _database;
        private readonly Collection _ambulanceCol;

        public AmbulanceRequestController(Database database)
        {
            _database = database;
            _ambulanceCol = _database.GetCollection("AmbulanceRequests");
        }

        [HttpPost]
        public IActionResult CreateRequest([FromBody] AmbulanceRequest request)
        {
            if (string.IsNullOrEmpty(request.FallEventId))
                return BadRequest("FallEventId is required.");

            var doc = new Document
            {
                ["FallEventId"] = request.FallEventId,
                ["UserId"] = request.UserId,
                ["RequestTime"] = DateTime.UtcNow,
                ["Location"] = request.Location ?? "",
                ["Status"] = "Pending",
                ["DispatchNotes"] = request.DispatchNotes ?? ""
            };

            _ambulanceCol.Insert(doc);
            return Ok(new { success = true, id = doc.ObjectId, message = "Ambulance request created" });
        }

        [HttpGet("pending")]
        public IActionResult GetPendingRequests()
        {
            var requests = _ambulanceCol.Select()
                .Where(r => r.SelectPath("Status")?.ToString() != "Completed" 
                         && r.SelectPath("Status")?.ToString() != "Rejected")
                .OrderByDescending(r => r.SelectPath("RequestTime"))
                .Select(MapToModel)
                .ToList();

            return Ok(requests);
        }

        [HttpGet("all")]
        public IActionResult GetAllRequests()
        {
            var requests = _ambulanceCol.Select()
                .OrderByDescending(r => r.SelectPath("RequestTime"))
                .Select(MapToModel)
                .ToList();

            return Ok(requests);
        }

        [HttpGet("{id}")]
        public IActionResult GetRequestDetail(string id)
        {
            var doc = _ambulanceCol.Select().FirstOrDefault(r => r.ObjectId == id);
            if (doc == null) return NotFound("Ambulance request not found.");
            return Ok(MapToModel(doc));
        }

        [HttpPut("{id}/status")]
        public IActionResult UpdateStatus(string id, [FromBody] DispatchStatusUpdate update)
        {
            var existing = _ambulanceCol.Select().FirstOrDefault(r => r.ObjectId == id);
            if (existing == null) return NotFound("Ambulance request not found.");

            existing["Status"] = update.Status;
            if (!string.IsNullOrEmpty(update.DispatchNotes))
                existing["DispatchNotes"] = update.DispatchNotes;

            _ambulanceCol.Update(id, existing);
            return Ok(new { success = true, message = $"Status updated to {update.Status}" });
        }

        [HttpGet("user/{userId}")]
        public IActionResult GetUserRequests(string userId)
        {
            var requests = _ambulanceCol.Select()
                .Where(r => r.SelectPath("UserId")?.ToString() == userId)
                .OrderByDescending(r => r.SelectPath("RequestTime"))
                .Select(MapToModel)
                .ToList();

            return Ok(requests);
        }

        private AmbulanceRequest MapToModel(Document doc)
        {
            return new AmbulanceRequest
            {
                Id = doc.ObjectId,
                FallEventId = doc.SelectPath("FallEventId")?.ToString(),
                UserId = doc.SelectPath("UserId")?.ToString(),
                RequestTime = (DateTime?)doc.SelectPath("RequestTime") ?? DateTime.UtcNow,
                Location = doc.SelectPath("Location")?.ToString() ?? "",
                Status = doc.SelectPath("Status")?.ToString() ?? "Pending",
                DispatchNotes = doc.SelectPath("DispatchNotes")?.ToString() ?? ""
            };
        }
    }

    public class DispatchStatusUpdate
    {
        public string Status { get; set; } = "Pending";
        public string DispatchNotes { get; set; } = "";
    }
}