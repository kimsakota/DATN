using Microsoft.AspNetCore.Mvc;
using BsonData;
using DATN.Models;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace DATN.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly Database _database;
        private readonly Collection _userCol;

        public AuthController()
        {
            _database = new Database("FallDetectionDB");
            _database.Connect("DataStore");
            _userCol = _database.GetCollection("Users");
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var hash = HashPassword(request.Password);
            
            // T?m user
            var users = _userCol.Select();
            foreach (var doc in users)
            {
                if (doc.SelectPath("Username")?.ToString() == request.Username && 
                    doc.SelectPath("PasswordHash")?.ToString() == hash)
                {
                    return Ok(new { success = true, userId = doc.ObjectId, message = "Login successful" });
                }
            }

            return Unauthorized(new { success = false, message = "Invalid username or password" });
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] User userModel)
        {
            // Simple validation
            if (string.IsNullOrEmpty(userModel.Username) || string.IsNullOrEmpty(userModel.PasswordHash))
                return BadRequest("Invalid data");

            userModel.PasswordHash = HashPassword(userModel.PasswordHash);

            var doc = new Document
            {
                ["Username"] = userModel.Username,
                ["PasswordHash"] = userModel.PasswordHash,
                ["FullName"] = userModel.FullName,
                ["PhoneNumber"] = userModel.PhoneNumber,
            };

            _userCol.Insert(doc);

            return Ok(new { success = true, userId = doc.ObjectId });
        }

        private string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha.ComputeHash(bytes);
                return Convert.ToHexString(hash);
            }
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}