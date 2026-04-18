using Microsoft.AspNetCore.Mvc;
using BsonData;
using DATN.Models;

namespace DATN.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmergencyContactController : ControllerBase
    {
        private readonly Database _database;
        private readonly Collection _contactCol;

        public EmergencyContactController(Database database)
        {
            _database = database;
            _contactCol = _database.GetCollection("EmergencyContacts");
        }

        [HttpGet("user/{userId}")]
        public IActionResult GetContacts(string userId)
        {
            var contacts = _contactCol.Select()
                                    .Where(c => c.SelectPath("UserId")?.ToString() == userId)
                                    .Select(c => new EmergencyContact
                                    {
                                        Id = c.ObjectId,
                                        UserId = c.SelectPath("UserId")?.ToString(),
                                        Name = c.SelectPath("Name")?.ToString(),
                                        PhoneNumber = c.SelectPath("PhoneNumber")?.ToString(),
                                        Relationship = c.SelectPath("Relationship")?.ToString(),
                                        IsPrimary = (bool?)c.SelectPath("IsPrimary") ?? false
                                    }).ToList();

            return Ok(contacts);
        }

        [HttpPost]
        public IActionResult AddContact([FromBody] EmergencyContact contact)
        {
            if (string.IsNullOrEmpty(contact.UserId) || string.IsNullOrEmpty(contact.PhoneNumber))
                return BadRequest("Invalid details");

            var doc = new Document
            {
                ["UserId"] = contact.UserId,
                ["Name"] = contact.Name,
                ["PhoneNumber"] = contact.PhoneNumber,
                ["Relationship"] = contact.Relationship,
                ["IsPrimary"] = contact.IsPrimary
            };

            _contactCol.Insert(doc);
            return Ok(new { success = true, id = doc.ObjectId });
        }

        [HttpPut("{id}")]
        public IActionResult UpdateContact(string id, [FromBody] EmergencyContact contact)
        {
            var existing = _contactCol.Select().FirstOrDefault(c => c.ObjectId == id);
            if (existing == null) return NotFound("Contact not found.");

            existing["Name"] = contact.Name;
            existing["PhoneNumber"] = contact.PhoneNumber;
            existing["Relationship"] = contact.Relationship;
            existing["IsPrimary"] = contact.IsPrimary;

            _contactCol.Update(id, existing);
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteContact(string id)
        {
            _contactCol.Delete(id);
            return Ok(new { success = true });
        }
    }
}