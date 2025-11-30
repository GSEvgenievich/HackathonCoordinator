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
        public async Task NotifyRequestTaskCompletion(int taskId, int captainUserId, string taskTitle, string completedBy)
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
        /// Создать уведомление о завершении задачи (для капитана)
        /// </summary>
        public async Task NotifyRequestTaskCancellation(int taskId, int captainUserId, string taskTitle, string completedBy)
        {
            await CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = captainUserId,
                NotificationTypeId = 5,
                Title = "Отменить задачу",
                Message = $"Участник {completedBy} просит отменить задачу \"{taskTitle}\". Требуется подтверждение.",
                RelatedEntityType = "task",
                RelatedEntityId = taskId
            });
        }

        /// <summary>
        /// Создать уведомление о подтверждении задачи (для исполнителя)
        /// </summary>
        public async Task NotifyTaskConfirmation(int userId, string taskTitle, int taskId)
        {
            await CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = userId,
                NotificationTypeId = 4, // Задача подтверждена
                Title = "Задача подтверждена",
                Message = $"Капитан подтвердил завершение задачи: \"{taskTitle}\"",
                RelatedEntityType = "task",
                RelatedEntityId = taskId
            });
        }

        /// <summary>
        /// Создать уведомление об отмене завершения задачи (для исполнителя)
        /// </summary>
        public async Task NotifyTaskRejection(int userId, string taskTitle, int taskId)
        {
            await CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = userId,
                NotificationTypeId = 21, // Завершение задачи отменено
                Title = "Завершение задачи отменено",
                Message = $"Капитан отменил завершение задачи: \"{taskTitle}\"",
                RelatedEntityType = "task",
                RelatedEntityId = taskId
            });
        }

        /// <summary>
        /// Создать уведомление об отмене задачи (для исполнителя)
        /// </summary>
        public async Task NotifyTaskCancellation(int userId, string taskTitle, int taskId)
        {
            await CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = userId,
                NotificationTypeId = 5, // Задача отменена
                Title = "Задача подтверждена",
                Message = $"Капитан подтвердил завершение задачи: \"{taskTitle}\"",
                RelatedEntityType = "task",
                RelatedEntityId = taskId
            });
        }

        /// <summary>
        /// Создать уведомление о приближающемся дедлайне
        /// </summary>
        public async Task NotifyApproachingDeadline(int taskId, int assignedToUserId, string taskTitle, DateTime deadline)
        {
            await CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = assignedToUserId,
                NotificationTypeId = 6, // Срок задачи истекает
                Title = "Срок задачи истекает",
                Message = $"Срок выполнения задачи «{taskTitle}» истекает {deadline:dd.MM.yyyy}",
                RelatedEntityType = "task",
                RelatedEntityId = taskId
            });
        }

        /// <summary>
        /// Создать уведомление об истекшем дедлайне для исполнителя
        /// </summary>
        public async Task NotifyExpiredDeadline(int taskId, int assignedToUserId, string taskTitle, DateTime deadline)
        {
            await CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = assignedToUserId,
                NotificationTypeId = 12, // Срок задачи истек (нужно добавить в БД)
                Title = "Срок задачи истек!",
                Message = $"Срок задачи «{taskTitle}» истек {deadline:dd.MM.yyyy}. Задача просрочена!",
                RelatedEntityType = "task",
                RelatedEntityId = taskId
            });
        }

        /// <summary>
        /// Создать уведомление об истекшем дедлайне для капитана
        /// </summary>
        public async Task NotifyExpiredDeadlineToCaptain(int taskId, int captainUserId, string taskTitle, string assignedToUsername, DateTime deadline)
        {
            await CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = captainUserId,
                NotificationTypeId = 22, // Срок задачи истек у участника (нужно добавить в БД)
                Title = "Срок задачи истек у участника",
                Message = $"Срок задачи «{taskTitle}» (исполнитель: {assignedToUsername}) истек {deadline:dd.MM.yyyy}",
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
                    NotificationTypeId = 8, // Новый участник команды
                    Title = "Новый участник в команде",
                    Message = $"К команде присоединился {newMemberName}",
                    RelatedEntityType = "team",
                    RelatedEntityId = teamId
                });
            }
        }

        /// <summary>
        /// Создать уведомление о выходе участника из команды (для капитана)
        /// </summary>
        public async Task NotifyTeamMemberLeft(int teamId, int leftMemberId, string leftMemberName)
        {
            var captainId = await _context.Users
                .Where(u => u.TeamId == teamId && u.RoleId == 1)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            await CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = captainId,
                NotificationTypeId = 9, // Участник вышел из команды
                Title = "Участник покинул команду",
                Message = $"Участник {leftMemberName} покинул команду",
                RelatedEntityType = "team",
                RelatedEntityId = teamId
            });
        }

        /// <summary>
        /// Создать уведомление об исключении из команды
        /// </summary>
        public async Task NotifyMemberKicked(int kickedMemberId, string teamName)
        {
            await CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = kickedMemberId,
                NotificationTypeId = 10, // Вас выгнали из команды
                Title = "Исключение из команды",
                Message = $"Вас исключили из команды «{teamName}»"
            });
        }

        /// <summary>
        /// Создать уведомление об удалении команды для всех участников
        /// </summary>
        public async Task NotifyTeamDeleted(List<int> membersIds, string teamName, string deletedBy)
        {
            foreach (var id in membersIds)
            {
                await CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = id,
                    NotificationTypeId = 10, // Вас выгнали из команды
                    Title = "Команда удалена",
                    Message = $"Команда «{teamName}» была удалена организатором {deletedBy}"
                });
            }
        }

        /// <summary>
        /// Создать уведомление о новом капитане команды (для всех участников)
        /// </summary>
        public async Task NotifyNewCaptainToTeam(int teamId, int newCaptainId, string newCaptainName, string teamName)
        {
            var teamMembers = await _context.Users
                .Where(u => u.TeamId == teamId && u.Id != newCaptainId)
                .ToListAsync();

            foreach (var member in teamMembers)
            {
                await CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = member.Id,
                    NotificationTypeId = 11, // Был назначен новый капитан команды
                    Title = "Новый капитан команды",
                    Message = $"Участник {newCaptainName} назначен новым капитаном команды «{teamName}»",
                    RelatedEntityType = "team",
                    RelatedEntityId = teamId
                });
            }
        }

        /// <summary>
        /// Создать уведомление о назначении капитана
        /// </summary>
        public async Task NotifyCaptainAssignment(int teamId, int newCaptainId, string teamName)
        {
            await CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = newCaptainId,
                NotificationTypeId = 12, // Вы стали капитаном
                Title = "Вы стали капитаном команды",
                Message = $"Вас назначили капитаном команды «{teamName}»",
                RelatedEntityType = "team",
                RelatedEntityId = teamId
            });
        }

        // Создать уведомление о создании GitHub репозитория
        /// </summary>
        public async Task NotifyGitHubRepoCreated(int teamId, string teamName, string repoName, string repoUrl)
        {
            var teamMembers = await _context.Users
                .Where(u => u.TeamId == teamId)
                .ToListAsync();

            foreach (var member in teamMembers)
            {
                await CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = member.Id,
                    NotificationTypeId = 13, // Создан GitHub репозиторий
                    Title = "Создан GitHub репозиторий",
                    Message = $"Для команды «{teamName}» создан GitHub репозиторий: {repoName}",
                    RelatedEntityType = "team",
                    RelatedEntityId = teamId
                });
            }
        }

        /// <summary>
        /// Создать уведомление о важном сообщении в чате
        /// </summary>
        public async Task NotifyNewChatMessage(int chatId, int teamId, string captain, string messagePreview)
        {
            var teamMembers = await _context.Users
                .Where(u => u.TeamId == teamId)
                .ToListAsync();

            foreach (var member in teamMembers)
            {
                await CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = member.Id,
                    NotificationTypeId = 14,
                    Title = "Важное сообщение от капитана в чате",
                    Message = $"{captain} (капитан): {messagePreview}",
                    RelatedEntityType = "chat",
                    RelatedEntityId = chatId
                });
            }
        }

        /// <summary>
        /// Уведомить организаторов о новом соревновании
        /// </summary>
        public async Task NotifyOrganizersAboutNewCompetition(int competitionId, string competitionName, string createdBy)
        {
            // Получаем всех организаторов
            var organizersIds = await _context.Users
                .Where(u => u.RoleId == 3) // 3 = Organizer
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var id in organizersIds)
            {
                await CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = id,
                    NotificationTypeId = 16,
                    Title = "Создано новое соревнование",
                    Message = $"Организатор {createdBy} создал соревнование: \"{competitionName}\"",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }
        }

        /// <summary>
        /// Уведомить организаторов о новой команде
        /// </summary>
        public async Task NotifyOrganizersAboutNewTeam(int competitionId, int teamId, string teamName, string competitionName, string createdBy)
        {
            var organizersIds = await _context.Users
                .Where(u => u.RoleId == 3) // 3 = Organizer
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var id in organizersIds)
            {
                await CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = id,
                    NotificationTypeId = 15,
                    Title = "Создана новая команда",
                    Message = $"Организатор {createdBy} создал команду «{teamName}» в соревновании «{competitionName}»",
                    RelatedEntityType = "team",
                    RelatedEntityId = teamId
                });
            }
        }

        /// <summary>
        /// Создать уведомление об удалении команды для организаторов
        /// </summary>
        public async Task NotifyOrganizersAboutTeamDeletion(int competitionId, string teamName, string competitionName, string deletedBy)
        {
            var organizersIds = await _context.Users
                .Where(u => u.RoleId == 3) // 3 = Organizer
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var id in organizersIds)
            {
                await CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = id,
                    NotificationTypeId = 20, // Команда удалена
                    Title = "Команда удалена",
                    Message = $"Организатор {deletedBy} удалил команду «{teamName}» из соревнования «{competitionName}»",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }
        }

        /// <summary>
        /// Создать уведомление о начале соревнования
        /// </summary>
        public async Task NotifyCompetitionStarted(int competitionId, string competitionName)
        {
            var participants = await _context.Users
                .Where(u => u.Team != null && u.Team.CompetitionId == competitionId)
                .ToListAsync();

            foreach (var participant in participants)
            {
                await CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = participant.Id,
                    NotificationTypeId = 17, // Соревнование началось
                    Title = "Соревнование началось!",
                    Message = $"Соревнование «{competitionName}» началось. Удачи!",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }
        }

        /// <summary>
        /// Создать уведомление о завершении соревнования
        /// </summary>
        public async Task NotifyCompetitionEnded(int competitionId, string competitionName)
        {
            var participants = await _context.Users
                .Where(u => u.Team != null && u.Team.CompetitionId == competitionId)
                .ToListAsync();

            foreach (var participant in participants)
            {
                await CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = participant.Id,
                    NotificationTypeId = 18, // Соревнование завершено
                    Title = "Соревнование завершено",
                    Message = $"Соревнование «{competitionName}» завершено. Спасибо за участие!",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }
        }

        /// <summary>
        /// Создать системное уведомление
        /// </summary>
        public async Task NotifySystemMessage(int userId, string title, string message, string? relatedEntityType = null, int? relatedEntityId = null)
        {
            await CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = userId,
                NotificationTypeId = 19, // Системное уведомление
                Title = title,
                Message = message,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId
            });
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