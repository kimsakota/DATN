using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace DATN.Models
{
    public class FallEvent
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        [BsonRepresentation(BsonType.ObjectId)]
        public string? UserId { get; set; }
        public string DeviceId { get; set; } = string.Empty;

        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        // V? trí khi té ng? (n?u có GPS ho?c LBS)
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string Status { get; set; } = "Detected"; // Ð? x? l?, ðang ð?i x? l?...
        public bool FalseAlarm { get; set; } = false; // Ðánh d?u là báo ð?ng gi?
        public string Source { get; set; } = "Sensor"; // B?t ngu?n t? ðâu...
    }
}
