using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
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
                .Include(u => u.Team)
                    .ThenInclude(t => t.Users)
                        .ThenInclude(m => m.Role)
                .Include(u => u.Team.Projects)
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
                GitHubUrl = team.GitHubUrl,
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
                    CreatedAt = p.CreatedAt.Value,
                    Description = p.Description,
                    GithubRepoName = p.GithubRepoName,
                    ChatId = p.ChatId
                }).ToList()
            };

            return Ok(teamDto);
        }

        [HttpGet("{teamId}")]
        public async Task<ActionResult<TeamDto>> GetTeamById(int teamId)
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("Не удалось определить пользователя.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("Пользователь не найден.");

            if (user.RoleId != 3)
                return NotFound("Только организатор имеет доступ к данным всех команд.");

            var team = await _context.Teams
                .Where(t => t.Id == teamId)
                .Include(t => t.Users)
                    .ThenInclude(u => u.ProfileIcon)
                .Include(t => t.Users)
                    .ThenInclude(u => u.Role)
                .Include(t => t.Projects)
                .FirstOrDefaultAsync();

            var teamDto = new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                InviteCode = team.InviteCode,
                GitHubUrl = team.GitHubUrl,
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
                    CreatedAt = p.CreatedAt.Value,
                    Description = p.Description,
                    GithubRepoName = p.GithubRepoName,
                    ChatId = p.ChatId
                }).ToList()
            };

            return Ok(teamDto);
        }

        [HttpGet("current/id")]
        public async Task<ActionResult<int>> GetCurrentTeamId()
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("Не удалось определить пользователя.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("Пользователь не найден.");

            if (user.TeamId == null)
                return NotFound("Вы не состоите в команде.");

            return Ok(user.TeamId);
        }

        [HttpPost("leave")]
        public async Task<IActionResult> LeaveTeam()
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("Не удалось определить пользователя.");

            var user = await _context.Users
                .Include(u => u.Team)
                .ThenInclude(t => t.Users) // Важно: включаем всех участников команды
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("Пользователь не найден.");

            if (user.TeamId == null)
                return BadRequest("Вы не состоите в команде.");

            var team = user.Team;

            // Правильно считаем количество участников ДО удаления текущего пользователя
            var membersCount = team.Users.Count;

            // Если пользователь - капитан (RoleId = 1) и в команде больше 1 участника
            if (user.RoleId == 1 && membersCount > 1)
            {
                // Находим нового капитана (первого обычного участника)
                var newCaptain = team.Users
                    .FirstOrDefault(u => u.Id != userId && u.RoleId != 1);

                if (newCaptain != null)
                {
                    newCaptain.RoleId = 1;
                }
            }
            user.TeamId = null;
            user.RoleId = 2;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Вы покинули команду." });
        }

        [HttpPost("{teamId}/assign-captain")]
        public async Task<IActionResult> AssignCaptain(int teamId, [FromBody] AssignCaptainDto dto)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return Unauthorized("Пользователь не найден");

            if (user.RoleId == 2)
                return Forbid("Только организатор или капитан может назначать капитанов");

            var team = await _context.Teams
                .Include(t => t.Users)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
                return NotFound("Команда не найдена");

            if (user.TeamId != teamId && user.RoleId != 3)
                return BadRequest("Капитан может назначить капитаном только члена своей команды");

            var newCaptain = team.Users.FirstOrDefault(u => u.Id == dto.UserId);
            if (newCaptain == null)
                return BadRequest("Участник не найден в команде");


            var currentCaptain = team.Users.FirstOrDefault(u => u.RoleId == 1);
            if (currentCaptain != null)
            {
                if (currentCaptain.Id == dto.UserId)
                    return BadRequest("Участник уже является капитаном");

                currentCaptain.RoleId = 2;
            }

            newCaptain.RoleId = 1;

            await _context.SaveChangesAsync();

            return Ok("Капитан успешно назначен");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user?.RoleId != 3) // Только организатор
                return Forbid("Только организатор может удалять команды");

            var team = await _context.Teams.FindAsync(id);
            if (team == null)
                return NotFound("Команда не найдена");

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();

            return Ok("Команда успешно удалена");
        }

        private int GetUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("id");

            return int.TryParse(idClaim, out var userId) ? userId : 0;
        }
    }
}
