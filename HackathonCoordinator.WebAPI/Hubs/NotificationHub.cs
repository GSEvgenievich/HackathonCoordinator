// Hubs/NotificationHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace HackathonCoordinator.WebAPI.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await UnsubscribeFromNotifications();
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Подписаться на уведомления пользователя
        /// </summary>
        public async Task SubscribeToUserNotifications()
        {
            var userId = GetUserId();
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        /// <summary>
        /// Отписаться от уведомлений пользователя
        /// </summary>
        public async Task UnsubscribeFromNotifications()
        {
            var userId = GetUserId();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        private int GetUserId()
        {
            var idClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim?.Value, out var userId) ? userId : 0;
        }
    }
}