// Controllers/ExportController.cs
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

                if (user?.RoleId != 3)
                    return HandleForbidden<CompetitionExportDataDto>("Только организатор может экспортировать данные");

                var competition = await _context.Competitions
                    .Include(c => c.CreatedBy)
                    .Include(c => c.Teams)
                        .ThenInclude(t => t.Users)
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

                if (competition == null)
                    return HandleNotFound<CompetitionExportDataDto>("Соревнование не найдено");

                var exportData = new CompetitionExportDataDto
                {
                    Competition = new CompetitionDto
                    {
                        Id = competition.Id,
                        Name = competition.Name,
                        Description = competition.Description,
                        StartDate = competition.StartDate,
                        EndDate = competition.EndDate,
                        CreatedByUsername = competition.CreatedBy.Username,
                        CreatedAt = competition.CreatedAt
                    },
                    Teams = competition.Teams.Select(t => new TeamExportDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        CreatedAt = t.CreatedAt,
                        Members = t.Users.Select(u => new TeamMemberDto
                        {
                            Username = u.Username,
                            Role = u.RoleId == 1 ? "Капитан" : "Участник",
                            IsCaptain = u.RoleId == 1
                        }).ToList(),
                        Tasks = t.Tasks.Select(task => new TaskExportDto
                        {
                            Title = task.Title,
                            Description = task.Description,
                            Type = task.Type.Name,
                            Status = task.Status.Name,
                            AssignedTo = task.AssignedTo?.Username,
                            Deadline = task.Deadline,
                            CreatedAt = task.CreatedAt
                        }).ToList(),
                        TeamStats = CalculateTeamStats(t)
                    }).ToList(),
                    Stats = CalculateCompetitionStats(competition),
                    SuggestedFileName = GenerateFileName(competition.Name)
                };

                return HandleResult(exportData);
            }
            catch (Exception ex)
            {
                return HandleError<CompetitionExportDataDto>($"Ошибка получения данных: {ex.Message}");
            }
        }

        private string GenerateFileName(string competitionName)
        {
            if (string.IsNullOrWhiteSpace(competitionName))
                return $"competition_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(competitionName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
            safeName = safeName.Trim().Replace("  ", " ");

            if (safeName.Length > 50)
                safeName = safeName.Substring(0, 50).Trim();

            safeName = string.IsNullOrWhiteSpace(safeName) ? "competition" : safeName;

            return $"{safeName}_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        }

        private TeamStatsDto CalculateTeamStats(Team team)
        {
            var tasks = team.Tasks ?? new List<Models.Task>();
            var totalTasks = tasks.Count;
            var completedTasks = tasks.Count(t => t.StatusId == 4);
            var inProgressTasks = tasks.Count(t => t.StatusId == 2 || t.StatusId == 3);
            var plannedTasks = tasks.Count(t => t.StatusId == 1);

            var completionPercentage = totalTasks > 0 ? (int)Math.Round((double)completedTasks / totalTasks * 100) : 0;

            return new TeamStatsDto
            {
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                InProgressTasks = inProgressTasks,
                PlannedTasks = plannedTasks,
                CompletionPercentage = completionPercentage
            };
        }

        private CompetitionStatsDto CalculateCompetitionStats(Competition competition)
        {
            var allTeamsStats = competition.Teams.Select(CalculateTeamStats).ToList();

            return new CompetitionStatsDto
            {
                TotalParticipants = competition.Teams.Sum(t => t.Users.Count),
                TotalTasks = allTeamsStats.Sum(s => s.TotalTasks),
                TotalCompletedTasks = allTeamsStats.Sum(s => s.CompletedTasks),
                TotalCompletionPercentage = allTeamsStats.Sum(s => s.TotalTasks) > 0
                    ? (int)Math.Round((double)allTeamsStats.Sum(s => s.CompletedTasks) / allTeamsStats.Sum(s => s.TotalTasks) * 100)
                    : 0,
                AverageTeamProgress = allTeamsStats.Count > 0
                    ? (int)Math.Round(allTeamsStats.Average(s => s.CompletionPercentage))
                    : 0
            };
        }
    }
}