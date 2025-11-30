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
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Отписаться от всех уведомлений
        /// </summary>
        public async Task UnsubscribeFromAllNotifications(int? teamId)
        {
            await UnsubscribeFromUserNotifications();
            await UnsubscribeFromOrganizersNotifications();

            if (teamId != null)
                UnsubscribeFromTeamNotifications(teamId.Value);
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
        public async Task UnsubscribeFromUserNotifications()
        {
            var userId = GetUserId();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        /// <summary>
        /// Подписаться на уведомления организаторов
        /// </summary>
        public async Task SubscribeToOrganizersNotifications()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"organizers");
        }

        /// <summary>
        /// Отписаться от уведомлений организаторов
        /// </summary>
        public async Task UnsubscribeFromOrganizersNotifications()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"organizers");
        }

        /// <summary>
        /// Подписаться на уведомления команды
        /// </summary>
        public async Task SubscribeToTeamNotifications(int teamId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"team-{teamId}");
        }

        /// <summary>
        /// Отписаться от уведомлений команды
        /// </summary>
        public async Task UnsubscribeFromTeamNotifications(int teamId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"team-{teamId}");
        }

        private int GetUserId()
        {
            var idClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim?.Value, out var userId) ? userId : 0;
        }
    }
}