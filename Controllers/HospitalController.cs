using BsonData;
using Microsoft.AspNetCore.Mvc;
using DATN.Models;

namespace DATN.Controllers
{
    /// <summary>
    /// Hospital Dashboard Web Controller.
    /// Provides the dispatcher web UI to view and manage ambulance requests in real-time.
    /// </summary>
    public class HospitalController : Controller
    {
        private readonly Database _database;

        public HospitalController(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Main hospital dashboard — shows pending ambulance requests.
        /// </summary>
        public IActionResult Index()
        {
            var col = _database.GetCollection("AmbulanceRequests");
            var requests = col.Select()
                .OrderByDescending(r => r.SelectPath("RequestTime"))
                .Select(r => new AmbulanceRequest
                {
                    Id = r.ObjectId,
                    FallEventId = r.SelectPath("FallEventId")?.ToString() ?? "",
                    UserId = r.SelectPath("UserId")?.ToString() ?? "",
                    RequestTime = (DateTime?)r.SelectPath("RequestTime") ?? DateTime.UtcNow,
                    Location = r.SelectPath("Location")?.ToString() ?? "",
                    Status = r.SelectPath("Status")?.ToString() ?? "Pending",
                    DispatchNotes = r.SelectPath("DispatchNotes")?.ToString() ?? ""
                }).ToList();

            return View(requests);
        }

        /// <summary>
        /// Update the dispatch status of an ambulance request (POST from dashboard buttons).
        /// </summary>
        [HttpPost]
        public IActionResult UpdateStatus(string id, string status, string notes)
        {
            var col = _database.GetCollection("AmbulanceRequests");
            var doc = col.Select().FirstOrDefault(r => r.ObjectId == id);
            if (doc == null) return NotFound();

            doc["Status"] = status;
            if (!string.IsNullOrEmpty(notes))
                doc["DispatchNotes"] = notes;

            col.Update(id, doc);
            return RedirectToAction("Index");
        }
    }
}
