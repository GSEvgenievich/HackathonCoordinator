using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Task = HackathonCoordinator.WebAPI.Models.Task;
using TaskStatus = HackathonCoordinator.WebAPI.Models.TaskStatus;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly HackathonCoordinatorContext _context;

    public TasksController(HackathonCoordinatorContext context)
    {
        _context = context;
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
            .Include(t => t.Project)
            .ThenInclude(p => p.Team)
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
            ProjectId = task.ProjectId,
            Title = task.Title,
            Description = task.Description,
            TypeId = task.TypeId,
            TypeName = task.Type.Name,
            StatusId = task.StatusId,
            StatusName = task.Status.Name,
            AssignedToId = task.AssignedToId,
            AssignedToUsername = task.AssignedTo?.Username,
            Deadline = task.Deadline,
            GithubBranchName = task.GithubBranchName,
            CreatedAt = task.CreatedAt ?? DateTime.UtcNow,

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

    [HttpPost("projects/{projectId}/tasks")]
    public async Task<IActionResult> CreateTask(int projectId, [FromBody] CreateTaskDto dto)
    {
        var userId = GetUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return Unauthorized("Пользователь не найден");

        var project = await _context.Projects
            .Include(p => p.Team)
            .ThenInclude(t => t.Users)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
            return NotFound("Проект не найден");

        // Проверяем права: только капитан может создавать задачи
        if (user.RoleId != 1 && !project.Team.Users.Any(u => u.Id == userId && u.RoleId == 1))
            return Forbid("Только капитан команды может создавать задачи");

        var task = new Task
        {
            ProjectId = projectId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            TypeId = dto.TypeId,
            StatusId = 1, // В планах
            AssignedToId = dto.AssignedToId,
            Deadline = dto.Deadline,
            GithubBranchName = dto.GithubBranchName?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Задача успешно создана", taskId = task.Id });
    }

    [HttpPut("{taskId}")]
    public async Task<IActionResult> UpdateTask(int taskId, [FromBody] CreateTaskDto dto)
    {
        var userId = GetUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return Unauthorized("Пользователь не найден");

        var task = await _context.Tasks
            .Include(t => t.Project)
            .ThenInclude(p => p.Team)
            .ThenInclude(t => t.Users)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return NotFound("Задача не найдена");

        // Проверяем права: только капитан может редактировать задачи
        if (user.RoleId != 1 && !task.Project.Team.Users.Any(u => u.Id == userId && u.RoleId == 1))
            return Forbid("Только капитан команды может редактировать задачи");

        task.Title = dto.Title.Trim();
        task.Description = dto.Description?.Trim();
        task.TypeId = dto.TypeId;
        task.AssignedToId = dto.AssignedToId;
        task.Deadline = dto.Deadline;
        task.GithubBranchName = dto.GithubBranchName?.Trim();

        await _context.SaveChangesAsync();

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
            .Include(t => t.Project)
            .ThenInclude(p => p.Team)
            .ThenInclude(t => t.Users)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return NotFound("Задача не найдена");

        // Проверяем права: только капитан может назначать задачи
        if (user.RoleId != 1 && !task.Project.Team.Users.Any(u => u.Id == userId && u.RoleId == 1))
            return Forbid("Только капитан команды может назначать задачи");

        // Проверяем что пользователь состоит в команде
        var assignee = task.Project.Team.Users.FirstOrDefault(u => u.Id == dto.UserId);
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
            .Include(t => t.Project)
            .ThenInclude(p => p.Team)
            .ThenInclude(t => t.Users)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return NotFound("Задача не найдена");

        // Проверяем права: только капитан может удалять задачи
        if (user.RoleId != 1 && !task.Project.Team.Users.Any(u => u.Id == userId && u.RoleId == 1))
            return Forbid("Только капитан команды может удалять задачи");

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return Ok("Задача успешно удалена");
    }

    private int GetUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var userId) ? userId : 0;
    }
}