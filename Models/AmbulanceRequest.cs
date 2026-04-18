using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace DATN.Models
{
    public class AmbulanceRequest
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        [BsonRepresentation(BsonType.ObjectId)]
        public string? FallEventId { get; set; }
        
        [BsonRepresentation(BsonType.ObjectId)]
        public string? UserId { get; set; }
        
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Dispatched, Arrived, Completed
        public string DispatchNotes { get; set; } = string.Empty;
    }
}
