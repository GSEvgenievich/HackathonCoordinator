using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Models;
using HackathonCoordinator.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HackathonCoordinator.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CompetitionsController : BaseApiController
    {
        private readonly HackathonCoordinatorContext _context;
        private readonly NotificationHelperService _notificationHelper;

        public CompetitionsController(HackathonCoordinatorContext context, NotificationHelperService notificationHelper)
        {
            _context = context;
            _notificationHelper = notificationHelper;
        }

        /// <summary>
        /// Получить список соревнований
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CompetitionDto>>>> GetCompetitions()
        {
            var competitions = await _context.Competitions
                .Include(c => c.CreatedBy)
                .Include(c => c.Teams)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var result = competitions.Select(c => new CompetitionDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                CreatedAt = c.CreatedAt,
                CreatedById = c.CreatedById,
                CreatedByUsername = c.CreatedBy.Username,
                Teams = c.Teams.Select(t => new TeamDto { Id = t.Id, Name = t.Name }).ToList()
            }).ToList();

            return HandleResult(result);
        }

        /// <summary>
        /// Получить соревнование по ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CompetitionDto>>> GetCompetition(int id)
        {
            var competition = await _context.Competitions
                .Include(c => c.CreatedBy)
                .Include(c => c.Teams)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (competition == null)
                return HandleNotFound<CompetitionDto>("Соревнование не найдено");

            var result = new CompetitionDto
            {
                Id = competition.Id,
                Name = competition.Name,
                Description = competition.Description,
                StartDate = competition.StartDate,
                EndDate = competition.EndDate,
                CreatedAt = competition.CreatedAt,
                CreatedById = competition.CreatedById,
                CreatedByUsername = competition.CreatedBy.Username,
                Teams = competition.Teams.Select(t => new TeamDto { Id = t.Id, Name = t.Name }).ToList()
            };

            return HandleResult(result);
        }

        /// <summary>
        /// Создать новое соревнование (только организатор)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse>> CreateCompetition([FromBody] CreateCompetitionDto dto)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user?.RoleId != 3)
                return HandleForbidden("Только организатор может создавать соревнования");

            var competition = new Competition
            {
                Name = dto.Name,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CreatedById = userId
            };

            _context.Competitions.Add(competition);
            await _context.SaveChangesAsync();

            // СОЗДАЕМ УВЕДОМЛЕНИЕ ДЛЯ ВСЕХ ОРГАНИЗАТОРОВ
            try
            {
                await _notificationHelper.NotifyOrganizersAboutNewCompetition(
                    competition.Id,
                    competition.Name,
                    user.Username);
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не прерываем создание соревнования
                Console.WriteLine($"Ошибка при создании уведомления: {ex.Message}");
            }

            return HandleSuccess("Соревнование успешно создано");
        }

        /// <summary>
        /// Обновить соревнование (только организатор)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse>> UpdateCompetition(int id, [FromBody] CreateCompetitionDto dto)
        {
            var competition = await _context.Competitions.FindAsync(id);
            if (competition == null)
                return HandleNotFound("Соревнование не найдено");

            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user?.RoleId != 3)
                return HandleForbidden("Только организатор может редактировать соревнования");

            competition.Name = dto.Name;
            competition.Description = dto.Description;
            competition.StartDate = dto.StartDate;
            competition.EndDate = dto.EndDate;

            await _context.SaveChangesAsync();
            return HandleSuccess("Соревнование успешно обновлено");
        }

        /// <summary>
        /// Создать команду в соревновании (только организатор)
        /// </summary>
        [HttpPost("{id}/teams")]
        public async Task<ActionResult<ApiResponse>> CreateTeamInCompetition(int id, [FromBody] CreateTeamInCompetitionDto dto)
        {
            var competition = await _context.Competitions.FindAsync(id);
            if (competition == null)
                return HandleNotFound("Соревнование не найдено");

            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user?.RoleId != 3)
                return HandleForbidden("Только организатор может создавать команды");

            var teamExists = await _context.Teams
                .AnyAsync(t => t.Name.ToLower() == dto.Name.Trim().ToLower() && t.CompetitionId == id);

            if (teamExists)
                return HandleError("Команда с таким названием уже существует в этом соревновании");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var teamChat = new Chat
                {
                    Name = $"Чат команды {dto.Name.Trim()}",
                    TypeId = 1,
                    CreatedAt = DateTime.Now
                };
                _context.Chats.Add(teamChat);
                await _context.SaveChangesAsync();

                var team = new Team
                {
                    Name = dto.Name.Trim(),
                    CompetitionId = id,
                    InviteCode = Guid.NewGuid().ToString(),
                    ChatId = teamChat.Id,
                    CreatedAt = DateTime.Now
                };

                _context.Teams.Add(team);
                await _context.SaveChangesAsync();

                var welcomeMessage = new Message
                {
                    ChatId = teamChat.Id,
                    UserId = userId,
                    Text = $"Команда \"{dto.Name.Trim()}\" создана! Добро пожаловать в общий чат команды.",
                    SentAt = DateTime.Now
                };
                _context.Messages.Add(welcomeMessage);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Уведомление: Новая команда
                try
                {
                    await _notificationHelper.NotifyOrganizersAboutNewTeam(
                        competition.Id,
                        team.Id,
                        team.Name,
                        competition.Name,
                        user.Username);
                }
                catch (Exception ex)
                {
                    // Логируем ошибку, но не прерываем создание соревнования
                    Console.WriteLine($"Ошибка при создании уведомления: {ex.Message}");
                }
                return HandleSuccess("Команда успешно создана");
            }
            catch
            {
                await transaction.RollbackAsync();
                return HandleError("Ошибка при создании команды");
            }
        }
    }
}