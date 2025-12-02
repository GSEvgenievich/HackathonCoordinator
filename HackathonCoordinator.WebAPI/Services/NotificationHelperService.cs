using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Hubs;
using HackathonCoordinator.WebAPI.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace HackathonCoordinator.WebAPI.Services
{
    /// <summary>
    /// Enum для типов уведомлений
    /// </summary>
    public enum NotificationType
    {
        NewTask = 1,
        TaskAssigned = 2,
        TaskCompleted = 3,
        TaskConfirmed = 4,
        TaskCancelled = 5,
        TaskDeadlineApproaching = 6,
        ImportantTaskChatMessage = 7,
        NewTeamMember = 8,
        TeamMemberLeft = 9,
        MemberKickedFromTeam = 10,
        NewTeamCaptainAppointed = 11,
        BecameTeamCaptain = 12,
        GitHubRepoCreated = 13,
        ImportantTeamChatMessage = 14,
        NewTeamCreated = 15,
        NewCompetitionCreated = 16,
        CompetitionStarted = 17,
        CompetitionEnded = 18,
        SystemNotification = 19,
        TeamDeleted = 20,
        TaskCompletionCancelled = 21,
        TaskDeadlineExpired = 22
    }

    /// <summary>
    /// Сервис для создания и отправки уведомлений
    /// </summary>
    public class NotificationHelperService
    {
        private readonly HackathonCoordinatorContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationHelperService> _logger;

        // Константы для ролей пользователей
        private const int CAPTAIN_ROLE_ID = 1;
        private const int MEMBER_ROLE_ID = 2;
        private const int ORGANIZER_ROLE_ID = 3;

        public NotificationHelperService(
            HackathonCoordinatorContext context,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationHelperService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        // --- Методы для уведомлений о задачах ---

        /// <summary>
        /// Создать уведомление о новой задаче для всех участников команды
        /// </summary>
        public async Task NotifyTeamAboutNewTask(int taskId, int teamId, string taskTitle)
        {
            var teamMembers = await GetTeamMembersAsync(teamId);

            foreach (var member in teamMembers)
            {
                await CreateNotificationAsync(member.Id, new CreateNotificationDto
                {
                    UserId = member.Id,
                    NotificationTypeId = (int)NotificationType.NewTask,
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
            await CreateNotificationAsync(assignedToUserId, new CreateNotificationDto
            {
                UserId = assignedToUserId,
                NotificationTypeId = (int)NotificationType.TaskAssigned,
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
            await CreateNotificationAsync(captainUserId, new CreateNotificationDto
            {
                UserId = captainUserId,
                NotificationTypeId = (int)NotificationType.TaskCompleted,
                Title = "Задача завершена",
                Message = $"Задача \"{taskTitle}\" завершена участником {completedBy}. Требуется подтверждение.",
                RelatedEntityType = "task",
                RelatedEntityId = taskId
            });
        }

        /// <summary>
        /// Создать уведомление о запросе отмены задачи (для капитана)
        /// </summary>
        public async Task NotifyRequestTaskCancellation(int taskId, int captainUserId, string taskTitle, string completedBy)
        {
            await CreateNotificationAsync(captainUserId, new CreateNotificationDto
            {
                UserId = captainUserId,
                NotificationTypeId = (int)NotificationType.TaskCancelled,
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
            await CreateNotificationAsync(userId, new CreateNotificationDto
            {
                UserId = userId,
                NotificationTypeId = (int)NotificationType.TaskConfirmed,
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
            await CreateNotificationAsync(userId, new CreateNotificationDto
            {
                UserId = userId,
                NotificationTypeId = (int)NotificationType.TaskCompletionCancelled,
                Title = "Завершение задачи отменено",
                Message = $"Капитан отменил завершение задачи: \"{taskTitle}\"",
                RelatedEntityType = "task",
                RelatedEntityId = taskId
            });
        }

        /// <summary>
        /// Создать уведомление о отмене задачи (для исполнителя)
        /// </summary>
        public async Task NotifyTaskCancellation(int userId, string taskTitle, int taskId)
        {
            await CreateNotificationAsync(userId, new CreateNotificationDto
            {
                UserId = userId,
                NotificationTypeId = (int)NotificationType.TaskCancelled,
                Title = "Задача отменена",
                Message = $"Капитан подтвердил отмену задачи: \"{taskTitle}\"",
                RelatedEntityType = "task",
                RelatedEntityId = taskId
            });
        }

        /// <summary>
        /// Создать уведомление о приближающемся дедлайне
        /// </summary>
        public async Task NotifyApproachingDeadline(int taskId, int assignedToUserId, string taskTitle, DateTime deadline)
        {
            await CreateNotificationAsync(assignedToUserId, new CreateNotificationDto
            {
                UserId = assignedToUserId,
                NotificationTypeId = (int)NotificationType.TaskDeadlineApproaching,
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
            await CreateNotificationAsync(assignedToUserId, new CreateNotificationDto
            {
                UserId = assignedToUserId,
                NotificationTypeId = (int)NotificationType.TaskDeadlineExpired,
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
            await CreateNotificationAsync(captainUserId, new CreateNotificationDto
            {
                UserId = captainUserId,
                NotificationTypeId = (int)NotificationType.TaskDeadlineExpired,
                Title = "Срок задачи истек у участника",
                Message = $"Срок задачи «{taskTitle}» (исполнитель: {assignedToUsername}) истек {deadline:dd.MM.yyyy}",
                RelatedEntityType = "task",
                RelatedEntityId = taskId
            });
        }

        /// <summary>
        /// Создать уведомление о важном сообщении в чате задачи
        /// </summary>
        public async Task NotifyImportantTaskChatMessage(int chatId, int userId, int taskId, string taskName, string captain, string messagePreview)
        {
            await CreateNotificationAsync(userId, new CreateNotificationDto
            {
                UserId = userId,
                NotificationTypeId = (int)NotificationType.ImportantTaskChatMessage,
                Title = $"Важное сообщение от капитана в чате задачи {taskName}",
                Message = $"{captain} (капитан): {messagePreview}",
                RelatedEntityType = "task chat",
                RelatedEntityId = taskId
            });
        }

        // --- Методы для уведомлений о командах ---

        /// <summary>
        /// Создать уведомление о новом участнике команды
        /// </summary>
        public async Task NotifyNewTeamMember(int teamId, int newMemberId, string newMemberName)
        {
            var teamMembers = await GetTeamMembersExceptAsync(teamId, newMemberId);

            foreach (var member in teamMembers)
            {
                await CreateNotificationAsync(member.Id, new CreateNotificationDto
                {
                    UserId = member.Id,
                    NotificationTypeId = (int)NotificationType.NewTeamMember,
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
            var captainId = await GetTeamCaptainIdAsync(teamId);

            if (captainId > 0)
            {
                await CreateNotificationAsync(captainId, new CreateNotificationDto
                {
                    UserId = captainId,
                    NotificationTypeId = (int)NotificationType.TeamMemberLeft,
                    Title = "Участник покинул команду",
                    Message = $"Участник {leftMemberName} покинул команду",
                    RelatedEntityType = "team",
                    RelatedEntityId = teamId
                });
            }
        }

        /// <summary>
        /// Создать уведомление об исключении из команды
        /// </summary>
        public async Task NotifyMemberKicked(int kickedMemberId, string teamName)
        {
            await CreateNotificationAsync(kickedMemberId, new CreateNotificationDto
            {
                UserId = kickedMemberId,
                NotificationTypeId = (int)NotificationType.MemberKickedFromTeam,
                Title = "Исключение из команды",
                Message = $"Вас исключили из команды «{teamName}»"
            });
        }

        /// <summary>
        /// Создать уведомление о новом капитане команды (для всех участников, кроме нового капитана)
        /// </summary>
        public async Task NotifyNewCaptainToTeam(int teamId, int newCaptainId, string newCaptainName, string teamName)
        {
            var teamMembers = await GetTeamMembersExceptAsync(teamId, newCaptainId);

            foreach (var member in teamMembers)
            {
                await CreateNotificationAsync(member.Id, new CreateNotificationDto
                {
                    UserId = member.Id,
                    NotificationTypeId = (int)NotificationType.NewTeamCaptainAppointed,
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
            await CreateNotificationAsync(newCaptainId, new CreateNotificationDto
            {
                UserId = newCaptainId,
                NotificationTypeId = (int)NotificationType.BecameTeamCaptain,
                Title = "Вы стали капитаном команды",
                Message = $"Вас назначили капитаном команды «{teamName}»",
                RelatedEntityType = "team",
                RelatedEntityId = teamId
            });
        }

        /// <summary>
        /// Создать уведомление о создании GitHub репозитория для команды
        /// </summary>
        public async Task NotifyGitHubRepoCreated(int teamId, string teamName, string repoName, string repoUrl)
        {
            var teamMembers = await GetTeamMembersAsync(teamId);

            foreach (var member in teamMembers)
            {
                await CreateNotificationAsync(member.Id, new CreateNotificationDto
                {
                    UserId = member.Id,
                    NotificationTypeId = (int)NotificationType.GitHubRepoCreated,
                    Title = "Создан GitHub репозиторий",
                    Message = $"Для команды «{teamName}» создан GitHub репозиторий: {repoName}",
                    RelatedEntityType = "team",
                    RelatedEntityId = teamId
                });
            }
        }

        /// <summary>
        /// Создать уведомление о важном сообщении в чате команды
        /// </summary>
        public async Task NotifyImportantTeamChatMessage(int chatId, int teamId, string captain, string messagePreview)
        {
            var teamMembers = await GetTeamMembersAsync(teamId);

            foreach (var member in teamMembers)
            {
                await CreateNotificationAsync(member.Id, new CreateNotificationDto
                {
                    UserId = member.Id,
                    NotificationTypeId = (int)NotificationType.ImportantTeamChatMessage,
                    Title = "Важное сообщение от капитана в чате команды",
                    Message = $"{captain} (капитан): {messagePreview}",
                    RelatedEntityType = "team chat",
                    RelatedEntityId = teamId
                });
            }
        }

        /// <summary>
        /// Создать уведомление об удалении команды для всех участников
        /// </summary>
        public async Task NotifyTeamDeleted(List<int> membersIds, string teamName, string deletedBy)
        {
            foreach (var id in membersIds)
            {
                await CreateNotificationAsync(id, new CreateNotificationDto
                {
                    UserId = id,
                    NotificationTypeId = (int)NotificationType.MemberKickedFromTeam, // Используем существующий тип
                    Title = "Команда удалена",
                    Message = $"Команда «{teamName}» была удалена организатором {deletedBy}"
                });
            }
        }

        // --- Методы для уведомлений о соревнованиях ---

        /// <summary>
        /// Уведомить организаторов о новом соревновании
        /// </summary>
        public async Task NotifyOrganizersAboutNewCompetition(int competitionId, string competitionName, string createdBy)
        {
            var organizersIds = await GetOrganizerIdsAsync();

            foreach (var id in organizersIds)
            {
                await CreateNotificationAsync(id, new CreateNotificationDto
                {
                    UserId = id,
                    NotificationTypeId = (int)NotificationType.NewCompetitionCreated,
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
            var organizersIds = await GetOrganizerIdsAsync();

            foreach (var id in organizersIds)
            {
                await CreateNotificationAsync(id, new CreateNotificationDto
                {
                    UserId = id,
                    NotificationTypeId = (int)NotificationType.NewTeamCreated,
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
            var organizersIds = await GetOrganizerIdsAsync();

            foreach (var id in organizersIds)
            {
                await CreateNotificationAsync(id, new CreateNotificationDto
                {
                    UserId = id,
                    NotificationTypeId = (int)NotificationType.TeamDeleted,
                    Title = "Команда удалена",
                    Message = $"Организатор {deletedBy} удалил команду «{teamName}» из соревнования «{competitionName}»",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }
        }

        /// <summary>
        /// Создать уведомление о начале соревнования для всех участников
        /// </summary>
        public async Task NotifyCompetitionStarted(int competitionId, string competitionName)
        {
            var participants = await GetCompetitionParticipantsAsync(competitionId);

            foreach (var participant in participants)
            {
                await CreateNotificationAsync(participant.Id, new CreateNotificationDto
                {
                    UserId = participant.Id,
                    NotificationTypeId = (int)NotificationType.CompetitionStarted,
                    Title = "Соревнование началось!",
                    Message = $"Соревнование «{competitionName}» началось. Удачи!",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }
        }

        /// <summary>
        /// Создать уведомление о завершении соревнования для всех участников
        /// </summary>
        public async Task NotifyCompetitionEnded(int competitionId, string competitionName)
        {
            var participants = await GetCompetitionParticipantsAsync(competitionId);

            foreach (var participant in participants)
            {
                await CreateNotificationAsync(participant.Id, new CreateNotificationDto
                {
                    UserId = participant.Id,
                    NotificationTypeId = (int)NotificationType.CompetitionEnded,
                    Title = "Соревнование завершено",
                    Message = $"Соревнование «{competitionName}» завершено. Спасибо за участие!",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }
        }

        // --- Общие методы уведомлений ---

        /// <summary>
        /// Создать системное уведомление
        /// </summary>
        public async Task NotifySystemMessage(int userId, string title, string message, string? relatedEntityType = null, int? relatedEntityId = null)
        {
            await CreateNotificationAsync(userId, new CreateNotificationDto
            {
                UserId = userId,
                NotificationTypeId = (int)NotificationType.SystemNotification,
                Title = title,
                Message = message,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId
            });
        }

        // --- Вспомогательные методы ---

        /// <summary>
        /// Общий метод создания уведомления
        /// </summary>
        private async Task CreateNotificationAsync(int userId, CreateNotificationDto dto)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
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

                await SendNotificationViaSignalR(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании уведомления для пользователя {UserId}", userId);
            }
        }

        /// <summary>
        /// Отправка уведомления через SignalR
        /// </summary>
        public async Task SendNotificationViaSignalR(Notification notification)
        {
            try
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

                    await UpdateUnreadCount(notification.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомления через SignalR");
            }
        }

        /// <summary>
        /// Обновление счетчика непрочитанных уведомлений
        /// </summary>
        public async Task UpdateUnreadCount(int userId)
        {
            try
            {
                var unreadCount = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("UpdateUnreadCount", unreadCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении счетчика уведомлений для пользователя {UserId}", userId);
            }
        }

        /// <summary>
        /// Получение участников команды
        /// </summary>
        private async Task<List<User>> GetTeamMembersAsync(int teamId)
        {
            return await _context.Users
                .Where(u => u.TeamId == teamId)
                .ToListAsync();
        }

        /// <summary>
        /// Получение участников команды, кроме указанного пользователя
        /// </summary>
        private async Task<List<User>> GetTeamMembersExceptAsync(int teamId, int excludeUserId)
        {
            return await _context.Users
                .Where(u => u.TeamId == teamId && u.Id != excludeUserId)
                .ToListAsync();
        }

        /// <summary>
        /// Получение ID капитана команды
        /// </summary>
        private async Task<int> GetTeamCaptainIdAsync(int teamId)
        {
            return await _context.Users
                .Where(u => u.TeamId == teamId && u.RoleId == CAPTAIN_ROLE_ID)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Получение ID всех организаторов
        /// </summary>
        private async Task<List<int>> GetOrganizerIdsAsync()
        {
            return await _context.Users
                .Where(u => u.RoleId == ORGANIZER_ROLE_ID)
                .Select(u => u.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Получение участников соревнования
        /// </summary>
        private async Task<List<User>> GetCompetitionParticipantsAsync(int competitionId)
        {
            return await _context.Users
                .Where(u => u.Team != null && u.Team.CompetitionId == competitionId)
                .ToListAsync();
        }

        /// <summary>
        /// Форматирование времени "сколько времени назад"
        /// </summary>
        private static string GetTimeAgo(DateTime date)
        {
            var timeSpan = DateTime.Now - date;

            if (timeSpan.TotalMinutes < 1)
                return "только что";
            if (timeSpan.TotalHours < 1)
                return $"{(int)timeSpan.TotalMinutes} мин назад";
            if (timeSpan.TotalDays < 1)
                return $"{(int)timeSpan.TotalHours} ч назад";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} дн назад";

            return date.ToString("dd.MM.yy");
        }
    }
}