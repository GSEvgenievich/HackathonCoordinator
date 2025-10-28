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

            // Проверяем привязку GitHub если требуется
            if (dto.LinkToGitHub && string.IsNullOrEmpty(user.GitHubAccessToken))
                return BadRequest("Для привязки команды к GitHub необходимо привязать GitHub аккаунт.");

            var inviteCode = GenerateInviteCode();

            var team = new Team
            {
                Name = dto.Name.Trim(),
                InviteCode = inviteCode,
                CreatedAt = DateTime.UtcNow
            };

            if (dto.LinkToGitHub)
            {
                team.GitHubUrl = $"https://github.com/{user.GitHubUsername}";
            }

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            user.TeamId = team.Id;
            user.RoleId = 1; // Лидер команды
            await _context.SaveChangesAsync();

            return Ok(dto.LinkToGitHub
                ? "Команда успешно создана и привязана к GitHub!"
                : "Команда успешно создана.");
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
                    Description = p.Description
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

            var user= await _context.Users
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
                    newCaptain.RoleId = 1; // Назначаем нового капитана
                    user.TeamId = null; // Текущий пользователь покидает команду
                }
                else
                {
                    // Если нет других участников (маловероятно, но на всякий случай)
                    _context.Teams.Remove(team);
                    user.TeamId = null;
                }
            }
            else if (membersCount == 1)
            {
                // Если в команде только один участник - удаляем команду
                _context.Teams.Remove(team);
                user.TeamId = null;
            }
            else
            {
                // Обычный участник просто покидает команду
                user.TeamId = null;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Вы покинули команду." });
        }

        [HttpPost("transfer-leadership")]
        public async Task<IActionResult> TransferLeadership([FromBody] TransferLeadershipDto dto)
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("Не удалось определить пользователя.");

            var currentUser = await _context.Users
                .Include(u => u.Team)
                .ThenInclude(t => t.Users)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (currentUser == null || currentUser.TeamId == null)
                return NotFound("Вы не состоите в команде.");

            if (currentUser.RoleId != 1)
                return BadRequest("Только капитан может передавать права.");

            var newCaptain = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == dto.NewCaptainUserId && u.TeamId == currentUser.TeamId);

            if (newCaptain == null)
                return BadRequest("Участник не найден в вашей команде.");

            // Передаем права
            currentUser.RoleId = 2; // Становится обычным участником
            newCaptain.RoleId = 1;  // Становится капитаном

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Права капитана переданы участнику {newCaptain.Username}" });
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
