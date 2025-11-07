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
    public class ProjectsController : ControllerBase
    {
        private readonly HackathonCoordinatorContext _context;

        public ProjectsController(HackathonCoordinatorContext context)
        {
            _context = context;
        }

        // GET: api/projects/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ProjectDto>> GetProject(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound("Проект не найден");

            var projectDto = new ProjectDto
            {
                Id = project.Id,
                TeamId = project.TeamId,
                Name = project.Name,
                Description = project.Description,
                GithubRepoName = project.GithubRepoName,
                CreatedAt = project.CreatedAt ?? DateTime.UtcNow,
                ChatId = project.ChatId,
                TeamName = project.Team.Name
            };

            return Ok(projectDto);
        }

        // GET: api/projects/{id}/tasks
        [HttpGet("{id}/tasks")]
        public async Task<ActionResult<List<TaskDto>>> GetProjectTasks(int id)
        {
            var tasks = await _context.Tasks
                .Where(t => t.ProjectId == id)
                .Include(t => t.AssignedTo)
                .Include(t => t.Type)
                .Include(t => t.Status)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var taskDtos = tasks.Select(t => new TaskDto
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                Title = t.Title,
                Description = t.Description,
                TypeId = t.TypeId,
                TypeName = t.Type.Name,
                StatusId = t.StatusId,
                StatusName = t.Status.Name,
                AssignedToId = t.AssignedToId,
                AssignedToUsername = t.AssignedTo != null ? t.AssignedTo.Username : "Не назначена",
                Deadline = t.Deadline,
                CreatedAt = t.CreatedAt ?? DateTime.UtcNow
            }).ToList();

            return Ok(taskDtos);
        }

        // POST: api/teams/{teamId}/projects
        [HttpPost("~/api/teams/{teamId}/projects")]
        public async Task<IActionResult> CreateProject(int teamId, [FromBody] CreateProjectDto dto)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return Unauthorized("Пользователь не найден");

            // Проверяем, что пользователь - капитан команды или организатор
            var team = await _context.Teams
                .Include(t => t.Users)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
                return NotFound("Команда не найдена");

            if (user.RoleId != 3 && !team.Users.Any(u => u.Id == userId && u.RoleId == 1))
                return Forbid("Только капитан команды или организатор может создавать проекты");

            // Проверяем уникальность названия проекта в команде
            var projectExists = await _context.Projects
                .AnyAsync(p => p.TeamId == teamId && p.Name.ToLower() == dto.Name.Trim().ToLower());

            if (projectExists)
                return BadRequest("Проект с таким названием уже существует в команде");

            // Используем транзакцию для атомарности операций
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Сначала создаем чат для проекта
                var projectChat = new Chat
                {
                    Name = $"Чат проекта: {dto.Name.Trim()}",
                    TypeId = 2, // Предположительно тип "Проектный чат"
                    CreatedAt = DateTime.UtcNow
                };

                _context.Chats.Add(projectChat);
                await _context.SaveChangesAsync(); // Сохраняем, чтобы получить Chat.Id

                // 2. Теперь создаем проект с известным ChatId
                var project = new Project
                {
                    TeamId = teamId,
                    Name = dto.Name.Trim(),
                    Description = dto.Description?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    ChatId = projectChat.Id // Теперь ID известен
                };

                // Если нужно создать GitHub репозиторий
                if (dto.CreateGitHubRepo && !string.IsNullOrEmpty(user.GitHubAccessToken))
                {
                    // Здесь будет логика создания репозитория через GitHub API
                    // Пока просто сохраняем название
                    project.GithubRepoName = dto.GitHubRepoName;
                }

                _context.Projects.Add(project);
                await _context.SaveChangesAsync(); // Сохраняем проект

                // 3. Добавляем всех участников команды в чат проекта
                var chatMembers = team.Users.Select(member => new ChatMember
                {
                    ChatId = projectChat.Id, // Теперь это валидный ID
                    UserId = member.Id,
                    JoinedAt = DateTime.UtcNow
                }).ToList();

                _context.ChatMembers.AddRange(chatMembers);
                await _context.SaveChangesAsync();

                // Фиксируем транзакцию
                await transaction.CommitAsync();

                return Ok(new { message = "Проект успешно создан", projectId = project.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Произошла ошибка при создании проекта");
            }
        }

        // PUT: api/projects/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectDto dto)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return Unauthorized("Пользователь не найден");

            var project = await _context.Projects
                .Include(p => p.Team)
                .ThenInclude(t => t.Users)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound("Проект не найден");

            // Проверяем права: организатор или капитан команды
            if (user.RoleId != 3 && !project.Team.Users.Any(u => u.Id == userId && u.RoleId == 1))
                return Forbid("Только капитан команды или организатор может редактировать проекты");

            // Проверяем уникальность названия (кроме текущего проекта)
            var nameExists = await _context.Projects
                .AnyAsync(p => p.TeamId == project.TeamId &&
                              p.Id != id &&
                              p.Name.ToLower() == dto.Name.Trim().ToLower());

            if (nameExists)
                return BadRequest("Проект с таким названием уже существует в команде");

            project.Name = dto.Name.Trim();
            project.Description = dto.Description?.Trim();

            await _context.SaveChangesAsync();

            return Ok("Проект успешно обновлен");
        }

        // DELETE: api/projects/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return Unauthorized("Пользователь не найден");

            var project = await _context.Projects
                .Include(p => p.Team)
                .ThenInclude(t => t.Users)
                .Include(p => p.Tasks)
                .Include(p => p.Files)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound("Проект не найден");

            // Проверяем права: организатор или капитан команды
            if (user.RoleId != 3 && !project.Team.Users.Any(u => u.Id == userId && u.RoleId == 1))
                return Forbid("Только капитан команды или организатор может удалять проекты");

            // Удаляем связанные сущности
            if (project.Tasks.Any())
            {
                // Удаляем голосования задач
                var taskIds = project.Tasks.Select(t => t.Id).ToList();
                var taskVotes = await _context.TaskVotes
                    .Where(tv => taskIds.Contains(tv.TaskId))
                    .ToListAsync();
                _context.TaskVotes.RemoveRange(taskVotes);

                // Удаляем задачи
                _context.Tasks.RemoveRange(project.Tasks);
            }

            // Удаляем файлы
            if (project.Files.Any())
                _context.Files.RemoveRange(project.Files);

            // Удаляем чат проекта
            if (project.ChatId.HasValue)
            {
                var chat = await _context.Chats
                    .Include(c => c.ChatMembers)
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == project.ChatId.Value);

                if (chat != null)
                {
                    _context.ChatMembers.RemoveRange(chat.ChatMembers);
                    _context.Messages.RemoveRange(chat.Messages);
                    _context.Chats.Remove(chat);
                }
            }

            // Удаляем проект
            _context.Projects.Remove(project);

            await _context.SaveChangesAsync();

            return Ok("Проект успешно удален");
        }

        private int GetUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var userId) ? userId : 0;
        }
    }
}