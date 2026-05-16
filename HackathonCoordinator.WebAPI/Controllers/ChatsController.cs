using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Helpers;
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
        private readonly IStorageService _storageService;  // Изменено с IFileStorageService на IStorageService

        public ChatsController(
            HackathonCoordinatorContext context,
            NotificationHelperService notificationHelper,
            IStorageService storageService,  // Изменено
            IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
            _storageService = storageService;  // Изменено
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

                if (user.TeamId != teamId && user.RoleId != (int)Roles.Organizer && user.RoleId != (int)Roles.Admin)
                    return HandleForbidden<ChatDto>("Нет доступа к чату команды");

                var chat = await _context.Chats
                    .Include(c => c.Messages)
                        .ThenInclude(m => m.User)
                        .ThenInclude(u => u.ProfileIcon)
                    .Include(c => c.Messages)
                        .ThenInclude(m => m.MessageAttachments)
                    .Include(c => c.Type)
                    .FirstOrDefaultAsync(c => c.Teams.Any(t => t.Id == teamId) &&
                                             c.TypeId == (int)ChatTypes.TeamChat);

                if (chat == null)
                    return HandleNotFound<ChatDto>("Чат команды не найден");

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

                var hasAccess = user.RoleId == (int)Roles.Organizer ||
                                user.RoleId == (int)Roles.Admin ||
                                teamMembers.Any(u => u.Id == userId && u.RoleId == (int)Roles.Captain) ||
                                task.AssignedToId == userId;

                if (!hasAccess)
                    return HandleForbidden<ChatDto>("Нет доступа к чату задачи");

                var chat = await _context.Chats
                    .Include(c => c.Messages)
                        .ThenInclude(m => m.User)
                        .ThenInclude(u => u.ProfileIcon)
                    .Include(c => c.Messages)
                        .ThenInclude(m => m.MessageAttachments)
                    .Include(c => c.Type)
                    .FirstOrDefaultAsync(c => c.Tasks.Any(t => t.Id == taskId) &&
                                             c.TypeId == (int)ChatTypes.TaskChat);

                if (chat == null)
                    return HandleNotFound<ChatDto>("Чат задачи не найден");

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

                bool hasAccess = await CheckChatAccess(chat, userId);
                if (!hasAccess)
                    return HandleForbidden<List<MessageDto>>("Нет доступа к чату");

                var messages = await _context.Messages
                    .Where(m => m.ChatId == chatId)
                    .Include(m => m.User)
                        .ThenInclude(u => u.ProfileIcon)
                    .Include(m => m.MessageAttachments)
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
                        IsMyMessage = m.UserId == userId,
                        HasAttachments = m.HasAttachments,
                        Attachments = m.MessageAttachments.Select(a => new MessageAttachmentDto
                        {
                            Id = a.Id,
                            MessageId = a.MessageId,
                            FileName = a.FileName,
                            FileSize = a.FileSize,
                            ContentType = a.ContentType,
                            FilePath = a.FilePath,
                            ThumbnailBase64 = a.Thumbnail != null ? Convert.ToBase64String(a.Thumbnail) : null,
                            UploadedAt = a.UploadedAt
                        }).ToList()
                    })
                    .ToListAsync();

                return HandleResult(messages);
            }
            catch (Exception ex)
            {
                return HandleError<List<MessageDto>>("Ошибка при получении сообщений");
            }
        }

        /// <summary>
        /// Отправить сообщение с вложениями
        /// </summary>
        [HttpPost("send-with-attachments")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        public async Task<ActionResult<ApiResponse<MessageDto>>> SendMessageWithAttachments([FromForm] SendMessageWithAttachmentsDto request)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users
                    .Include(u => u.ProfileIcon)
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return HandleUnauthorized<MessageDto>("Пользователь не найден");

                var chat = await _context.Chats
                    .Include(c => c.Teams)
                    .Include(c => c.Tasks)
                        .ThenInclude(t => t.Team)
                        .ThenInclude(t => t.Users)
                    .FirstOrDefaultAsync(c => c.Id == request.ChatId);

                if (chat == null)
                    return HandleNotFound<MessageDto>("Чат не найден");

                bool hasAccess = await CheckChatAccess(chat, userId);
                if (!hasAccess)
                    return HandleForbidden<MessageDto>("Нет доступа к чату");

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var message = new Message
                    {
                        ChatId = request.ChatId,
                        UserId = userId,
                        Text = request.Text?.Trim() ?? "",
                        SentAt = DateTime.Now,
                        IsEdited = false,
                        HasAttachments = request.Attachments != null && request.Attachments.Any()
                    };

                    _context.Messages.Add(message);
                    await _context.SaveChangesAsync();

                    var attachments = new List<MessageAttachmentDto>();
                    if (request.Attachments != null && request.Attachments.Any())
                    {
                        foreach (var file in request.Attachments)
                        {
                            using var ms = new MemoryStream();
                            await file.CopyToAsync(ms);
                            var fileData = ms.ToArray();

                            // Загружаем в MinIO
                            var objectName = await _storageService.UploadAsync(
                                new MemoryStream(fileData),
                                file.FileName,
                                file.ContentType,
                                fileData.Length,
                                message.Id);

                            // Создаем эскиз для изображений
                            byte[]? thumbnail = null;
                            if (file.ContentType.StartsWith("image/"))
                            {
                                thumbnail = await _storageService.GetThumbnailAsync(objectName);
                            }

                            var attachment = new MessageAttachment
                            {
                                MessageId = message.Id,
                                FileName = file.FileName,
                                FileSize = fileData.Length,
                                ContentType = file.ContentType,
                                FilePath = objectName,
                                Thumbnail = thumbnail,
                                UploadedAt = DateTime.Now
                            };

                            _context.MessageAttachments.Add(attachment);
                            await _context.SaveChangesAsync();

                            attachments.Add(new MessageAttachmentDto
                            {
                                Id = attachment.Id,
                                MessageId = attachment.MessageId,
                                FileName = attachment.FileName,
                                FileSize = attachment.FileSize,
                                ContentType = attachment.ContentType,
                                FilePath = attachment.FilePath,
                                ThumbnailBase64 = thumbnail != null ? Convert.ToBase64String(thumbnail) : null,
                                UploadedAt = attachment.UploadedAt
                            });
                        }
                    }

                    await transaction.CommitAsync();

                    var messageDto = new MessageDto
                    {
                        Id = message.Id,
                        ChatId = message.ChatId,
                        UserId = message.UserId,
                        UserName = user.Username,
                        UserIcon = $"/Assets/Images/Profile/{user.ProfileIcon?.Name ?? "boy1"}.png",
                        Text = message.Text,
                        SentAt = message.SentAt,
                        IsMyMessage = false,
                        HasAttachments = message.HasAttachments,
                        Attachments = attachments
                    };

                    await _hubContext.Clients.Group($"chat-{request.ChatId}")
                        .SendAsync("ReceiveMessage", messageDto);

                    if (request.Text != null && request.Text.Contains("@notify") && user.RoleId != (int)Roles.Member)
                    {
                        await ProcessNotificationFromMessageAsync(chat, user, message);
                    }

                    return HandleResult(messageDto, "Сообщение отправлено");
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return HandleError<MessageDto>($"Ошибка при отправке сообщения: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить полное изображение для просмотра (через MinIO)
        /// </summary>
        [HttpGet("image/{attachmentId}")]
        public async Task<ActionResult<ApiResponse>> GetFullImage(int attachmentId)
        {
            try
            {
                var attachment = await _context.MessageAttachments
                    .Include(a => a.Message)
                    .ThenInclude(m => m.Chat)
                    .FirstOrDefaultAsync(a => a.Id == attachmentId);

                if (attachment == null)
                    return HandleNotFound();

                var userId = GetUserId();
                var hasAccess = await CheckChatAccess(attachment.Message.Chat, userId);
                if (!hasAccess)
                    return HandleForbidden();

                if (!attachment.ContentType.StartsWith("image/"))
                    return HandleError("Файл не является изображением");

                // Скачиваем из MinIO
                var stream = await _storageService.DownloadAsync(attachment.FilePath);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var fileBytes = ms.ToArray();

                return File(fileBytes, attachment.ContentType);
            }
            catch (Exception ex)
            {
                return HandleError($"Ошибка при загрузке изображения: {ex.Message}");
            }
        }

        /// <summary>
        /// Скачать вложение (через MinIO с presigned URL)
        /// </summary>
        [HttpGet("download/{attachmentId}")]
        public async Task<ActionResult<ApiResponse>> DownloadAttachment(int attachmentId)
        {
            try
            {
                var attachment = await _context.MessageAttachments
                    .Include(a => a.Message)
                    .ThenInclude(m => m.Chat)
                    .FirstOrDefaultAsync(a => a.Id == attachmentId);

                if (attachment == null)
                    return HandleNotFound();

                var userId = GetUserId();
                var hasAccess = await CheckChatAccess(attachment.Message.Chat, userId);
                if (!hasAccess)
                    return HandleForbidden();

                // Скачиваем из MinIO
                var stream = await _storageService.DownloadAsync(attachment.FilePath);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var fileBytes = ms.ToArray();

                return File(fileBytes, attachment.ContentType, attachment.FileName);
            }
            catch (Exception ex)
            {
                return HandleError($"Ошибка при загрузке вложения: {ex.Message}");
            }
        }

        /// <summary>
        /// Отправить текстовое сообщение (без вложений)
        /// </summary>
        [HttpPost("send")]
        public async Task<ActionResult<ApiResponse<MessageDto>>> SendMessage([FromBody] SendMessageDto dto)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users
                    .Include(u => u.ProfileIcon)
                    .Include(u => u.Role)
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

                bool hasAccess = await CheckChatAccess(chat, userId);
                if (!hasAccess)
                    return HandleForbidden<MessageDto>("Нет доступа к чату");

                var message = new Message
                {
                    ChatId = dto.ChatId,
                    UserId = userId,
                    Text = dto.Text.Trim(),
                    SentAt = DateTime.Now,
                    IsEdited = false,
                    HasAttachments = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

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
                    IsMyMessage = false,
                    HasAttachments = message.HasAttachments
                };

                await _hubContext.Clients.Group($"chat-{dto.ChatId}")
                    .SendAsync("ReceiveMessage", messageDto);

                if (dto.Text.Contains("@notify") && user.RoleId != (int)Roles.Member)
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

                var timeSinceSent = DateTime.Now - message.SentAt;
                if (timeSinceSent.TotalMinutes > 15)
                    return HandleError("Сообщение можно редактировать только в течение 15 минут после отправки");

                message.Text = newText.Trim();
                message.IsEdited = true;
                message.EditedAt = DateTime.Now;

                await _context.SaveChangesAsync();

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

                if (message.UserId != userId && user?.RoleId != (int)Roles.Organizer && user?.RoleId != (int)Roles.Admin)
                    return HandleForbidden("Недостаточно прав для удаления сообщения");

                // Удаляем вложения из MinIO
                if (message.HasAttachments)
                {
                    var attachments = await _context.MessageAttachments
                        .Where(a => a.MessageId == messageId)
                        .ToListAsync();

                    foreach (var attachment in attachments)
                    {
                        await _storageService.DeleteAsync(attachment.FilePath);
                    }
                }

                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.Group($"chat-{message.ChatId}")
                    .SendAsync("MessageDeleted", messageId);

                return HandleSuccess("Сообщение удалено");
            }
            catch (Exception ex)
            {
                return HandleError("Ошибка при удалении сообщения");
            }
        }

        // --- Вспомогательные методы ---

        private async Task<bool> CheckChatAccess(Chat chat, int userId)
        {
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.RoleId, u.TeamId })
                .FirstOrDefaultAsync();

            if (user == null) return false;

            if (user.RoleId == (int)Roles.Organizer || user.RoleId == (int)Roles.Admin)
                return true;

            if (chat.TypeId == (int)ChatTypes.TeamChat)
            {
                var teamId = await _context.Teams
                    .Where(t => t.ChatId == chat.Id)
                    .Select(t => t.Id)
                    .FirstOrDefaultAsync();

                return user.TeamId == teamId;
            }

            if (chat.TypeId == (int)ChatTypes.TaskChat)
            {
                return await _context.Tasks
                    .Where(t => t.ChatId == chat.Id)
                    .AnyAsync(t => t.Team.Users.Any(u => u.Id == userId && u.RoleId == (int)Roles.Captain) ||
                                  t.AssignedToId == userId);
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
                    IsMyMessage = m.UserId == currentUserId,
                    HasAttachments = m.HasAttachments,
                    Attachments = m.MessageAttachments.Select(a => new MessageAttachmentDto
                    {
                        Id = a.Id,
                        MessageId = a.MessageId,
                        FileName = a.FileName,
                        FileSize = a.FileSize,
                        ContentType = a.ContentType,
                        FilePath = a.FilePath,
                        ThumbnailBase64 = a.Thumbnail != null ? Convert.ToBase64String(a.Thumbnail) : null,
                        UploadedAt = a.UploadedAt
                    }).ToList()
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

        private async Task ProcessNotificationFromMessageAsync(Chat chat, User sender, Message message)
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
                            $"({sender.Role.Name}) {sender.Username}",
                            sender.Id,
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
                            task.TeamId,
                            task.Id,
                            task.Title,
                            $"({sender.Role.Name}) {sender.Username}",
                            sender.RoleId,
                            message.Text.Replace("@notify", "").Trim());
                    }
                }
            }
            catch (Exception) { }
        }
    }
}