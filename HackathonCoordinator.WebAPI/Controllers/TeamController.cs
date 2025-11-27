using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Models;
using HackathonCoordinator.WebAPI.Services;
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
        private readonly IEncryptionService _encryptionService;
        private readonly IGitHubService _gitHubService;

        public TeamsController(HackathonCoordinatorContext context, IEncryptionService encryptionService, IGitHubService gitHubService)
        {
            _context = context;
            _encryptionService = encryptionService;
            _gitHubService = gitHubService;
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

            var teamData = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    Team = u.Team,
                    Members = u.Team.Users.Select(m => new MemberDto
                    {
                        Id = m.Id,
                        Username = m.Username,
                        RoleName = m.Role.Name ?? "Участник",
                        IconName = m.ProfileIcon.Name,
                        IsCaptain = m.RoleId == 1
                    })
                    .OrderByDescending(m => m.IsCaptain)
                    .ThenBy(m => m.Username)
                    .ToList(),
                    Tasks = u.Team.Tasks
                        .OrderByDescending(t => t.CreatedAt)
                        .Select(t => new TaskDto
                        {
                            Id = t.Id,
                            TeamId = t.TeamId,
                            Title = t.Title,
                            Description = t.Description,
                            TypeId = t.TypeId,
                            TypeName = t.Type.Name,
                            StatusId = t.StatusId,
                            StatusName = t.Status.Name,
                            AssignedToId = t.AssignedToId,
                            AssignedToUsername = t.AssignedTo != null ? t.AssignedTo.Username : "Не назначена",
                            Deadline = t.Deadline,
                            CreatedAt = t.CreatedAt
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (teamData?.Team == null)
                return NotFound("Вы не состоите в команде.");

            var teamCaptainGitHub = "";

            if (teamData.Team.GitRepoName != null)
                teamCaptainGitHub = await _context.Users.Where(u => u.RoleId == 1 && u.TeamId == teamData.Team.Id).Select(t => t.GitHubUsername).FirstOrDefaultAsync();

            var teamDto = new TeamDto
            {
                Id = teamData.Team.Id,
                Name = teamData.Team.Name,
                ChatId = teamData.Team.ChatId,
                InviteCode = teamData.Team.InviteCode,
                GitHubUrl = teamData.Team.GitRepoName != null ? $"https://github.com/{teamCaptainGitHub}/{teamData.Team.GitRepoName}" : null,
                Members = teamData.Members,
                Tasks = teamData.Tasks
            };

            return Ok(teamDto);
        }

        [HttpGet("{teamId}/tasks")]
        public async Task<ActionResult<List<TaskDto>>> GetTeamTasks(int teamId)
        {
            var tasks = await _context.Tasks
                .Where(t => t.TeamId == teamId)
                .Include(t => t.AssignedTo)
                .Include(t => t.Type)
                .Include(t => t.Status)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var taskDtos = tasks.Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                TypeId = t.TypeId,
                TypeName = t.Type.Name,
                StatusId = t.StatusId,
                StatusName = t.Status.Name,
                AssignedToId = t.AssignedToId,
                AssignedToUsername = t.AssignedTo?.Username ?? "Не назначена",
                Deadline = t.Deadline,
                CreatedAt = t.CreatedAt
            }).ToList();

            return Ok(taskDtos);
        }

        [HttpPost("{teamId}/tasks")]
        public async Task<IActionResult> CreateTask(int teamId, [FromBody] CreateTaskDto dto)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return Unauthorized("Пользователь не найден");

            var team = await _context.Teams
                .Include(p => p.Users)
                .FirstOrDefaultAsync(p => p.Id == teamId);

            if (team == null)
                return NotFound("Команда не найден");

            // Проверяем права: только капитан может создавать задачи
            if (user.RoleId != 1 && !team.Users.Any(u => u.Id == userId && u.RoleId == 1))
                return Forbid("Только капитан команды может создавать задачи");

            var taskChat = new Chat
            {
                Name = $"Чат задачи: {dto.Title.Trim()}",
                TypeId = 2,
                CreatedAt = DateTime.UtcNow
            };
            _context.Chats.Add(taskChat);
            await _context.SaveChangesAsync();

            var task = new Models.Task
            {
                TeamId = teamId,
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                TypeId = dto.TypeId,
                StatusId = dto.AssignedToId != null ? 2 : 1, // В планах
                AssignedToId = dto.AssignedToId,
                Deadline = dto.Deadline,
                ChatId = taskChat.Id,
                CreatedAt = DateTime.Now
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            var welcomeMessage = new Message
            {
                ChatId = taskChat.Id,
                UserId = userId,
                Text = $"Задача \"{dto.Title.Trim()}\" создана! Обсуждайте детали выполнения здесь.",
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(welcomeMessage);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(dto.GitHubBranchName) && !string.IsNullOrEmpty(team.GitRepoName))
            {
                try
                {
                    var captain = await _context.Users
                        .FirstOrDefaultAsync(u => u.TeamId == team.Id && u.RoleId == 1);

                    GitHubBranchResult? branchResult = null;

                    if (captain == null || string.IsNullOrEmpty(captain.GitHubAccessToken))
                    {
                        branchResult = new GitHubBranchResult { Success = false, ErrorMessage = "Капитан не найден" };
                    }
                    else
                    {
                        var decryptedToken = _encryptionService.Decrypt(captain.GitHubAccessToken);
                        branchResult = await _gitHubService.CreateBranchAsync(decryptedToken, captain.GitHubUsername, team.GitRepoName, dto.GitHubBranchName);
                    }

                    if (!branchResult.Success)
                    {
                        Console.WriteLine($"Ошибка создания ветки GitHub: {branchResult.ErrorMessage}");
                        return Ok(new
                        {
                            message = "Задача создана, но не удалось создать ветку GitHub",
                            warning = branchResult.ErrorMessage
                        });
                    }

                    task.GithubBranchName = dto.GitHubBranchName?.Trim();
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Исключение при создании ветки GitHub: {ex.Message}");
                    return Ok(new
                    {
                        message = "Задача создана, но произошла ошибка при создании ветки GitHub",
                        warning = ex.Message
                    });
                }
            }

            return Ok(new { message = "Задача успешно создана", taskId = task.Id });
        }

        [HttpGet("{teamId}/competition")]
        public async Task<ActionResult<CompetitionDto>> GetTeamCompetitionAsync(int teamId)
        {
            var team = await _context.Teams
                .Include(t => t.Competition)
                    .ThenInclude(c => c.CreatedBy)
                .Include(t => t.Competition)
                    .ThenInclude(c => c.Teams)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team?.Competition == null)
                return NotFound("Соревнование для указанной команды не найдено");

            var competition = team.Competition;

            return new CompetitionDto
            {
                Id = competition.Id,
                Name = competition.Name,
                Description = competition.Description,
                StartDate = competition.StartDate,
                EndDate = competition.EndDate,
                CreatedAt = competition.CreatedAt,
                CreatedById = competition.CreatedById,
                CreatedByUsername = competition.CreatedBy.Username,
                Teams = competition.Teams.Select(t => new TeamDto
                {
                    Id = t.Id,
                    Name = t.Name
                }).ToList()
            };
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

            var teamData = await _context.Teams
                .Where(t => t.Id == teamId)
                .Select(t => new
                {
                    Team = t,
                    Members = t.Users
                        .OrderByDescending(m => m.RoleId == 1) // Капитаны первые
                        .ThenBy(m => m.Username)               // Затем по алфавиту
                        .Select(m => new MemberDto
                        {
                            Id = m.Id,
                            Username = m.Username,
                            RoleName = m.Role.Name ?? "Участник",
                            IconName = m.ProfileIcon.Name,
                            IsCaptain = m.RoleId == 1
                        })
                        .ToList(),
                    Tasks = t.Tasks
                        .OrderByDescending(task => task.CreatedAt) // Новые задачи сначала
                        .Take(100) // Ограничиваем количество
                        .Select(task => new TaskDto
                        {
                            Id = task.Id,
                            TeamId = task.TeamId,
                            Title = task.Title,
                            Description = task.Description,
                            TypeId = task.TypeId,
                            TypeName = task.Type.Name,
                            StatusId = task.StatusId,
                            StatusName = task.Status.Name,
                            AssignedToId = task.AssignedToId,
                            AssignedToUsername = task.AssignedTo != null ? task.AssignedTo.Username : "Не назначена",
                            Deadline = task.Deadline,
                            CreatedAt = task.CreatedAt
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (teamData == null)
                return NotFound("Команда не найдена.");

            var teamCaptainGitHub = await _context.Users.Where(u => u.RoleId == 1).Select(t => t.GitHubUsername).FirstOrDefaultAsync();

            var teamDto = new TeamDto
            {
                Id = teamData.Team.Id,
                Name = teamData.Team.Name,
                ChatId = teamData.Team.ChatId,
                InviteCode = teamData.Team.InviteCode,
                GitHubUrl = $"https://github.com/{teamCaptainGitHub}/{teamData.Team.GitRepoName}",
                Members = teamData.Members,
                Tasks = teamData.Tasks
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
            team.GitRepoName = null;

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
            team.GitRepoName = null;

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

            var chatId = team.ChatId;

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();

            var chat = await _context.Chats.FindAsync(chatId);

            if (chat != null)
            {
                _context.Chats.Remove(chat);
                await _context.SaveChangesAsync();
            }

            return Ok("Команда успешно удалена");
        }

        [HttpPost("{teamId}/create-github-repo")]
        public async Task<IActionResult> CreateGitHubRepository(int teamId, [FromBody] CreateGitHubRepoDto dto)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return Unauthorized("Пользователь не найден");

            var team = await _context.Teams
                .Include(t => t.Users)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
                return NotFound("Команда не найдена");

            if (user.RoleId != 1)
                return Forbid("Только капитан команды может создавать GitHub репозиторий");

            if (string.IsNullOrEmpty(user.GitHubAccessToken))
                return BadRequest("У капитана команды не привязан GitHub аккаунт");

            if (team.GitRepoName != null)
                return BadRequest("К команде уже подключен GitHub репозиторий");

            var decryptedToken = _encryptionService.Decrypt(user.GitHubAccessToken);

            var result = await _gitHubService.CreateRepositoryAsync(decryptedToken, dto.RepoName, dto.Description, dto.IsPrivate);

            if (!result.Success)
                return BadRequest(new { error = result.ErrorMessage });

            // Сохраняем в БД
            team.GitRepoName = dto.RepoName;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = result.Message,
                repoUrl = result.RepoUrl,
                repoName = result.RepoName
            });
        }

        private int GetUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("id");

            return int.TryParse(idClaim, out var userId) ? userId : 0;
        }
    }
}
