using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HackathonCoordinator.WebAPI.Services
{
    /// <summary>
    /// Фоновый сервис для мониторинга соревнований и этапов
    /// </summary>
    public class CompetitionNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CompetitionNotificationService> _logger;

        public CompetitionNotificationService(
            IServiceProvider serviceProvider,
            ILogger<CompetitionNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Сервис уведомлений о соревнованиях запущен");

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckCompetitionsStartAsync();
                    await CheckCompetitionsEndAsync();
                    await CheckStagesStartAsync();

                    await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в сервисе уведомлений о соревнованиях");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Сервис уведомлений о соревнованиях остановлен");
        }

        /// <summary>
        /// Проверка начала соревнований
        /// </summary>
        private async Task CheckCompetitionsStartAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HackathonCoordinatorContext>();
            var notificationHelper = scope.ServiceProvider.GetRequiredService<NotificationHelperService>();

            var now = DateTime.Now;

            // Соревнования, которые начались, но уведомление еще не отправлено
            var startedCompetitions = await context.Competitions
                .Where(c => c.StartDate <= now &&
                           !c.IsStartNotified &&
                           !c.IsArchived)
                .ToListAsync();

            foreach (var competition in startedCompetitions)
            {
                try
                {
                    await notificationHelper.NotifyCompetitionStarted(competition.Id, competition.Name);

                    competition.IsStartNotified = true;
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Отправлено уведомление о начале соревнования {CompetitionId}: {CompetitionName}",
                        competition.Id, competition.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при отправке уведомления о начале соревнования {CompetitionId}", competition.Id);
                }
            }
        }

        /// <summary>
        /// Проверка завершения соревнований
        /// </summary>
        private async Task CheckCompetitionsEndAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HackathonCoordinatorContext>();
            var notificationHelper = scope.ServiceProvider.GetRequiredService<NotificationHelperService>();

            var now = DateTime.Now;

            // Соревнования, которые завершились, но уведомление еще не отправлено
            var endedCompetitions = await context.Competitions
                .Where(c => c.EndDate <= now &&
                           !c.IsEndNotified &&
                           !c.IsArchived)
                .ToListAsync();

            foreach (var competition in endedCompetitions)
            {
                try
                {
                    await notificationHelper.NotifyCompetitionEnded(competition.Id, competition.Name);

                    competition.IsEndNotified = true;
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Отправлено уведомление о завершении соревнования {CompetitionId}: {CompetitionName}",
                        competition.Id, competition.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при отправке уведомления о завершении соревнования {CompetitionId}", competition.Id);
                }
            }
        }

        /// <summary>
        /// Проверка начала этапов соревнований
        /// </summary>
        private async Task CheckStagesStartAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HackathonCoordinatorContext>();
            var notificationHelper = scope.ServiceProvider.GetRequiredService<NotificationHelperService>();

            var now = DateTime.Now;

            // Этапы, которые начались, но уведомление еще не отправлено
            var startedStages = await context.Stages
                .Include(s => s.Competition)
                .Where(s => s.StartTime <= now &&
                           !s.IsStartNotified)
                .ToListAsync();

            foreach (var stage in startedStages)
            {
                try
                {
                    await notificationHelper.NotifyStageStarted(stage.CompetitionId, stage.Id, stage.Name, stage.Competition.Name);

                    stage.IsStartNotified = true;
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Отправлено уведомление о начале этапа {StageId}: {StageName} в соревновании {CompetitionId}",
                        stage.Id, stage.Name, stage.CompetitionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при отправке уведомления о начале этапа {StageId}", stage.Id);
                }
            }
        }
    }
}