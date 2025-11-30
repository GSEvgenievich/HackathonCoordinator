using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Hubs;
using HackathonCoordinator.WebAPI.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace HackathonCoordinator.WebAPI.Services
{
    public class NotificationHelperService
    {
        private readonly HackathonCoordinatorContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationHelperService(HackathonCoordinatorContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Создать уведомление о новой задаче (для всех участников команды)
        /// </summary>
        public async Task NotifyTeamAboutNewTask(int taskId, int teamId, string taskTitle)
        {
            var teamMembers = await _context.Users
                .Where(u => u.TeamId == teamId)
                .ToListAsync();

            foreach (var member in teamMembers)
            {
                await CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = member.Id,
                    NotificationTypeId = 1, // Новая задача
                    Title = "Новая задача в команде",
                    Message = $"Создана новая задача: \"{taskTitle}\"",
                    RelatedEntityType = "task",
                    RelatedEntityId = taskId
                });
            }
        }

        /// <summary>
        /// Создать уведомление о назначении задачи
        /// </summary>
        public async Task NotifyTaskAssignment(int taskId, int assignedToUserId, string taskTitle)
        {
            await CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = assignedToUserId,
                NotificationTypeId = 2, // Задача назначена
                Title = "Вам назначена задача",
                Message = $"Вам назначена задача: \"{taskTitle}\"",
                RelatedEntityType = "task",
                RelatedEntityId = taskId
            });
        }

        /// <summary>
        /// Создать уведомление о завершении задачи (для капитана)
        /// </summary>
        public async Task NotifyTaskCompletion(int taskId, int captainUserId, string taskTitle, string completedBy)
        {
            await CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = captainUserId,
                NotificationTypeId = 3, // Задача завершена
                Title = "Задача завершена",
                Message = $"Задача \"{taskTitle}\" завершена участником {completedBy}. Требуется подтверждение.",
                RelatedEntityType = "task",
                RelatedEntityId = taskId
            });
        }

        /// <summary>
        /// Создать уведомление о новом участнике команды
        /// </summary>
        public async Task NotifyNewTeamMember(int teamId, int newMemberId, string newMemberName)
        {
            var teamMembers = await _context.Users
                .Where(u => u.TeamId == teamId && u.Id != newMemberId)
                .ToListAsync();

            foreach (var member in teamMembers)
            {
                await CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = member.Id,
                    NotificationTypeId = 6, // Новый участник команды
                    Title = "Новый участник в команде",
                    Message = $"К команде присоединился {newMemberName}",
                    RelatedEntityType = "team",
                    RelatedEntityId = teamId
                });
            }
        }

        public async Task NotifyOrganizersAboutNewCompetition(int competitionId, string competitionName, string createdBy)
        {
            // Получаем всех организаторов
            var organizers = await _context.Users
                .Where(u => u.RoleId == 3) // 3 = Organizer
                .ToListAsync();

            foreach (var organizer in organizers)
            {
                await CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = organizer.Id,
                    NotificationTypeId = 14,
                    Title = "Создано новое соревнование",
                    Message = $"Организатор {createdBy} создал соревнование: \"{competitionName}\"",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }
        }

        /// <summary>
        /// Общий метод создания уведомления
        /// </summary>
        private async Task CreateNotificationAsync(CreateNotificationDto dto)
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

            // Отправляем через SignalR
            await SendNotificationViaSignalR(notification);
        }

        public async Task SendNotificationViaSignalR(Notification notification)
        {
            var notificationDto = await _context.Notifications
                .Where(n => n.Id == notification.Id)
                .Include(n => n.NotificationType)
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
                    TimeAgo = GetTimeAgo(n.CreatedAt)
                })
                .FirstOrDefaultAsync();

            if (notificationDto != null)
            {
                await _hubContext.Clients.User(notification.UserId.ToString())
                    .SendAsync("ReceiveNotification", notificationDto);

                // Обновляем счетчик
                await UpdateUnreadCount(notification.UserId);
            }
        }

        public async Task UpdateUnreadCount(int userId)
        {
            var unreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            await _hubContext.Clients.User(userId.ToString())
                .SendAsync("UpdateUnreadCount", unreadCount);
        }

        private static string GetTimeAgo(DateTime date)
        {
            var timeSpan = DateTime.Now - date;
            if (timeSpan.TotalMinutes < 1) return "только что";
            if (timeSpan.TotalHours < 1) return $"{(int)timeSpan.TotalMinutes} мин назад";
            if (timeSpan.TotalDays < 1) return $"{(int)timeSpan.TotalHours} ч назад";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} дн назад";
            return date.ToString("dd.MM.yy");
        }
    }
}