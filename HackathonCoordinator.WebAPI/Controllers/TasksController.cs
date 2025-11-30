using HackathonCoordinator.WebAPI;
using HackathonCoordinator.WebAPI.Controllers;
using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Models;
using HackathonCoordinator.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskStatus = HackathonCoordinator.WebAPI.Models.TaskStatus;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TasksController : BaseApiController
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

    /// <summary>
    /// Получить типы задач
    /// </summary>
    [HttpGet("types")]
    public async Task<ActionResult<ApiResponse<List<TaskType>>>> GetTaskTypes()
    {
        var types = await _context.TaskTypes.ToListAsync();
        return HandleResult(types);
    }

    /// <summary>
    /// Получить статусы задач
    /// </summary>
    [HttpGet("statuses")]
    public async Task<ActionResult<ApiResponse<List<TaskStatus>>>> GetTaskStatuses()
    {
        var statuses = await _context.TaskStatuses.ToListAsync();
        return HandleResult(statuses);
    }

    /// <summary>
    /// Получить список своих задач
    /// </summary>
    [HttpGet("my/ids")]
    public async Task<ActionResult<ApiResponse<List<int>>>> GetUserTasksIds()
    {
        var userId = GetUserId();

        var tasksIds = await _context.Tasks
            .Where(t => t.AssignedToId == userId)
            .Select(t => t.Id)
            .ToListAsync();

        return HandleResult(tasksIds);
    }

    /// <summary>
    /// Получить детальную информацию о задаче
    /// </summary>
    [HttpGet("{taskId}/details")]
    public async Task<ActionResult<ApiResponse<TaskDetailsDto>>> GetTaskDetails(int taskId)
    {
        var task = await _context.Tasks
            .Include(t => t.Team)
            .ThenInclude(t => t.Users)
            .Include(t => t.Type)
            .Include(t => t.Status)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return HandleNotFound<TaskDetailsDto>("Задача не найдена");

        var userId = GetUserId();
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
            CanEdit = isCaptain,
            CanAssign = isCaptain && task.AssignedToId == null,
            CanComplete = isMyTask && task.StatusId == 2,
            CanCancel = isMyTask && task.StatusId != 5,
            HasChat = task.ChatId != null,
            TaskChatId = task.ChatId
        };

        return HandleResult(dto);
    }

    /// <summary>
    /// Обновить задачу
    /// </summary>
    [HttpPut("{taskId}")]
    public async Task<ActionResult<ApiResponse>> UpdateTask(int taskId, [FromBody] CreateTaskDto dto)
    {
        var userId = GetUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return HandleUnauthorized("Пользователь не найден");

        var task = await _context.Tasks
            .Include(t => t.Team)
            .ThenInclude(t => t.Users)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return HandleNotFound("Задача не найдена");

        if (user.RoleId != 1 && !task.Team.Users.Any(u => u.Id == userId && u.RoleId == 1))
            return HandleForbidden("Только капитан команды может редактировать задачи");

        var oldBranchName = task.GithubBranchName;
        var hasExistingBranch = !string.IsNullOrEmpty(oldBranchName);

        task.Title = dto.Title.Trim();
        task.Description = dto.Description?.Trim();
        task.TypeId = dto.TypeId;
        task.AssignedToId = dto.AssignedToId;
        task.Deadline = dto.Deadline;

        await _context.SaveChangesAsync();

        string warning = null;

        if (!string.IsNullOrEmpty(dto.GitHubBranchName) &&
            !string.IsNullOrEmpty(task.Team.GitRepoName) &&
            !hasExistingBranch)
        {
            try
            {
                var captain = await _context.Users
                    .FirstOrDefaultAsync(u => u.TeamId == task.Team.Id && u.RoleId == 1);

                GitHubBranchResult branchResult = null;

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
                    warning = branchResult.ErrorMessage;
                }
                else
                {
                    task.GithubBranchName = dto.GitHubBranchName?.Trim();
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                warning = ex.Message;
            }
        }

        var message = "Задача успешно обновлена";
        if (!string.IsNullOrEmpty(warning))
        {
            message += $". Предупреждение: {warning}";
        }

        return HandleSuccess(message);
    }

    /// <summary>
    /// Назначить задачу пользователю
    /// </summary>
    [HttpPost("{taskId}/assign")]
    public async Task<ActionResult<ApiResponse>> AssignTask(int taskId, [FromBody] AssignTaskDto dto)
    {
        var userId = GetUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return HandleUnauthorized("Пользователь не найден");

        var task = await _context.Tasks
            .Include(p => p.Team)
            .ThenInclude(t => t.Users)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return HandleNotFound("Задача не найдена");

        if (user.RoleId != 1 && !task.Team.Users.Any(u => u.Id == userId && u.RoleId == 1))
            return HandleForbidden("Только капитан команды может назначать задачи");

        var assignee = task.Team.Users.FirstOrDefault(u => u.Id == dto.UserId);
        if (assignee == null)
            return HandleError("Пользователь не состоит в команде");

        task.AssignedToId = dto.UserId;
        task.StatusId = 2;

        await _context.SaveChangesAsync();

        return HandleSuccess("Задача успешно назначена");
    }

    /// <summary>
    /// Запросить завершение задачи
    /// </summary>
    [HttpPost("{taskId}/request-completion")]
    public async Task<ActionResult<ApiResponse>> RequestCompletion(int taskId)
    {
        var userId = GetUserId();
        var task = await _context.Tasks
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return HandleNotFound("Задача не найдена");

        if (task.AssignedToId != userId)
            return HandleForbidden("Только исполнитель задачи может запрашивать завершение");

        if (task.StatusId != 2)
            return HandleError("Задача должна быть в статусе 'В процессе'");

        task.StatusId = 3;
        await _context.SaveChangesAsync();

        return HandleSuccess("Запрос на завершение отправлен капитану");
    }

    /// <summary>
    /// Запросить отмену задачи
    /// </summary>
    [HttpPost("{taskId}/request-cancellation")]
    public async Task<ActionResult<ApiResponse>> RequestCancellation(int taskId)
    {
        var userId = GetUserId();
        var task = await _context.Tasks
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return HandleNotFound("Задача не найдена");

        if (task.AssignedToId != userId)
            return HandleForbidden("Только исполнитель задачи может запрашивать отмену");

        if (task.StatusId == 5)
            return HandleError("Задача уже отменена");

        task.StatusId = 5;
        await _context.SaveChangesAsync();

        return HandleSuccess("Задача отменена");
    }

    /// <summary>
    /// Удалить задачу
    /// </summary>
    [HttpDelete("{taskId}")]
    public async Task<ActionResult<ApiResponse>> DeleteTask(int taskId)
    {
        var userId = GetUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return HandleUnauthorized("Пользователь не найден");

        var task = await _context.Tasks
            .Include(t => t.Team)
            .ThenInclude(t => t.Users)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return HandleNotFound("Задача не найдена");

        if (user.RoleId != 1 && !task.Team.Users.Any(u => u.Id == userId && u.RoleId == 1))
            return HandleForbidden("Только капитан команды может удалять задачи");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var chatId = task.ChatId;

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            if (chatId != null)
            {
                var chat = await _context.Chats.FindAsync(chatId);
                if (chat != null)
                {
                    _context.Chats.Remove(chat);
                    await _context.SaveChangesAsync();
                }
            }

            await transaction.CommitAsync();
            return HandleSuccess("Задача успешно удалена");
        }
        catch
        {
            await transaction.RollbackAsync();
            return HandleError("Ошибка при удалении задачи");
        }
    }
}