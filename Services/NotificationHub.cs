using Microsoft.AspNetCore.SignalR;

namespace DATN.Services
{
    /// <summary>
    /// SignalR Hub for real-time notifications to mobile app clients.
    /// Clients join groups based on their userId to receive targeted alerts.
    /// </summary>
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Client calls this after login to join their user-specific notification group.
        /// </summary>
        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation($"Client {Context.ConnectionId} joined group user_{userId}");
        }

        /// <summary>
        /// Client calls this on logout to leave their notification group.
        /// </summary>
        public async Task LeaveUserGroup(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation($"Client {Context.ConnectionId} left group user_{userId}");
        }

        /// <summary>
        /// Hospital staff joins hospital group for ambulance request notifications.
        /// </summary>
        public async Task JoinHospitalGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "hospital");
            _logger.LogInformation($"Hospital client {Context.ConnectionId} joined hospital group");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}