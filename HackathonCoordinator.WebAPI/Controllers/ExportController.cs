using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HackathonCoordinator.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExportController : BaseApiController
    {
        private readonly HackathonCoordinatorContext _context;

        public ExportController(
            HackathonCoordinatorContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получить данные для экспорта соревнования
        /// </summary>
        [HttpGet("competition-data/{competitionId}")]
        public async Task<ActionResult<ApiResponse<CompetitionExportDataDto>>> GetCompetitionExportData(int competitionId)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user?.RoleId != (int)Roles.Organizer)
                    return HandleForbidden<CompetitionExportDataDto>("Только организатор может экспортировать данные");

                var competition = await GetCompetitionWithDetailsAsync(competitionId);
                if (competition == null)
                    return HandleNotFound<CompetitionExportDataDto>("Соревнование не найдено");

                var exportData = await CreateExportDataDtoAsync(competition);

                return HandleResult(exportData);
            }
            catch (Exception ex)
            {
                return HandleError<CompetitionExportDataDto>("Ошибка получения данных для экспорта");
            }
        }

        // --- Вспомогательные методы ---

        /// <summary>
        /// Получение соревнования с деталями для экспорта
        /// </summary>
        private async Task<Competition?> GetCompetitionWithDetailsAsync(int competitionId)
        {
            return await _context.Competitions
                .Include(c => c.CreatedBy)
                .Include(c => c.Teams)
                    .ThenInclude(t => t.Users)
                    .ThenInclude(t => t.Role)
                .Include(c => c.Teams)
                    .ThenInclude(t => t.Tasks)
                        .ThenInclude(task => task.AssignedTo)
                .Include(c => c.Teams)
                    .ThenInclude(t => t.Tasks)
                        .ThenInclude(task => task.Type)
                .Include(c => c.Teams)
                    .ThenInclude(t => t.Tasks)
                        .ThenInclude(task => task.Status)
                .FirstOrDefaultAsync(c => c.Id == competitionId);
        }

        /// <summary>
        /// Создание DTO с данными для экспорта
        /// </summary>
        private async Task<CompetitionExportDataDto> CreateExportDataDtoAsync(Competition competition)
        {
            var competitionDto = CreateCompetitionDto(competition);
            var teamExportDtos = await CreateTeamExportDtosAsync(competition);
            var competitionStats = CalculateCompetitionStats(teamExportDtos);

            return new CompetitionExportDataDto
            {
                Competition = competitionDto,
                Teams = teamExportDtos,
                Stats = competitionStats,
                SuggestedFileName = GenerateFileName(competition.Name)
            };
        }

        /// <summary>
        /// Создание DTO соревнования
        /// </summary>
        private CompetitionDto CreateCompetitionDto(Competition competition)
        {
            return new CompetitionDto
            {
                Id = competition.Id,
                Name = competition.Name,
                Description = competition.Description,
                StartDate = competition.StartDate,
                EndDate = competition.EndDate,
                CreatedByUsername = competition.CreatedBy.Username,
                CreatedAt = competition.CreatedAt
            };
        }

        /// <summary>
        /// Создание списка DTO команд для экспорта
        /// </summary>
        private async Task<List<TeamExportDto>> CreateTeamExportDtosAsync(Competition competition)
        {
            var teamExportDtos = new List<TeamExportDto>();

            foreach (var team in competition.Teams)
            {
                var teamDto = new TeamExportDto
                {
                    Id = team.Id,
                    Name = team.Name,
                    CreatedAt = team.CreatedAt,
                    Members = CreateTeamMemberDtos(team.Users),
                    Tasks = CreateTaskExportDtos(team.Tasks),
                    TeamStats = CalculateTeamStats(team.Tasks?.ToList() ?? new List<Models.Task>())
                };

                teamExportDtos.Add(teamDto);
            }

            return teamExportDtos;
        }

        /// <summary>
        /// Создание списка DTO участников команды
        /// </summary>
        private List<TeamMemberDto> CreateTeamMemberDtos(ICollection<User> users)
        {
            return users.Select(u => new TeamMemberDto
            {
                Username = u.Username,
                Role = u.Role.Name,
                IsCaptain = u.RoleId == (int)Roles.Captain
            }).ToList();
        }

        /// <summary>
        /// Создание списка DTO задач команды
        /// </summary>
        private List<TaskExportDto> CreateTaskExportDtos(ICollection<Models.Task> tasks)
        {
            return tasks.Select(task => new TaskExportDto
            {
                Title = task.Title,
                Description = task.Description,
                Type = task.Type?.Name ?? "Не указан",
                Status = task.Status?.Name ?? "Не указан",
                AssignedTo = task.AssignedTo?.Username,
                Deadline = task.Deadline,
                CreatedAt = task.CreatedAt
            }).ToList();
        }

        /// <summary>
        /// Расчет статистики команды
        /// </summary>
        private TeamStatsDto CalculateTeamStats(List<Models.Task> tasks)
        {
            var totalTasks = tasks.Count;
            var completedTasks = tasks.Count(t => t.StatusId == (int)TaskStatuses.Completed);
            var inProgressTasks = tasks.Count(t =>
                t.StatusId == (int)TaskStatuses.InProgress ||
                t.StatusId == (int)TaskStatuses.InReview);
            var plannedTasks = tasks.Count(t => t.StatusId == (int)TaskStatuses.Pending);

            var completionPercentage = totalTasks > 0
                ? (int)Math.Round((double)completedTasks / totalTasks * 100)
                : 0;

            return new TeamStatsDto
            {
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                InProgressTasks = inProgressTasks,
                PlannedTasks = plannedTasks,
                CompletionPercentage = completionPercentage
            };
        }

        /// <summary>
        /// Расчет общей статистики соревнования
        /// </summary>
        private CompetitionStatsDto CalculateCompetitionStats(List<TeamExportDto> teamExportDtos)
        {
            var allTeamStats = teamExportDtos.Select(t => t.TeamStats).ToList();
            var totalParticipants = teamExportDtos.Sum(t => t.Members.Count);
            var totalTasks = allTeamStats.Sum(s => s.TotalTasks);
            var totalCompletedTasks = allTeamStats.Sum(s => s.CompletedTasks);

            return new CompetitionStatsDto
            {
                TotalParticipants = totalParticipants,
                TotalTasks = totalTasks,
                TotalCompletedTasks = totalCompletedTasks,
                TotalCompletionPercentage = totalTasks > 0
                    ? (int)Math.Round((double)totalCompletedTasks / totalTasks * 100)
                    : 0,
                AverageTeamProgress = allTeamStats.Count > 0 && allTeamStats.All(s => s.TotalTasks > 0)
                    ? (int)Math.Round(allTeamStats.Average(s => s.CompletionPercentage))
                    : 0
            };
        }

        /// <summary>
        /// Генерация безопасного имени файла
        /// </summary>
        private string GenerateFileName(string competitionName)
        {
            if (string.IsNullOrWhiteSpace(competitionName))
                return $"competition_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(competitionName
                .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
                .ToArray());

            safeName = safeName.Trim();

            // Удаление двойных пробелов
            while (safeName.Contains("  "))
                safeName = safeName.Replace("  ", " ");

            // Ограничение длины имени
            if (safeName.Length > 50)
                safeName = safeName.Substring(0, 50).Trim();

            // Проверка на пустое имя после обработки
            if (string.IsNullOrWhiteSpace(safeName))
                safeName = "competition";

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{safeName}_export_{timestamp}.xlsx";
        }
    }
}