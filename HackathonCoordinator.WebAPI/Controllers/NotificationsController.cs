using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Hubs;
using HackathonCoordinator.WebAPI.Models;
using HackathonCoordinator.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HackathonCoordinator.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : BaseApiController
    {
        private readonly HackathonCoordinatorContext _context;
        private readonly NotificationHelperService _notificationHelper;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationsController(HackathonCoordinatorContext context, IHubContext<NotificationHub> hubContext, NotificationHelperService notificationHelper)
        {
            _notificationHelper = notificationHelper;
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Получить уведомления пользователя
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetUserNotifications()
        {
            try
            {
                var userId = GetUserId();

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .Include(n => n.NotificationType)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(50)
                    .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        UserId = n.UserId,
                        Title = n.Title,
                        Message = n.Message,
                        TypeName = n.NotificationType.Name,
                        TypeIcon = n.NotificationType.Icon,
                        Category = n.NotificationType.Category,
                        RelatedEntityType = n.RelatedEntityType,
                        RelatedEntityId = n.RelatedEntityId,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt,
                        ReadAt = n.ReadAt,
                        TimeAgo = GetTimeAgo(n.CreatedAt)
                    })
                    .ToListAsync();

                return HandleResult(notifications);
            }
            catch (DbUpdateException ex)
            {
                return HandleError<List<NotificationDto>>("Ошибка базы данных при получении уведомлений");
            }
            catch (InvalidOperationException ex)
            {
                return HandleUnauthorized<List<NotificationDto>>("Пользователь не найден");
            }
            catch (Exception ex)
            {
                return HandleError<List<NotificationDto>>("Внутренняя ошибка сервера при получении уведомлений");
            }
        }

        /// <summary>
        /// Получить количество непрочитанных уведомлений
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount()
        {
            try
            {
                var userId = GetUserId();

                var count = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                return HandleResult(count);
            }
            catch (DbUpdateException ex)
            {
                return HandleError<int>("Ошибка базы данных при получении количества уведомлений");
            }
            catch (InvalidOperationException ex)
            {
                return HandleUnauthorized<int>("Пользователь не найден");
            }
            catch (Exception ex)
            {
                return HandleError<int>("Внутренняя ошибка сервера при получении количества уведомлений");
            }
        }

        /// <summary>
        /// Получить статистику по уведомлениям
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse<NotificationStatsDto>>> GetNotificationStats()
        {
            try
            {
                var userId = GetUserId();

                var stats = new NotificationStatsDto
                {
                    TotalCount = await _context.Notifications.CountAsync(n => n.UserId == userId),
                    UnreadCount = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead),
                    TeamNotifications = await _context.Notifications
                        .CountAsync(n => n.UserId == userId && n.NotificationType.Category == "team"),
                    TaskNotifications = await _context.Notifications
                        .CountAsync(n => n.UserId == userId && n.NotificationType.Category == "task"),
                    SystemNotifications = await _context.Notifications
                        .CountAsync(n => n.UserId == userId && n.NotificationType.Category == "system")
                };

                return HandleResult(stats);
            }
            catch (DbUpdateException ex)
            {
                return HandleError<NotificationStatsDto>("Ошибка базы данных при получении статистики");
            }
            catch (InvalidOperationException ex)
            {
                return HandleUnauthorized<NotificationStatsDto>("Пользователь не найден");
            }
            catch (Exception ex)
            {
                return HandleError<NotificationStatsDto>("Внутренняя ошибка сервера при получении статистики");
            }
        }

        /// <summary>
        /// Отметить уведомление как прочитанное
        /// </summary>
        [HttpPut("{id}/read")]
        public async Task<ActionResult<ApiResponse>> MarkAsRead(int id)
        {
            try
            {
                var userId = GetUserId();
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

                if (notification == null)
                    return HandleNotFound("Уведомление не найдено");

                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Обновляем счетчик через SignalR
                    await _notificationHelper.UpdateUnreadCount(userId);
                }

                return HandleSuccess("Уведомление отмечено как прочитанное");
            }
            catch (DbUpdateException ex)
            {
                return HandleError("Ошибка базы данных при обновлении уведомления");
            }
            catch (InvalidOperationException ex)
            {
                return HandleUnauthorized("Пользователь не найден");
            }
            catch (Exception ex)
            {
                return HandleError("Внутренняя ошибка сервера при обновлении уведомления");
            }
        }

        /// <summary>
        /// Отметить все уведомления как прочитанные
        /// </summary>
        [HttpPut("mark-all-read")]
        public async Task<ActionResult<ApiResponse>> MarkAllAsRead()
        {
            try
            {
                var userId = GetUserId();

                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                // Обновляем счетчик через SignalR
                await _notificationHelper.UpdateUnreadCount(userId);

                return HandleSuccess("Все уведомления отмечены как прочитанные");
            }
            catch (DbUpdateException ex)
            {
                return HandleError("Ошибка базы данных при массовом обновлении уведомлений");
            }
            catch (InvalidOperationException ex)
            {
                return HandleUnauthorized("Пользователь не найден");
            }
            catch (Exception ex)
            {
                return HandleError("Внутренняя ошибка сервера при массовом обновлении уведомлений");
            }
        }

        /// <summary>
        /// Удалить уведомление
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteNotification(int id)
        {
            try
            {
                var userId = GetUserId();
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

                if (notification == null)
                    return HandleNotFound("Уведомление не найдено");

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                // Обновляем счетчик через SignalR
                await _notificationHelper.UpdateUnreadCount(userId);

                return HandleSuccess("Уведомление удалено");
            }
            catch (DbUpdateException ex)
            {
                return HandleError("Ошибка базы данных при удалении уведомления");
            }
            catch (InvalidOperationException ex)
            {
                return HandleUnauthorized("Пользователь не найден");
            }
            catch (Exception ex)
            {
                return HandleError("Внутренняя ошибка сервера при удалении уведомления");
            }
        }

        /// <summary>
        /// Создать уведомление (для внутреннего использования)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse>> CreateNotification([FromBody] CreateNotificationDto dto)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = dto.UserId,
                    NotificationTypeId = dto.NotificationTypeId,
                    Title = dto.Title,
                    Message = dto.Message,
                    RelatedEntityType = dto.RelatedEntityType,
                    RelatedEntityId = dto.RelatedEntityId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Отправляем уведомление через SignalR
                await _notificationHelper.SendNotificationViaSignalR(notification);

                return HandleSuccess("Уведомление создано");
            }
            catch (DbUpdateException ex)
            {
                return HandleError("Ошибка базы данных при создании уведомления");
            }
            catch (ArgumentNullException ex)
            {
                return HandleError("Некорректные данные для создания уведомления");
            }
            catch (Exception ex)
            {
                return HandleError("Внутренняя ошибка сервера при создании уведомления");
            }
        }

        private static string GetTimeAgo(DateTime date)
        {
            try
            {
                var timeSpan = DateTime.Now - date;

                if (timeSpan.TotalMinutes < 1) return "только что";
                if (timeSpan.TotalHours < 1) return $"{(int)timeSpan.TotalMinutes} мин назад";
                if (timeSpan.TotalDays < 1) return $"{(int)timeSpan.TotalHours} ч назад";
                if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} дн назад";

                return date.ToString("dd.MM.yy");
            }
            catch
            {
                return date.ToString("dd.MM.yy");
            }
        }
    }
}