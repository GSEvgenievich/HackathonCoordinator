using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Helpers;
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

        public ExportController(HackathonCoordinatorContext context)
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

                if (user?.RoleId != (int)Roles.Organizer && user?.RoleId != (int)Roles.Admin)
                    return HandleForbidden<CompetitionExportDataDto>("Недостаточно прав для экспорта данных");

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

        /// <summary>
        /// Получение соревнования с деталями для экспорта
        /// </summary>
        private async Task<Competition?> GetCompetitionWithDetailsAsync(int competitionId)
        {
            return await _context.Competitions
                .Include(c => c.CreatedBy)
                .Include(c => c.ResultsCreatedBy)
                .Include(c => c.ResultsUpdatedBy)
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
            var results = await GetCompetitionResultsAsync(competition.Id);

            return new CompetitionExportDataDto
            {
                Competition = competitionDto,
                Teams = teamExportDtos,
                Stats = competitionStats,
                Results = results,
                SuggestedFileName = GenerateSafeFileName(competition.Name, "competition_export")
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
                CreatedAt = competition.CreatedAt,
                HasResults = competition.HasResults,
                ResultsCreatedAt = competition.ResultsCreatedAt,
                ResultsCreatedById = competition.ResultsCreatedById,
                ResultsCreatedByUsername = competition.ResultsCreatedBy?.Username,
                ResultsUpdatedAt = competition.ResultsUpdatedAt,
                ResultsUpdatedById = competition.ResultsUpdatedById,
                ResultsUpdatedByUsername = competition.ResultsUpdatedBy?.Username
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
        /// Получение результатов соревнования
        /// </summary>
        private async Task<List<TeamResultDto>> GetCompetitionResultsAsync(int competitionId)
        {
            var results = await _context.Results
                .Where(r => r.CompetitionId == competitionId)
                .Include(r => r.Team)
                .Select(r => new TeamResultDto
                {
                    TeamId = r.TeamId,
                    TeamName = r.Team.Name,
                    Place = r.Place,
                    PlaceDisplay = r.PlaceDisplay,
                    Comment = r.Comment,
                    IsSaved = true,
                    MembersCount = r.Team.Users.Count.ToString()
                })
                .ToListAsync();

            return results;
        }

        /// <summary>
        /// Генерация безопасного имени файла
        /// </summary>
        /// <param name="sourceName">Исходное название (например, имя соревнования)</param>
        /// <param name="prefix">Префикс для файла</param>
        /// <returns>Безопасное имя файла</returns>
        private string GenerateSafeFileName(string sourceName, string prefix)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
                return $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            // Список недопустимых символов для Windows
            var invalidChars = Path.GetInvalidFileNameChars();

            // Замена недопустимых символов на '_'
            var safeName = new string(sourceName
                .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
                .ToArray());

            // Удаляем специальные символы
            safeName = System.Text.RegularExpressions.Regex.Replace(safeName, @"[\""\'\`\~\@\$\%\^\&\*\(\)\=\+\{\}\[\]\|\\\/\?\<\>\:\;]", "_");

            // Удаляем двойные кавычки и другие проблемные символы
            safeName = safeName.Replace("\"", "_")
                               .Replace("'", "_")
                               .Replace("`", "_")
                               .Replace("«", "_")
                               .Replace("»", "_")
                               .Replace("„", "_")
                               .Replace("“", "_")
                               .Replace("”", "_");

            // Обрезаем пробелы в начале и конце
            safeName = safeName.Trim();

            // Заменяем множественные пробелы на один
            while (safeName.Contains("  "))
                safeName = safeName.Replace("  ", " ");

            // Заменяем пробелы на знак подчеркивания (опционально, для читаемости)
            // safeName = safeName.Replace(' ', '_');

            // Ограничиваем длину имени (максимум 50 символов + префикс)
            const int maxNameLength = 50;
            if (safeName.Length > maxNameLength)
                safeName = safeName.Substring(0, maxNameLength).Trim();

            // Если после очистки имя пустое, используем дефолтное
            if (string.IsNullOrWhiteSpace(safeName))
                safeName = "export";

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{prefix}_{safeName}_{timestamp}.xlsx";
        }
       
    }
}