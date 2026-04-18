using BsonData;
using Microsoft.AspNetCore.SignalR;

namespace DATN.Services
{
    /// <summary>
    /// Service responsible for sending notifications via SignalR
    /// and logging notification history to the database.
    /// Also reads EmergencyContacts and notifies them per Plan §4.5.
    /// </summary>
    public class NotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;
        private readonly Database _database;

        public NotificationService(IHubContext<NotificationHub> hubContext, ILogger<NotificationService> logger, Database database)
        {
            _hubContext = hubContext;
            _logger = logger;
            _database = database;
        }

        /// <summary>
        /// Push fall alert to the user in-app via SignalR.
        /// </summary>
        public async Task PushFallAlertAsync(string userId, string fallEventId, string deviceId, DateTime detectedAt)
        {
            var payload = new
            {
                Type = "FallAlert",
                FallEventId = fallEventId,
                DeviceId = deviceId,
                DetectedAt = detectedAt,
                Message = "Fall detected! Please check immediately."
            };

            await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", payload);
            _logger.LogWarning($"[NOTIFICATION] Fall alert sent to user_{userId}");

            SaveNotificationLog(userId, "FallAlert", $"Fall detected on device {deviceId}");
        }

        /// <summary>
        /// Notify Emergency Contacts (§4.5 Activity: Notify Emergency Contact).
        /// Reads the user's EmergencyContacts list from DB and:
        /// 1. Logs each contact that should be notified (for SMS/call integration)
        /// 2. Sends SignalR push to contacts_{userId} group (if contacts use the app)
        /// 3. Saves individual notification log per contact
        /// NOTE: Real SMS/call requires Twilio or FCM integration (external API keys).
        ///       This implementation provides the full pipeline — swap SavedContact log
        ///       with Twilio.SendSms() call when credentials are available.
        /// </summary>
        public async Task NotifyEmergencyContactsAsync(string userId, string fallEventId, string deviceId, DateTime detectedAt)
        {
            // Read emergency contacts from DB
            var contactCol = _database.GetCollection("EmergencyContacts");
            var contacts = contactCol.Select()
                .Where(c => c.SelectPath("UserId")?.ToString() == userId)
                .ToList();

            if (!contacts.Any())
            {
                _logger.LogInformation($"[NOTIFICATION] No emergency contacts for user {userId}");
                return;
            }

            var alertMessage = $"CANH BAO TE NGA! Nguoi than cua ban vua bi phat hien te nga luc {detectedAt:HH:mm dd/MM/yyyy}. Thiet bi: {deviceId}. Vui long kiem tra ngay!";

            foreach (var contact in contacts)
            {
                var name = contact.SelectPath("Name")?.ToString() ?? "Unknown";
                var phone = contact.SelectPath("PhoneNumber")?.ToString() ?? "";
                var relationship = contact.SelectPath("Relationship")?.ToString() ?? "";

                // Log each contact notification attempt
                _logger.LogWarning($"[NOTIFY CONTACT] Sending to {name} ({relationship}) - Phone: {phone}");

                // TODO: Replace with real SMS/FCM when credentials available:
                // await _twilioClient.SendSms(phone, alertMessage);
                // await _fcmClient.SendPushToContact(phone, alertMessage);

                // Save per-contact notification log
                SaveNotificationLog(contact.ObjectId, "EmergencyContactAlert",
                    $"Alert sent to {name} ({phone}) for fall event {fallEventId}");
            }

            // Also push SignalR to contacts group (for contacts who also have the app)
            var payload = new
            {
                Type = "FallAlert",
                FallEventId = fallEventId,
                DeviceId = deviceId,
                DetectedAt = detectedAt,
                Message = alertMessage
            };
            await _hubContext.Clients.Group($"contacts_{userId}").SendAsync("ReceiveNotification", payload);

            _logger.LogWarning($"[NOTIFICATION] Notified {contacts.Count} emergency contacts for user {userId}");
        }

        /// <summary>
        /// Push ambulance status update to user.
        /// </summary>
        public async Task PushAmbulanceStatusAsync(string userId, string requestId, string status)
        {
            var payload = new
            {
                Type = "AmbulanceStatus",
                RequestId = requestId,
                Status = status,
                Message = $"Ambulance status updated: {status}"
            };

            await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", payload);
            _logger.LogInformation($"[NOTIFICATION] Ambulance status '{status}' sent to user_{userId}");

            SaveNotificationLog(userId, "AmbulanceStatus", $"Ambulance {requestId}: {status}");
        }

        /// <summary>
        /// Notify hospital of new ambulance request.
        /// </summary>
        public async Task NotifyHospitalAsync(string requestId, string userId, string location)
        {
            var payload = new
            {
                Type = "NewAmbulanceRequest",
                RequestId = requestId,
                UserId = userId,
                Location = location,
                Message = "New ambulance request received!"
            };

            await _hubContext.Clients.Group("hospital").SendAsync("ReceiveNotification", payload);
            _logger.LogWarning($"[NOTIFICATION] New ambulance request {requestId} sent to hospital group");

            SaveNotificationLog("hospital", "AmbulanceRequest", $"New request {requestId} from user {userId}");
        }

        private void SaveNotificationLog(string targetId, string type, string message)
        {
            try
            {
                var logCol = _database.GetCollection("NotificationLogs");
                logCol.Insert(new Document
                {
                    ["TargetId"] = targetId,
                    ["Type"] = type,
                    ["Message"] = message,
                    ["SentAt"] = DateTime.UtcNow,
                    ["IsRead"] = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save notification log");
            }
        }
    }
}