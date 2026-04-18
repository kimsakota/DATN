using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace DATN.Models
{
    public class Device
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string DeviceId { get; set; } = string.Empty; // ESP32 MAC or unique identifier
        
        [BsonRepresentation(BsonType.ObjectId)]
        public string? UserId { get; set; } // Liên k?t ð?n userId ðang s? d?ng thi?t b? này
        
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastConnectedAt { get; set; }
    }
}
