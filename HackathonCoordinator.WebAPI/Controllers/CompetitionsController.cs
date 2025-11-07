using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HackathonCoordinator.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CompetitionsController : ControllerBase
    {
        private readonly HackathonCoordinatorContext _context;

        public CompetitionsController(HackathonCoordinatorContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CompetitionDto>>> GetCompetitions()
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            var competitions = await _context.Competitions
                .Include(c => c.CreatedBy)
                .Include(c => c.Teams)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // Для организатора возвращаем все соревнования, для остальных - только активные
            if (user?.RoleId != 3) // 3 = Organizer
            {
                competitions = competitions.Where(c => c.IsActive && c.EndDate >= DateTime.Now).ToList();
            }

            return competitions.Select(c => new CompetitionDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                CreatedById = c.CreatedById,
                CreatedByUsername = c.CreatedBy.Username,
                Teams = c.Teams.Select(t => new TeamDto { Id = t.Id, Name = t.Name }).ToList()
            }).ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CompetitionDto>> GetCompetition(int id)
        {
            var competition = await _context.Competitions
                .Include(c => c.CreatedBy)
                .Include(c => c.Teams)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (competition == null) return NotFound();

            return new CompetitionDto
            {
                Id = competition.Id,
                Name = competition.Name,
                Description = competition.Description,
                StartDate = competition.StartDate,
                EndDate = competition.EndDate,
                IsActive = competition.IsActive,
                CreatedAt = competition.CreatedAt,
                CreatedById = competition.CreatedById,
                CreatedByUsername = competition.CreatedBy.Username,
                Teams = competition.Teams.Select(t => new TeamDto { Id = t.Id, Name = t.Name }).ToList()
            };
        }

        [HttpPost]
        public async Task<IActionResult> CreateCompetition([FromBody] CreateCompetitionDto dto)
        {
            var userId = GetUserId();

            var competition = new Competition
            {
                Name = dto.Name,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CreatedById = userId,
                IsActive = true
            };

            _context.Competitions.Add(competition);
            await _context.SaveChangesAsync();

            return Ok("Соревнование успешно создано");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCompetition(int id, [FromBody] CreateCompetitionDto dto)
        {
            var competition = await _context.Competitions.FindAsync(id);
            if (competition == null)
                return NotFound("Соревнование не найдено");

            // Проверяем права (только создатель может редактировать)
            var userId = GetUserId();
            if (competition.CreatedById != userId)
                return Forbid("Вы не можете редактировать это соревнование");

            competition.Name = dto.Name;
            competition.Description = dto.Description;
            competition.StartDate = dto.StartDate;
            competition.EndDate = dto.EndDate;

            await _context.SaveChangesAsync();
            return Ok("Соревнование успешно обновлено");
        }

        [HttpPost("{id}/teams")]
        public async Task<IActionResult> CreateTeamInCompetition(int id, [FromBody] CreateTeamInCompetitionDto dto)
        {
            var competition = await _context.Competitions.FindAsync(id);
            if (competition == null)
                return NotFound("Соревнование не найдено");

            // Проверяем, что пользователь - организатор
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user?.RoleId != 3) // 3 = Organizer
                return Forbid("Только организатор может создавать команды");

            var teamExists = await _context.Teams
                .AnyAsync(t => t.Name.ToLower() == dto.Name.Trim().ToLower() && t.CompetitionId == id);
            if (teamExists)
                return BadRequest("Команда с таким названием уже существует в этом соревновании");

            var team = new Team
            {
                Name = dto.Name.Trim(),
                CompetitionId = id,
                InviteCode = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            return Ok("Команда успешно создана");
        }

        private int GetUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var userId) ? userId : 0;
        }
    }
}
