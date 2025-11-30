using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace HackathonCoordinator.WebAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            var userName = GetUserName();
            var connectionId = Context.ConnectionId;

            _logger.LogInformation($"=== SIGNALR CONNECTED ===");
            _logger.LogInformation($"User: {userName} (ID: {userId})");
            _logger.LogInformation($"Connection: {connectionId}");
            _logger.LogInformation($"Has User: {Context.User != null}");
            _logger.LogInformation($"User Identity: {Context.User?.Identity?.Name}");
            _logger.LogInformation($"Is Authenticated: {Context.User?.Identity?.IsAuthenticated}");

            if (Context.User != null)
            {
                foreach (var claim in Context.User.Claims)
                {
                    _logger.LogInformation($"Claim: {claim.Type} = {claim.Value}");
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = GetUserId();
            var userName = GetUserName();

            _logger.LogInformation($"User {userName} (ID: {userId}) disconnected from chat hub");

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Присоединиться к чату
        /// </summary>
        public async Task JoinChat(int chatId)
        {
            var userId = GetUserId();
            var userName = GetUserName();
            var connectionId = Context.ConnectionId;

            _logger.LogInformation($"=== JOIN CHAT ===");
            _logger.LogInformation($"User: {userName} (ID: {userId})");
            _logger.LogInformation($"Chat ID: {chatId}");
            _logger.LogInformation($"Connection: {connectionId}");

            await Groups.AddToGroupAsync(connectionId, $"chat-{chatId}");

            _logger.LogInformation($"User {userName} successfully joined group: chat-{chatId}");
        }

        /// <summary>
        /// Покинуть чат
        /// </summary>
        public async Task LeaveChat(int chatId)
        {
            var userId = GetUserId();
            var userName = GetUserName();

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat-{chatId}");

            _logger.LogInformation($"User {userName} left chat {chatId}");

            // Уведомляем других участников об отключении
            await Clients.OthersInGroup($"chat-{chatId}")
                .SendAsync("UserLeft", userId, userName);
        }

        private int GetUserId()
        {
            var idClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim?.Value, out var userId) ? userId : 0;
        }

        private string GetUserName()
        {
            return Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        }
    }
}