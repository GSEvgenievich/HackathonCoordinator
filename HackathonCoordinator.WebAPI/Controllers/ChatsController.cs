using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Hubs;
using HackathonCoordinator.WebAPI.Models;
using HackathonCoordinator.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace HackathonCoordinator.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatsController : BaseApiController
    {
        private readonly HackathonCoordinatorContext _context;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly NotificationHelperService _notificationHelper;

        public ChatsController(
            HackathonCoordinatorContext context,
            NotificationHelperService notificationHelper,
            IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
            _notificationHelper = notificationHelper;
        }

        /// <summary>
        /// Получить чат команды
        /// </summary>
        [HttpGet("team/{teamId}")]
        public async Task<ActionResult<ApiResponse<ChatDto>>> GetTeamChat(int teamId)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users
                    .Include(u => u.Team)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return HandleUnauthorized<ChatDto>("Пользователь не найден");

                // Проверка доступа - участник команды или организатор
                if (user.TeamId != teamId && user.RoleId != (int)Roles.Organizer)
                    return HandleForbidden<ChatDto>("Нет доступа к чату команды");

                var chat = await _context.Chats
                    .Include(c => c.Messages)
                        .ThenInclude(m => m.User)
                        .ThenInclude(u => u.ProfileIcon)
                    .Include(c => c.Type)
                    .FirstOrDefaultAsync(c => c.Teams.Any(t => t.Id == teamId) &&
                                             c.TypeId == (int)ChatTypes.TeamChat);

                if (chat == null)
                    return HandleNotFound<ChatDto>("Чат команды не найден");

                // Получаем участников команды
                var teamMembers = await _context.Users
                    .Where(u => u.TeamId == teamId)
                    .Include(u => u.ProfileIcon)
                    .Include(u => u.Role)
                    .ToListAsync();

                var chatDto = MapToChatDto(chat, userId, teamMembers);
                return HandleResult(chatDto);
            }
            catch (Exception ex)
            {
                return HandleError<ChatDto>("Ошибка при получении чата команды");
            }
        }

        /// <summary>
        /// Получить чат задачи
        /// </summary>
        [HttpGet("task/{taskId}")]
        public async Task<ActionResult<ApiResponse<ChatDto>>> GetTaskChat(int taskId)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users
                    .Include(u => u.Team)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return HandleUnauthorized<ChatDto>("Пользователь не найден");

                var task = await _context.Tasks
                    .Include(t => t.Team)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                    return HandleNotFound<ChatDto>("Задача не найдена");

                var teamMembers = await _context.Users
                    .Where(u => u.TeamId == task.TeamId)
                    .Include(u => u.ProfileIcon)
                    .Include(u => u.Role)
                    .ToListAsync();

                // Проверка доступа: капитан команды, исполнитель задачи или организатор
                var hasAccess = user.RoleId == (int)Roles.Organizer ||
                              teamMembers.Any(u => u.Id == userId && u.RoleId == (int)Roles.Captain) ||
                              task.AssignedToId == userId;

                if (!hasAccess)
                    return HandleForbidden<ChatDto>("Нет доступа к чату задачи");

                var chat = await _context.Chats
                    .Include(c => c.Messages)
                        .ThenInclude(m => m.User)
                        .ThenInclude(u => u.ProfileIcon)
                    .Include(c => c.Type)
                    .FirstOrDefaultAsync(c => c.Tasks.Any(t => t.Id == taskId) &&
                                             c.TypeId == (int)ChatTypes.TaskChat);

                if (chat == null)
                    return HandleNotFound<ChatDto>("Чат задачи не найден");

                // Получаем участников чата задачи (капитан и исполнитель)
                var participants = new List<User>();
                var captain = teamMembers.FirstOrDefault(u => u.RoleId == (int)Roles.Captain);
                var assignedTo = teamMembers.FirstOrDefault(u => u.Id == task.AssignedToId);

                if (captain != null)
                    participants.Add(captain);

                if (assignedTo != null && assignedTo.Id != captain?.Id)
                    participants.Add(assignedTo);

                var chatDto = MapToChatDto(chat, userId, participants);
                return HandleResult(chatDto);
            }
            catch (Exception ex)
            {
                return HandleError<ChatDto>("Ошибка при получении чата задачи");
            }
        }

        /// <summary>
        /// Отправить сообщение
        /// </summary>
        [HttpPost("send")]
        public async Task<ActionResult<ApiResponse<MessageDto>>> SendMessage([FromBody] SendMessageDto dto)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users
                    .Include(u => u.ProfileIcon)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return HandleUnauthorized<MessageDto>("Пользователь не найден");

                var chat = await _context.Chats
                    .Include(c => c.Teams)
                    .Include(c => c.Tasks)
                        .ThenInclude(t => t.Team)
                        .ThenInclude(t => t.Users)
                    .FirstOrDefaultAsync(c => c.Id == dto.ChatId);

                if (chat == null)
                    return HandleNotFound<MessageDto>("Чат не найден");

                // Проверка доступа в зависимости от типа чата
                bool hasAccess = false;

                if (chat.TypeId == (int)ChatTypes.TeamChat) // Чат команды
                {
                    hasAccess = chat.Teams.Any(t => t.Users.Any(u => u.Id == userId)) ||
                               user.RoleId == (int)Roles.Organizer;
                }
                else if (chat.TypeId == (int)ChatTypes.TaskChat) // Чат задачи
                {
                    var task = chat.Tasks.FirstOrDefault();
                    if (task != null)
                    {
                        // Проверяем доступ: капитан, исполнитель или организатор
                        hasAccess = user.RoleId == (int)Roles.Organizer ||
                                   task.Team.Users.Any(u => u.Id == userId && u.RoleId == (int)Roles.Captain) ||
                                   task.AssignedToId == userId;
                    }
                }

                if (!hasAccess)
                    return HandleForbidden<MessageDto>("Нет доступа к чату");

                var message = new Message
                {
                    ChatId = dto.ChatId,
                    UserId = userId,
                    Text = dto.Text.Trim(),
                    SentAt = DateTime.Now,
                    IsEdited = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Загружаем полные данные сообщения
                await _context.Entry(message)
                    .Reference(m => m.User)
                    .Query()
                    .Include(u => u.ProfileIcon)
                    .LoadAsync();

                var messageDto = new MessageDto
                {
                    Id = message.Id,
                    ChatId = message.ChatId,
                    UserId = message.UserId,
                    UserName = message.User.Username,
                    UserIcon = $"/Assets/Images/Profile/{message.User.ProfileIcon?.Name ?? "boy1"}.png",
                    Text = message.Text,
                    SentAt = message.SentAt,
                    IsMyMessage = false
                };

                // Отправляем сообщение через SignalR
                await _hubContext.Clients.Group($"chat-{dto.ChatId}")
                    .SendAsync("ReceiveMessage", messageDto);

                // Обработка уведомлений через @notify
                if (dto.Text.Contains("@notify") && user.RoleId == (int)Roles.Captain)
                {
                    await ProcessNotificationFromMessageAsync(chat, user, message);
                }

                return HandleResult(messageDto, "Сообщение отправлено");
            }
            catch (Exception ex)
            {
                return HandleError<MessageDto>("Ошибка при отправке сообщения");
            }
        }

        /// <summary>
        /// Редактировать сообщение
        /// </summary>
        [HttpPut("messages/{messageId}")]
        public async Task<ActionResult<ApiResponse>> EditMessage(int messageId, [FromBody] string newText)
        {
            try
            {
                var userId = GetUserId();
                var message = await _context.Messages
                    .Include(m => m.Chat)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                    return HandleNotFound("Сообщение не найдено");

                if (message.UserId != userId)
                    return HandleForbidden("Можно редактировать только свои сообщения");

                // Проверяем, не прошло ли слишком много времени (например, 15 минут)
                var timeSinceSent = DateTime.Now - message.SentAt;
                if (timeSinceSent.TotalMinutes > 15)
                    return HandleError("Сообщение можно редактировать только в течение 15 минут после отправки");

                message.Text = newText.Trim();
                message.IsEdited = true;
                message.EditedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // Уведомляем через SignalR
                await _hubContext.Clients.Group($"chat-{message.ChatId}")
                    .SendAsync("MessageEdited", messageId, newText);

                return HandleSuccess("Сообщение отредактировано");
            }
            catch (Exception ex)
            {
                return HandleError("Ошибка при редактировании сообщения");
            }
        }

        /// <summary>
        /// Удалить сообщение
        /// </summary>
        [HttpDelete("messages/{messageId}")]
        public async Task<ActionResult<ApiResponse>> DeleteMessage(int messageId)
        {
            try
            {
                var userId = GetUserId();
                var message = await _context.Messages
                    .Include(m => m.Chat)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                    return HandleNotFound("Сообщение не найдено");

                var user = await _context.Users.FindAsync(userId);

                // Может удалить только автор или организатор
                if (message.UserId != userId && user?.RoleId != (int)Roles.Organizer)
                    return HandleForbidden("Можно удалять только свои сообщения");

                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();

                // Уведомляем через SignalR
                await _hubContext.Clients.Group($"chat-{message.ChatId}")
                    .SendAsync("MessageDeleted", messageId);

                return HandleSuccess("Сообщение удалено");
            }
            catch (Exception ex)
            {
                return HandleError("Ошибка при удалении сообщения");
            }
        }

        /// <summary>
        /// Получить историю сообщений с пагинацией
        /// </summary>
        [HttpGet("{chatId}/messages")]
        public async Task<ActionResult<ApiResponse<List<MessageDto>>>> GetChatMessages(
            int chatId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return HandleUnauthorized<List<MessageDto>>("Пользователь не найден");

                var chat = await _context.Chats
                    .Include(c => c.Teams)
                    .Include(c => c.Tasks)
                    .FirstOrDefaultAsync(c => c.Id == chatId);

                if (chat == null)
                    return HandleNotFound<List<MessageDto>>("Чат не найден");

                // Проверка доступа
                bool hasAccess = await CheckChatAccess(chat, userId);
                if (!hasAccess)
                    return HandleForbidden<List<MessageDto>>("Нет доступа к чату");

                var messages = await _context.Messages
                    .Where(m => m.ChatId == chatId)
                    .Include(m => m.User)
                    .ThenInclude(u => u.ProfileIcon)
                    .OrderByDescending(m => m.SentAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .OrderBy(m => m.SentAt)
                    .Select(m => new MessageDto
                    {
                        Id = m.Id,
                        ChatId = m.ChatId,
                        UserId = m.UserId,
                        UserName = m.User.Username,
                        UserIcon = m.User.ProfileIcon.Name,
                        Text = m.Text,
                        SentAt = m.SentAt,
                        IsEdited = m.IsEdited,
                        IsMyMessage = m.UserId == userId
                    })
                    .ToListAsync();

                return HandleResult(messages);
            }
            catch (Exception ex)
            {
                return HandleError<List<MessageDto>>("Ошибка при получении сообщений");
            }
        }

        // --- Вспомогательные методы ---

        private async Task<bool> CheckChatAccess(Chat chat, int userId)
        {
            var user = await _context.Users
                .Include(u => u.Team)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return false;

            if (user.RoleId == (int)Roles.Organizer)
                return true;

            if (chat.TypeId == (int)ChatTypes.TeamChat)
            {
                return chat.Teams.Any(t => t.Users.Any(u => u.Id == userId));
            }
            else if (chat.TypeId == (int)ChatTypes.TaskChat)
            {
                var task = chat.Tasks.FirstOrDefault();
                if (task != null)
                {
                    return task.Team.Users.Any(u => u.Id == userId && u.RoleId == (int)Roles.Captain) ||
                           task.AssignedToId == userId;
                }
            }

            return false;
        }

        private ChatDto MapToChatDto(Chat chat, int currentUserId, List<User> participants)
        {
            var messages = chat.Messages
                .OrderBy(m => m.SentAt)
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    ChatId = m.ChatId,
                    UserId = m.UserId,
                    UserName = m.User.Username,
                    UserIcon = $"/Assets/Images/Profile/{m.User.ProfileIcon?.Name ?? "boy1"}.png",
                    Text = m.Text,
                    SentAt = m.SentAt,
                    IsEdited = m.IsEdited,
                    IsMyMessage = m.UserId == currentUserId
                })
                .ToList();

            var participantDtos = participants
                .Select(p => new ChatParticipantDto
                {
                    UserId = p.Id,
                    UserName = p.Username,
                    UserIcon = $"/Assets/Images/Profile/{p.ProfileIcon?.Name ?? "boy1"}.png",
                    Role = p.Role.Name,
                })
                .ToList();

            return new ChatDto
            {
                Id = chat.Id,
                Name = chat.Name,
                Type = chat.Type.Name,
                TeamId = chat.Teams.FirstOrDefault()?.Id,
                TaskId = chat.Tasks.FirstOrDefault()?.Id,
                Messages = messages,
                Participants = participantDtos,
                CreatedAt = chat.CreatedAt,
                CanSendMessages = true
            };
        }

        private async Task ProcessNotificationFromMessageAsync(Chat chat, User captain, Message message)
        {
            try
            {
                if (chat.TypeId == (int)ChatTypes.TeamChat)
                {
                    var team = chat.Teams.FirstOrDefault();
                    if (team != null)
                    {
                        await _notificationHelper.NotifyImportantTeamChatMessage(
                            chat.Id,
                            team.Id,
                            captain.Username,
                            message.Text.Replace("@notify", "").Trim());
                    }
                }
                else if (chat.TypeId == (int)ChatTypes.TaskChat)
                {
                    var task = chat.Tasks.FirstOrDefault();
                    if (task != null && task.AssignedToId.HasValue)
                    {
                        await _notificationHelper.NotifyImportantTaskChatMessage(
                            chat.Id,
                            task.AssignedToId.Value,
                            task.Id,
                            task.Title,
                            captain.Username,
                            message.Text.Replace("@notify", "").Trim());
                    }
                }
            }
            catch (Exception) { }
        }
    }
}