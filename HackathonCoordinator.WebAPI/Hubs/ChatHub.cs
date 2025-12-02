using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace HackathonCoordinator.WebAPI.Hubs
{
    /// <summary>
    /// SignalR хаб для чата
    /// </summary>
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
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Присоединиться к чату
        /// </summary>
        public async Task JoinChat(int chatId)
        {
            var userId = GetUserId();
            var connectionId = Context.ConnectionId;

            await Groups.AddToGroupAsync(connectionId, $"chat-{chatId}");
        }

        /// <summary>
        /// Покинуть чат
        /// </summary>
        public async Task LeaveChat(int chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat-{chatId}");
        }

        /// <summary>
        /// Получение ID текущего пользователя из claims
        /// </summary>
        private int GetUserId()
        {
            var idClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim?.Value, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Получение имени текущего пользователя из claims
        /// </summary>
        private string GetUserName()
        {
            return Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Неизвестно";
        }
    }
}