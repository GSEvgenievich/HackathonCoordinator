using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Helpers;
using HackathonCoordinator.WebAPI.Hubs;
using HackathonCoordinator.WebAPI.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace HackathonCoordinator.WebAPI.Services
{
    /// <summary>
    /// Сервис для создания и отправки уведомлений
    /// </summary>
    public class NotificationHelperService
    {
        private readonly HackathonCoordinatorContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationHelperService> _logger;

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
            var memberIds = await GetTeamMemberIdsAsync(teamId);

            foreach (var memberId in memberIds)
            {
                await CreateNotificationAsync(memberId, new CreateNotificationDto
                {
                    UserId = memberId,
                    NotificationTypeId = (int)NotificationTypes.NewTask,
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
                NotificationTypeId = (int)NotificationTypes.TaskAssigned,
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
                NotificationTypeId = (int)NotificationTypes.TaskCompleted,
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
                NotificationTypeId = (int)NotificationTypes.TaskCancelled,
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
                NotificationTypeId = (int)NotificationTypes.TaskConfirmed,
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
                NotificationTypeId = (int)NotificationTypes.TaskCompletionCancelled,
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
                NotificationTypeId = (int)NotificationTypes.TaskCancelled,
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
                NotificationTypeId = (int)NotificationTypes.TaskDeadlineApproaching,
                Title = "🚨 Срок задачи истекает!",
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
                NotificationTypeId = (int)NotificationTypes.TaskDeadlineExpired,
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
                NotificationTypeId = (int)NotificationTypes.TaskDeadlineExpired,
                Title = "Срок задачи истек у участника",
                Message = $"Срок задачи «{taskTitle}» (исполнитель: {assignedToUsername}) истек {deadline:dd.MM.yyyy}",
                RelatedEntityType = "task",
                RelatedEntityId = taskId
            });
        }

        /// <summary>
        /// Создать уведомление о важном сообщении в чате задачи
        /// </summary>
        public async Task NotifyImportantTaskChatMessage(int chatId, int userId, int teamId, int taskId, string taskName, string sender, int senderRoleId, string messagePreview)
        {
            var recipientsIds = new List<int>() { userId };

            if (senderRoleId == (int)Roles.Organizer)
            {
                var captainId = await GetTeamCaptainIdAsync(teamId);
                recipientsIds.Add(captainId);
            }

            foreach (var recipientId in recipientsIds)
            {
                await CreateNotificationAsync(recipientId, new CreateNotificationDto
                {
                    UserId = recipientId,
                    NotificationTypeId = (int)NotificationTypes.ImportantTaskChatMessage,
                    Title = $"Важное сообщение в чате задачи {taskName}",
                    Message = $"{sender}: {messagePreview}",
                    RelatedEntityType = "task chat",
                    RelatedEntityId = taskId
                });
            }
        }

        // --- Методы для уведомлений о командах ---

        /// <summary>
        /// Создать уведомление о новом участнике команды
        /// </summary>
        public async Task NotifyNewTeamMember(int teamId, int newMemberId, string newMemberName)
        {
            var memberIds = await GetTeamMemberIdsExceptAsync(teamId, newMemberId);

            foreach (var memberId in memberIds)
            {
                await CreateNotificationAsync(memberId, new CreateNotificationDto
                {
                    UserId = memberId,
                    NotificationTypeId = (int)NotificationTypes.NewTeamMember,
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
                    NotificationTypeId = (int)NotificationTypes.TeamMemberLeft,
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
                NotificationTypeId = (int)NotificationTypes.MemberKickedFromTeam,
                Title = "Исключение из команды",
                Message = $"Вас исключили из команды «{teamName}»"
            });
        }

        /// <summary>
        /// Создать уведомление о новом капитане команды (для всех участников, кроме нового капитана)
        /// </summary>
        public async Task NotifyNewCaptainToTeam(int teamId, int newCaptainId, string newCaptainName, string teamName)
        {
            var memberIds = await GetTeamMemberIdsExceptAsync(teamId, newCaptainId);

            foreach (var memberId in memberIds)
            {
                await CreateNotificationAsync(memberId, new CreateNotificationDto
                {
                    UserId = memberId,
                    NotificationTypeId = (int)NotificationTypes.NewTeamCaptainAppointed,
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
                NotificationTypeId = (int)NotificationTypes.BecameTeamCaptain,
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
            var memberIds = await GetTeamMemberIdsAsync(teamId);

            foreach (var memberId in memberIds)
            {
                await CreateNotificationAsync(memberId, new CreateNotificationDto
                {
                    UserId = memberId,
                    NotificationTypeId = (int)NotificationTypes.GitHubRepoCreated,
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
        public async Task NotifyImportantTeamChatMessage(int chatId, int teamId, string sender, int senderId, string messagePreview)
        {
            var memberIds = await GetTeamMemberIdsAsync(teamId);
            memberIds.Remove(senderId);

            foreach (var memberId in memberIds)
            {
                await CreateNotificationAsync(memberId, new CreateNotificationDto
                {
                    UserId = memberId,
                    NotificationTypeId = (int)NotificationTypes.ImportantTeamChatMessage,
                    Title = "Важное сообщение в чате команды",
                    Message = $"{sender}: {messagePreview}",
                    RelatedEntityType = "team chat",
                    RelatedEntityId = teamId
                });
            }
        }

        /// <summary>
        /// Создать уведомление об удалении команды для всех участников
        /// </summary>
        public async Task NotifyTeamDisbanded(List<int> membersIds, string teamName, string deletedBy)
        {
            foreach (var memberId in membersIds)
            {
                await CreateNotificationAsync(memberId, new CreateNotificationDto
                {
                    UserId = memberId,
                    NotificationTypeId = (int)NotificationTypes.MemberKickedFromTeam,
                    Title = "Команда расформирована",
                    Message = $"Команда «{teamName}» была расформирована организатором {deletedBy}",
                    RelatedEntityType = "team",
                    RelatedEntityId = null
                });
            }
        }

        // --- Методы для уведомлений о соревнованиях ---

        /// <summary>
        /// Уведомить организаторов о новом соревновании
        /// </summary>
        public async Task NotifyOrganizersAboutNewCompetition(int competitionId, string competitionName, string createdBy)
        {
            var organizerIds = await GetOrganizerIdsAsync();

            foreach (var organizerId in organizerIds)
            {
                await CreateNotificationAsync(organizerId, new CreateNotificationDto
                {
                    UserId = organizerId,
                    NotificationTypeId = (int)NotificationTypes.NewCompetitionCreated,
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
            var organizerIds = await GetOrganizerIdsAsync();

            foreach (var organizerId in organizerIds)
            {
                await CreateNotificationAsync(organizerId, new CreateNotificationDto
                {
                    UserId = organizerId,
                    NotificationTypeId = (int)NotificationTypes.NewTeamCreated,
                    Title = "Создана новая команда",
                    Message = $"Организатор {createdBy} создал команду «{teamName}» в соревновании «{competitionName}»",
                    RelatedEntityType = "team",
                    RelatedEntityId = teamId
                });
            }
        }

        /// <summary>
        /// Уведомить организаторов об удалении соревнования
        /// </summary>
        public async Task NotifyCompetitionDeleted(int competitionId, string competitionName, string deletedBy)
        {
            var organizersIds = await GetOrganizerIdsAsync();

            foreach (var id in organizersIds)
            {
                await CreateNotificationAsync(id, new CreateNotificationDto
                {
                    UserId = id,
                    NotificationTypeId = (int)NotificationTypes.CompetitionDeleted,
                    Title = "Соревнование удалено",
                    Message = $"Соревнование \"{competitionName}\" было удалено организатором {deletedBy}",
                    RelatedEntityType = "competition",
                    RelatedEntityId = null
                });
            }
        }

        /// <summary>
        /// Уведомить организаторов об отправке соревнования в архив
        /// </summary>
        public async Task NotifyCompetitionArchived(int competitionId, string competitionName, string archivedBy)
        {
            var organizersIds = await GetOrganizerIdsAsync();

            foreach (var id in organizersIds)
            {
                await CreateNotificationAsync(id, new CreateNotificationDto
                {
                    UserId = id,
                    NotificationTypeId = (int)NotificationTypes.CompetitionArchived,
                    Title = "Соревнование архивировано",
                    Message = $"Соревнование \"{competitionName}\" было отправлено в архив организатором {archivedBy}",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }
        }

        /// <summary>
        /// Создать уведомление об удалении команды для организаторов
        /// </summary>
        public async Task NotifyOrganizersAboutTeamDeletion(int competitionId, string teamName, string competitionName, string deletedBy)
        {
            var organizerIds = await GetOrganizerIdsAsync();

            foreach (var organizerId in organizerIds)
            {
                await CreateNotificationAsync(organizerId, new CreateNotificationDto
                {
                    UserId = organizerId,
                    NotificationTypeId = (int)NotificationTypes.TeamDeleted,
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
            var participantIds = await GetCompetitionParticipantIdsAsync(competitionId);
            var organizerIds = await GetOrganizerIdsAsync();

            foreach (var organizerId in organizerIds)
            {
                await CreateNotificationAsync(organizerId, new CreateNotificationDto
                {
                    UserId = organizerId,
                    NotificationTypeId = (int)NotificationTypes.CompetitionStarted,
                    Title = "Соревнование началось",
                    Message = $"Соревнование «{competitionName}» началось.",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }

            foreach (var participantId in participantIds)
            {
                await CreateNotificationAsync(participantId, new CreateNotificationDto
                {
                    UserId = participantId,
                    NotificationTypeId = (int)NotificationTypes.CompetitionStarted,
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
            var participantIds = await GetCompetitionParticipantIdsAsync(competitionId);
            var organizerIds = await GetOrganizerIdsAsync();

            foreach (var organizerId in organizerIds)
            {
                await CreateNotificationAsync(organizerId, new CreateNotificationDto
                {
                    UserId = organizerId,
                    NotificationTypeId = (int)NotificationTypes.CompetitionEnded,
                    Title = "Соревнование завершено",
                    Message = $"Соревнование «{competitionName}» завершено.",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }

            foreach (var participantId in participantIds)
            {
                await CreateNotificationAsync(participantId, new CreateNotificationDto
                {
                    UserId = participantId,
                    NotificationTypeId = (int)NotificationTypes.CompetitionEnded,
                    Title = "Соревнование завершено",
                    Message = $"Соревнование «{competitionName}» завершено. Скоро будут объявлены результаты!",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }
        }

        /// <summary>
        /// Создать уведомление о подведении итогов соревнования
        /// </summary>
        public async Task NotifyCompetitionResultsPublished(int competitionId, string competitionName)
        {
            var participantIds = await GetCompetitionParticipantIdsAsync(competitionId);
            var organizerIds = await GetOrganizerIdsAsync();

            foreach (var organizerId in organizerIds)
            {
                await CreateNotificationAsync(organizerId, new CreateNotificationDto
                {
                    UserId = organizerId,
                    NotificationTypeId = (int)NotificationTypes.CompetitionResultsPublished,
                    Title = "Результаты соревнования опубликованы",
                    Message = $"Результаты соревнования «{competitionName}» опубликованы.",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }

            foreach (var participantId in participantIds)
            {
                await CreateNotificationAsync(participantId, new CreateNotificationDto
                {
                    UserId = participantId,
                    NotificationTypeId = (int)NotificationTypes.CompetitionResultsPublished,
                    Title = "Результаты соревнования опубликованы!",
                    Message = $"Результаты соревнования «{competitionName}» опубликованы. Перейдите на страницу соревнования для просмотра.",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }
        }

        /// <summary>
        /// Создать уведомление об обновлении результатов соревнования
        /// </summary>
        public async Task NotifyCompetitionResultsUpdated(int competitionId, string competitionName)
        {
            var participantIds = await GetCompetitionParticipantIdsAsync(competitionId);
            var organizerIds = await GetOrganizerIdsAsync();

            foreach (var organizerId in organizerIds)
            {
                await CreateNotificationAsync(organizerId, new CreateNotificationDto
                {
                    UserId = organizerId,
                    NotificationTypeId = (int)NotificationTypes.CompetitionResultsPublished,
                    Title = "Результаты соревнования обновлены",
                    Message = $"Результаты соревнования «{competitionName}» обновлены.",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }

            foreach (var participantId in participantIds)
            {
                await CreateNotificationAsync(participantId, new CreateNotificationDto
                {
                    UserId = participantId,
                    NotificationTypeId = (int)NotificationTypes.CompetitionResultsUpdated,
                    Title = "Результаты соревнования обновлены!",
                    Message = $"Результаты соревнования «{competitionName}» были обновлены. Перейдите на страницу соревнования для просмотра актуальных итогов.",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }
        }

        /// <summary>
        /// Создать уведомление о начале этапа соревнования
        /// </summary>
        public async Task NotifyStageStarted(int competitionId, int stageId, string stageName, string competitionName)
        {
            var participantIds = await GetCompetitionParticipantIdsAsync(competitionId);
            var organizerIds = await GetOrganizerIdsAsync();

            foreach (var organizerId in organizerIds)
            {
                await CreateNotificationAsync(organizerId, new CreateNotificationDto
                {
                    UserId = organizerId,
                    NotificationTypeId = (int)NotificationTypes.CompetitionResultsPublished,
                    Title = $"Этап «{stageName}» начался",
                    Message = $"Начался этап «{stageName}» соревнования «{competitionName}».",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }

            foreach (var participantId in participantIds)
            {
                await CreateNotificationAsync(participantId, new CreateNotificationDto
                {
                    UserId = participantId,
                    NotificationTypeId = (int)NotificationTypes.StageStarted,
                    Title = $"Этап «{stageName}» начался!",
                    Message = $"Начался этап «{stageName}» соревнования «{competitionName}». Удачи!",
                    RelatedEntityType = "competition",
                    RelatedEntityId = competitionId
                });
            }
        }

        // --- Общие методы уведомлений ---

        /// <summary>
        /// Создать уведомление об изменении роли пользователя
        /// </summary>
        public async Task NotifyRoleChanged(int userId, string oldRole, string newRole, string changedBy)
        {
            await CreateNotificationAsync(userId, new CreateNotificationDto
            {
                UserId = userId,
                NotificationTypeId = (int)NotificationTypes.RoleChanged,
                Title = "Изменение прав доступа",
                Message = $"Ваша роль изменена с '{oldRole}' на '{newRole}' администратором {changedBy}. Пожалуйста, перезайдите в аккаунт для применения изменений."
            });
        }

        /// <summary>
        /// Создать системное уведомление
        /// </summary>
        public async Task NotifySystemMessage(int userId, string title, string message, string? relatedEntityType = null, int? relatedEntityId = null)
        {
            await CreateNotificationAsync(userId, new CreateNotificationDto
            {
                UserId = userId,
                NotificationTypeId = (int)NotificationTypes.SystemNotification,
                Title = title,
                Message = message,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId
            });
        }

        // --- Вспомогательные методы (оптимизированные) ---

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
                        NotificationTypeId = n.NotificationTypeId,
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
        /// Получение ID участников команды
        /// </summary>
        private async Task<List<int>> GetTeamMemberIdsAsync(int teamId)
        {
            return await _context.Users
                .Where(u => u.TeamId == teamId)
                .Select(u => u.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Получение ID участников команды, кроме указанного пользователя
        /// </summary>
        private async Task<List<int>> GetTeamMemberIdsExceptAsync(int teamId, int excludeUserId)
        {
            return await _context.Users
                .Where(u => u.TeamId == teamId && u.Id != excludeUserId)
                .Select(u => u.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Получение ID капитана команды
        /// </summary>
        private async Task<int> GetTeamCaptainIdAsync(int teamId)
        {
            return await _context.Users
                .Where(u => u.TeamId == teamId && u.RoleId == (int)Roles.Captain)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Получение ID всех организаторов
        /// </summary>
        private async Task<List<int>> GetOrganizerIdsAsync()
        {
            return await _context.Users
                .Where(u => u.RoleId == (int)Roles.Organizer || u.RoleId == (int)Roles.Admin)
                .Select(u => u.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Получение ID участников соревнования
        /// </summary>
        private async Task<List<int>> GetCompetitionParticipantIdsAsync(int competitionId)
        {
            return await _context.Users
                .Where(u => u.Team != null && u.Team.CompetitionId == competitionId)
                .Select(u => u.Id)
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