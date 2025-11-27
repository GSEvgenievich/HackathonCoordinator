using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Models;
using HackathonCoordinator.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskStatus = HackathonCoordinator.WebAPI.Models.TaskStatus;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly HackathonCoordinatorContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IGitHubService _gitHubService;

    public TasksController(HackathonCoordinatorContext context, IEncryptionService encryptionService, IGitHubService gitHubService)
    {
        _context = context;
        _encryptionService = encryptionService;
        _gitHubService = gitHubService;
    }

    [HttpGet("types")]
    public async Task<ActionResult<List<TaskType>>> GetTaskTypes()
    {
        return await _context.TaskTypes.ToListAsync();
    }

    [HttpGet("statuses")]
    public async Task<ActionResult<List<TaskStatus>>> GetTaskStatuses()
    {
        return await _context.TaskStatuses.ToListAsync();
    }

    [HttpGet("{taskId}/details")]
    public async Task<ActionResult<TaskDetailsDto>> GetTaskDetails(int taskId)
    {
        var userId = GetUserId();
        var task = await _context.Tasks
            .Include(t => t.Team)
            .ThenInclude(t => t.Users)
            .Include(t => t.Type)
            .Include(t => t.Status)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return NotFound("Задача не найдена");

        var user = await _context.Users.FindAsync(userId);
        var isCaptain = user?.RoleId == 1;
        var isMyTask = task.AssignedToId == userId;

        var dto = new TaskDetailsDto
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
            AssignedToUsername = task.AssignedTo?.Username,
            Deadline = task.Deadline,
            GitHubBranchName = task.GithubBranchName,
            CreatedAt = task.CreatedAt,

            // Права доступа
            CanEdit = isCaptain,
            CanAssign = isCaptain && task.AssignedToId == null,
            CanComplete = isMyTask && task.StatusId == 2, // В процессе
            CanCancel = isMyTask && task.StatusId != 5, // Не отменена
            HasChat = task.ChatId != null,
            TaskChatId = task.ChatId
        };

        return Ok(dto);
    }

    [HttpPut("{taskId}")]
    public async Task<IActionResult> UpdateTask(int taskId, [FromBody] CreateTaskDto dto)
    {
        var userId = GetUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return Unauthorized("Пользователь не найден");

        var task = await _context.Tasks
            .Include(t => t.Team)
            .ThenInclude(t => t.Users)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return NotFound("Задача не найдена");

        // Проверяем права: только капитан может редактировать задачи
        if (user.RoleId != 1 && !task.Team.Users.Any(u => u.Id == userId && u.RoleId == 1))
            return Forbid("Только капитан команды может редактировать задачи");

        var oldBranchName = task.GithubBranchName;
        var hasExistingBranch = !string.IsNullOrEmpty(oldBranchName);

        task.Title = dto.Title.Trim();
        task.Description = dto.Description?.Trim();
        task.TypeId = dto.TypeId;
        task.AssignedToId = dto.AssignedToId;
        task.Deadline = dto.Deadline;

        await _context.SaveChangesAsync();

        if (
        !string.IsNullOrEmpty(dto.GitHubBranchName) &&
        !string.IsNullOrEmpty(task.Team.GitRepoName) &&
        !hasExistingBranch) // Ключевое условие - не было ветки до этого
        {
            try
            {
                var captain = await _context.Users
                       .FirstOrDefaultAsync(u => u.TeamId == task.Team.Id && u.RoleId == 1);

                GitHubBranchResult? branchResult = null;

                if (captain == null || string.IsNullOrEmpty(captain.GitHubAccessToken))
                {
                    branchResult = new GitHubBranchResult { Success = false, ErrorMessage = "Капитан не найден" };
                }
                else
                {
                    var decryptedToken = _encryptionService.Decrypt(captain.GitHubAccessToken);
                    branchResult = await _gitHubService.CreateBranchAsync(decryptedToken, captain.GitHubUsername, task.Team.GitRepoName, dto.GitHubBranchName);
                }

                if (!branchResult.Success)
                {
                    Console.WriteLine($"Ошибка создания ветки GitHub при редактировании: {branchResult.ErrorMessage}");
                    // Можно вернуть предупреждение, но не ошибку
                    return Ok(new
                    {
                        message = "Задача обновлена, но не удалось создать ветку GitHub",
                        warning = branchResult.ErrorMessage
                    });
                }

                task.GithubBranchName = dto.GitHubBranchName?.Trim();

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение при создании ветки GitHub при редактировании: {ex.Message}");
                // Задача все равно обновлена
                return Ok(new
                {
                    message = "Задача обновлена, но произошла ошибка при создании ветки GitHub",
                    warning = ex.Message
                });
            }
        }

        return Ok("Задача успешно обновлена");
    }

    [HttpPost("{taskId}/assign")]
    public async Task<IActionResult> AssignTask(int taskId, [FromBody] AssignTaskDto dto)
    {
        var userId = GetUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return Unauthorized("Пользователь не найден");

        var task = await _context.Tasks
            .Include(p => p.Team)
            .ThenInclude(t => t.Users)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return NotFound("Задача не найдена");

        // Проверяем права: только капитан может назначать задачи
        if (user.RoleId != 1 && !task.Team.Users.Any(u => u.Id == userId && u.RoleId == 1))
            return Forbid("Только капитан команды может назначать задачи");

        // Проверяем что пользователь состоит в команде
        var assignee = task.Team.Users.FirstOrDefault(u => u.Id == dto.UserId);
        if (assignee == null)
            return BadRequest("Пользователь не состоит в команде");

        task.AssignedToId = dto.UserId;
        task.StatusId = 2; // Переводим в "В процессе"

        await _context.SaveChangesAsync();

        return Ok("Задача успешно назначена");
    }

    [HttpPost("{taskId}/request-completion")]
    public async Task<IActionResult> RequestCompletion(int taskId)
    {
        var userId = GetUserId();
        var task = await _context.Tasks
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return NotFound("Задача не найдена");

        // Проверяем что пользователь - исполнитель задачи
        if (task.AssignedToId != userId)
            return Forbid("Только исполнитель задачи может запрашивать завершение");

        // Проверяем что задача в процессе
        if (task.StatusId != 2)
            return BadRequest("Задача должна быть в статусе 'В процессе'");

        task.StatusId = 3; // Переводим в "На проверке"

        await _context.SaveChangesAsync();

        return Ok("Запрос на завершение отправлен капитану");
    }

    [HttpPost("{taskId}/request-cancellation")]
    public async Task<IActionResult> RequestCancellation(int taskId)
    {
        var userId = GetUserId();
        var task = await _context.Tasks
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return NotFound("Задача не найдена");

        // Проверяем что пользователь - исполнитель задачи
        if (task.AssignedToId != userId)
            return Forbid("Только исполнитель задачи может запрашивать отмену");

        // Нельзя отменить уже отмененную задачу
        if (task.StatusId == 5)
            return BadRequest("Задача уже отменена");

        task.StatusId = 5; // Переводим в "Отменена"

        await _context.SaveChangesAsync();

        return Ok("Задача отменена");
    }

    [HttpDelete("{taskId}")]
    public async Task<IActionResult> DeleteTask(int taskId)
    {
        var userId = GetUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return Unauthorized("Пользователь не найден");

        var task = await _context.Tasks
            .Include(t => t.Team)
            .ThenInclude(t => t.Users)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return NotFound("Задача не найдена");

        // Проверяем права: только капитан может удалять задачи
        if (user.RoleId != 1 && !task.Team.Users.Any(u => u.Id == userId && u.RoleId == 1))
            return Forbid("Только капитан команды может удалять задачи");

        var chatId = task.ChatId;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        var chat = await _context.Chats.FindAsync(chatId);

        if (chat != null)
        {
            _context.Chats.Remove(chat);
            await _context.SaveChangesAsync();
        }

        return Ok("Задача успешно удалена");
    }

    private int GetUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var userId) ? userId : 0;
    }
}