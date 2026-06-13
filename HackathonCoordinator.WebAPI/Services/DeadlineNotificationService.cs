using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HackathonCoordinator.WebAPI.Services
{
    /// <summary>
    /// Фоновый сервис для мониторинга дедлайнов задач и отправки уведомлений
    /// </summary>
    public class DeadlineNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DeadlineNotificationService> _logger;

        public DeadlineNotificationService(
            IServiceProvider serviceProvider,
            ILogger<DeadlineNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Сервис уведомлений о дедлайнах запущен");

            // Начальная задержка перед первой проверкой
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckExpiredDeadlinesAsync();
                    await CheckApproachingDeadlinesAsync();

                    // Проверка каждые 15 минут
                    await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в сервисе уведомлений о дедлайнах");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Сервис уведомлений о дедлайнах остановлен");
        }

        /// <summary>
        /// Проверка просроченных дедлайнов
        /// </summary>
        private async Task CheckExpiredDeadlinesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HackathonCoordinatorContext>();
            var notificationHelper = scope.ServiceProvider.GetRequiredService<NotificationHelperService>();

            var now = DateTime.Now;

            // Поиск просроченных задач, о которых еще не уведомляли
            var expiredTasks = await context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.Team)
                .ThenInclude(t => t.Users.Where(u => u.RoleId == (int)Roles.Captain)) // Только капитаны
                .Where(t => t.Deadline.HasValue &&
                           t.Deadline.Value < now &&
                           t.StatusId != 4 && t.StatusId != 5 && // Не завершены и не отменены
                           !t.IsDeadlineNotified) // Еще не уведомляли
                .ToListAsync();

            foreach (var task in expiredTasks)
            {
                try
                {
                    // Уведомление исполнителя
                    if (task.AssignedToId.HasValue)
                    {
                        await notificationHelper.NotifyExpiredDeadline(
                            task.Id,
                            task.AssignedToId.Value,
                            task.Title,
                            task.Deadline.Value);
                    }

                    // Уведомление капитана команды
                    var captain = task.Team.Users.FirstOrDefault(u => u.RoleId == (int)Roles.Captain);
                    if (captain != null)
                    {
                        await notificationHelper.NotifyExpiredDeadlineToCaptain(
                            task.Id,
                            captain.Id,
                            task.Title,
                            task.AssignedTo?.Username ?? "Не назначен",
                            task.Deadline.Value);
                    }

                    // Помечаем задачу как уведомленную
                    task.IsDeadlineNotified = true;
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Отправлены уведомления об истекшем дедлайне для задачи {TaskId}", task.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при отправке уведомления для задачи {TaskId}", task.Id);
                }
            }
        }

        /// <summary>
        /// Проверка приближающихся дедлайнов (за 1 час)
        /// </summary>
        private async Task CheckApproachingDeadlinesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HackathonCoordinatorContext>();
            var notificationHelper = scope.ServiceProvider.GetRequiredService<NotificationHelperService>();

            var approachingDeadline = DateTime.Now.AddHours(1); // За 1 час
            var approachingTasks = await context.Tasks
                .Include(t => t.AssignedTo)
                .Where(t => t.Deadline.HasValue &&
                           t.Deadline.Value.Date == approachingDeadline.Date &&
                           t.StatusId != 4 && t.StatusId != 5 && // Не завершены и не отменены
                           !t.IsDeadlineApproachNotified) // Еще не уведомляли о приближении
                .ToListAsync();

            foreach (var task in approachingTasks)
            {
                try
                {
                    if (task.AssignedToId.HasValue)
                    {
                        await notificationHelper.NotifyApproachingDeadline(
                            task.Id,
                            task.AssignedToId.Value,
                            task.Title,
                            task.Deadline.Value);

                        // Помечаем задачу как уведомленную о приближении
                        task.IsDeadlineApproachNotified = true;
                        await context.SaveChangesAsync();

                        _logger.LogInformation("Отправлено уведомление о приближающемся дедлайне для задачи {TaskId}", task.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при отправке уведомления о приближающемся дедлайне для задачи {TaskId}", task.Id);
                }
            }
        }
    }
}