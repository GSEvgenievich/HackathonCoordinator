using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HackathonCoordinator.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TeamsController : ControllerBase
    {
        private readonly HackathonCoordinatorContext _context;

        public TeamsController(HackathonCoordinatorContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Название команды не может быть пустым.");

            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("Не удалось определить пользователя.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("Пользователь не найден.");

            if (user.TeamId != null)
                return BadRequest("Вы уже состоите в команде. Сначала покиньте текущую.");

            var teamExists = await _context.Teams
                .AnyAsync(t => t.Name.ToLower() == dto.Name.Trim().ToLower());
            if (teamExists)
                return BadRequest("Команда с таким названием уже существует.");

            var inviteCode = GenerateInviteCode();

            var team = new Team
            {
                Name = dto.Name.Trim(),
                InviteCode = inviteCode,
                CreatedAt = DateTime.UtcNow
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            user.TeamId = team.Id;
            user.RoleId = 1;
            await _context.SaveChangesAsync();

            return Ok("Команда успешно создана.");
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinTeam([FromBody] JoinTeamDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.InviteCode))
                return BadRequest("Код приглашения не может быть пустым.");

            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("Не удалось определить пользователя.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("Пользователь не найден.");

            if (user.TeamId != null)
                return BadRequest("Вы уже состоите в команде. Сначала покиньте текущую.");

            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.InviteCode == dto.InviteCode.Trim());
            if (team == null)
                return NotFound("Команда с таким кодом не найдена.");

            user.TeamId = team.Id;
            await _context.SaveChangesAsync();

            return Ok($"Вы успешно присоединились к команде «{team.Name}».");
        }

        [HttpGet("current")]
        public async Task<ActionResult<TeamDto>> GetCurrentTeam()
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("Не удалось определить пользователя.");

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Include(u => u.Team)
                    .ThenInclude(t => t.Users)
                        .ThenInclude(m => m.ProfileIcon)
                .Include(u => u.Team.Projects)
                .Include(u => u.Role)
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound("Пользователь не найден.");

            if (user.Team == null)
                return NotFound("Вы не состоите в команде.");

            var team = user.Team;

            var teamDto = new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                InviteCode = team.InviteCode,
                Members = team.Users.Select(m => new MemberDto
                {
                    Id = m.Id,
                    Username = m.Username,
                    RoleName = m.Role?.Name ?? "Участник",
                    IconName = m.ProfileIcon?.Name
                }).ToList(),
                Projects = team.Projects.Select(p => new ProjectDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description
                }).ToList()
            };

            return Ok(teamDto);
        }

        [HttpPost("leave")]
        public async Task<IActionResult> LeaveTeam()
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("Не удалось определить пользователя.");

            var user = await _context.Users
                .Include(u => u.Team)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("Пользователь не найден.");

            if (user.TeamId == null)
                return BadRequest("Вы не состоите в команде.");

            var team = user.Team;

            var membersCount = await _context.Users.CountAsync(u => u.TeamId == team.Id);
            if (membersCount == 1)
            {
                _context.Teams.Remove(team);
                user.TeamId = null;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Вы покинули команду, команда была удалена (в ней больше нет участников)." });
            }

            user.TeamId = null;
            await _context.SaveChangesAsync();

            return Ok($"Вы покинули команду {team.Name}.");
        }

        private int GetUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("id");

            return int.TryParse(idClaim, out var userId) ? userId : 0;
        }

        private static string GenerateInviteCode()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
