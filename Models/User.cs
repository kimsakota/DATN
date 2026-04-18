using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace DATN.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // Role: "User" (default) or "Hospital"
        public string Role { get; set; } = "User";

        // Settings
        public bool AutoEmergencyEnabled { get; set; } = true;
        public int CountdownSeconds { get; set; } = 30; // Delay before auto-dispatch
        
        public List<EmergencyContact> EmergencyContacts { get; set; } = new();
    }
}
