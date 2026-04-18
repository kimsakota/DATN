using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DATN.Models
{
    public class EmergencyContact
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        [BsonRepresentation(BsonType.ObjectId)]
        public string? UserId { get; set; } // Liên k?t ð?n User

        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;

        // Ýu tiên hi?n th? trên cùng (First responder)
        public bool IsPrimary { get; set; } = false;
    }
}
