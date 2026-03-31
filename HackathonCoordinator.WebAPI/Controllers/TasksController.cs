using HackathonCoordinator.WebAPI;
using HackathonCoordinator.WebAPI.Controllers;
using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Helpers;
using HackathonCoordinator.WebAPI.Models;
using HackathonCoordinator.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using TaskStatus = HackathonCoordinator.WebAPI.Models.TaskStatus;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TasksController : BaseApiController
{
    private readonly HackathonCoordinatorContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IGitHubService _gitHubService;
    private readonly NotificationHelperService _notificationHelper;

    public TasksController(HackathonCoordinatorContext context, NotificationHelperService notificationHelper, IEncryptionService encryptionService, IGitHubService gitHubService)
    {
        _context = context;
        _encryptionService = encryptionService;
        _gitHubService = gitHubService;
        _notificationHelper = notificationHelper;
    }

    /// <summary>
    /// Получить типы задач
    /// </summary>
    [HttpGet("types")]
    public async Task<ActionResult<ApiResponse<List<TaskType>>>> GetTaskTypes()
    {
        try
        {
            var types = await _context.TaskTypes.ToListAsync();
            return HandleResult(types);
        }
        catch (DbUpdateException ex)
        {
            return HandleError<List<TaskType>>("Ошибка базы данных при получении типов задач");
        }
        catch (Exception ex)
        {
            return HandleError<List<TaskType>>("Внутренняя ошибка сервера при получении типов задач");
        }
    }

    /// <summary>
    /// Получить статусы задач
    /// </summary>
    [HttpGet("statuses")]
    public async Task<ActionResult<ApiResponse<List<TaskStatus>>>> GetTaskStatuses()
    {
        try
        {
            var statuses = await _context.TaskStatuses.ToListAsync();
            return HandleResult(statuses);
        }
        catch (DbUpdateException ex)
        {
            return HandleError<List<TaskStatus>>("Ошибка базы данных при получении статусов задач");
        }
        catch (Exception ex)
        {
            return HandleError<List<TaskStatus>>("Внутренняя ошибка сервера при получении статусов задач");
        }
    }

    /// <summary>
    /// Получить список своих задач
    /// </summary>
    [HttpGet("my/ids")]
    public async Task<ActionResult<ApiResponse<List<int>>>> GetUserTasksIds()
    {
        try
        {
            var userId = GetUserId();

            var tasksIds = await _context.Tasks
                .Where(t => t.AssignedToId == userId)
                .Select(t => t.Id)
                .ToListAsync();

            return HandleResult(tasksIds);
        }
        catch (DbUpdateException ex)
        {
            return HandleError<List<int>>("Ошибка базы данных при получении списка задач");
        }
        catch (InvalidOperationException ex)
        {
            return HandleUnauthorized<List<int>>("Пользователь не найден");
        }
        catch (Exception ex)
        {
            return HandleError<List<int>>("Внутренняя ошибка сервера при получении списка задач");
        }
    }

    /// <summary>
    /// Получить детальную информацию о задаче
    /// </summary>
    [HttpGet("{taskId}/details")]
    public async Task<ActionResult<ApiResponse<TaskDetailsDto>>> GetTaskDetails(int taskId)
    {
        try
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
            var isCaptain = user?.RoleId == (int)Roles.Captain;
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
                CanComplete = isMyTask && task.StatusId == (int)TaskStatuses.InProgress,
                CanCancel = isMyTask && task.StatusId == (int)TaskStatuses.InProgress,
                CanConfirmCompletion = isCaptain && task.StatusId == (int)TaskStatuses.InReview,
                CanRejectCompletion = isCaptain && task.StatusId == (int)TaskStatuses.InReview,
                CanCancelTaskAsCaptain = isCaptain && task.StatusId != (int)TaskStatuses.Completed && task.StatusId != (int)TaskStatuses.Cancelled,
                HasChat = task.ChatId != null,
                TaskChatId = task.ChatId
            };

            return HandleResult(dto);
        }
        catch (DbUpdateException ex)
        {
            return HandleError<TaskDetailsDto>("Ошибка базы данных при получении деталей задачи");
        }
        catch (InvalidOperationException ex)
        {
            return HandleUnauthorized<TaskDetailsDto>("Пользователь не найден");
        }
        catch (Exception ex)
        {
            return HandleError<TaskDetailsDto>("Внутренняя ошибка сервера при получении деталей задачи");
        }
    }

    /// <summary>
    /// Обновить задачу
    /// </summary>
    [HttpPut("{taskId}")]
    public async Task<ActionResult<ApiResponse>> UpdateTask(int taskId, [FromBody] CreateTaskDto dto)
    {
        try
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

            if (user.RoleId != (int)Roles.Captain && !task.Team.Users.Any(u => u.Id == userId && u.RoleId == (int)Roles.Captain))
                return HandleForbidden("Только капитан команды может редактировать задачи");

            var oldBranchName = task.GithubBranchName;
            var hasExistingBranch = !string.IsNullOrEmpty(oldBranchName);

            if (task.Deadline != dto.Deadline)
            {
                task.IsDeadlineNotified = false;
                task.IsDeadlineApproachNotified = false;
            }

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
                        .FirstOrDefaultAsync(u => u.TeamId == task.Team.Id && u.RoleId == (int)Roles.Captain);

                    GitHubBranchResult branchResult = null;

                    if (captain == null || string.IsNullOrEmpty(captain.GitHubAccessToken))
                    {
                        branchResult = new GitHubBranchResult { Success = false, ErrorMessage = "Капитан не найден или не привязал GitHub аккаунт" };
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
                catch (ArgumentNullException ex)
                {
                    warning = "Не удалось расшифровать GitHub токен";
                }
                catch (CryptographicException ex)
                {
                    warning = "Ошибка при работе с GitHub API";
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
        catch (DbUpdateException ex)
        {
            return HandleError("Ошибка базы данных при обновлении задачи");
        }
        catch (ArgumentNullException ex)
        {
            return HandleError("Некорректные данные задачи");
        }
        catch (Exception ex)
        {
            return HandleError("Внутренняя ошибка сервера при обновлении задачи");
        }
    }

    /// <summary>
    /// Назначить задачу пользователю
    /// </summary>
    [HttpPost("{taskId}/assign")]
    public async Task<ActionResult<ApiResponse>> AssignTask(int taskId, [FromBody] AssignTaskDto dto)
    {
        try
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

            if (user.RoleId != (int)Roles.Captain && !task.Team.Users.Any(u => u.Id == userId && u.RoleId == (int)Roles.Captain))
                return HandleForbidden("Только капитан команды может назначать задачи");

            var assignee = task.Team.Users.FirstOrDefault(u => u.Id == dto.UserId);
            if (assignee == null)
                return HandleError("Пользователь не состоит в команде");

            task.AssignedToId = dto.UserId;
            task.StatusId = 2;

            await _context.SaveChangesAsync();

            // УВЕДОМЛЕНИЕ: Назначение задачи
            try
            {
                await _notificationHelper.NotifyTaskAssignment(task.Id, assignee.Id, task.Title);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании уведомления: {ex.Message}");
            }

            return HandleSuccess("Задача успешно назначена");
        }
        catch (DbUpdateException ex)
        {
            return HandleError("Ошибка базы данных при назначении задачи");
        }
        catch (ArgumentNullException ex)
        {
            return HandleError("Некорректные данные для назначения задачи");
        }
        catch (Exception ex)
        {
            return HandleError("Внутренняя ошибка сервера при назначении задачи");
        }
    }

    /// <summary>
    /// Запросить завершение задачи
    /// </summary>
    [HttpPost("{taskId}/request-completion")]
    public async Task<ActionResult<ApiResponse>> RequestCompletion(int taskId)
    {
        try
        {
            var userId = GetUserId();
            var task = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.Team)
                .ThenInclude(t => t.Users)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return HandleNotFound("Задача не найдена");

            if (task.AssignedToId != userId)
                return HandleForbidden("Только исполнитель задачи может запрашивать завершение");

            if (task.StatusId != (int)TaskStatuses.InProgress)
                return HandleError("Задача должна быть в статусе 'В процессе'");

            task.StatusId = (int)TaskStatuses.InReview;
            await _context.SaveChangesAsync();

            // УВЕДОМЛЕНИЕ: Запрос завершения задачи для капитана
            try
            {
                var captain = task.Team.Users.FirstOrDefault(u => u.RoleId == (int)Roles.Captain);
                if (captain != null)
                {
                    await _notificationHelper.NotifyRequestTaskCompletion(
                        task.Id,
                        captain.Id,
                        task.Title,
                        task.AssignedTo?.Username ?? "Неизвестный участник");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании уведомления: {ex.Message}");
            }

            return HandleSuccess("Запрос на завершение отправлен капитану");
        }
        catch (DbUpdateException ex)
        {
            return HandleError("Ошибка базы данных при запросе завершения задачи");
        }
        catch (InvalidOperationException ex)
        {
            return HandleUnauthorized("Пользователь не найден");
        }
        catch (Exception ex)
        {
            return HandleError("Внутренняя ошибка сервера при запросе завершения задачи");
        }
    }

    /// <summary>
    /// Запросить отмену задачи
    /// </summary>
    [HttpPost("{taskId}/request-cancellation")]
    public async Task<ActionResult<ApiResponse>> RequestCancellation(int taskId)
    {
        try
        {
            var userId = GetUserId();
            var task = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.Team)
                .ThenInclude(t => t.Users)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return HandleNotFound("Задача не найдена");

            if (task.AssignedToId != userId)
                return HandleForbidden("Только исполнитель задачи может запрашивать отмену");

            if (task.StatusId == (int)TaskStatuses.Cancelled)
                return HandleError("Задача уже отменена");

            if (task.StatusId != (int)TaskStatuses.InProgress)
                return HandleError("Задача должна быть в статусе 'В процессе'");

            task.StatusId = (int)TaskStatuses.InReview;
            await _context.SaveChangesAsync();

            // УВЕДОМЛЕНИЕ: Запрос отмены задачи для капитана
            try
            {
                var captain = task.Team.Users.FirstOrDefault(u => u.RoleId == (int)Roles.Captain);
                if (captain != null)
                {
                    await _notificationHelper.NotifyRequestTaskCancellation(
                        task.Id,
                        captain.Id,
                        task.Title,
                        task.AssignedTo?.Username ?? "Неизвестный участник");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании уведомления: {ex.Message}");
            }

            return HandleSuccess("Задача отменена");
        }
        catch (DbUpdateException ex)
        {
            return HandleError("Ошибка базы данных при запросе отмены задачи");
        }
        catch (InvalidOperationException ex)
        {
            return HandleUnauthorized("Пользователь не найден");
        }
        catch (Exception ex)
        {
            return HandleError("Внутренняя ошибка сервера при запросе отмены задачи");
        }
    }

    /// <summary>
    /// Отклонить завершение задачи (для капитана)
    /// </summary>
    [HttpPost("{taskId}/reject-completion")]
    public async Task<ActionResult<ApiResponse>> RejectCompletion(int taskId)
    {
        try
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return HandleUnauthorized("Пользователь не найден");

            var task = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.Team)
                .ThenInclude(t => t.Users)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return HandleNotFound("Задача не найдена");

            // Проверяем, что пользователь - капитан команды
            var isCaptain = task.Team.Users.Any(u => u.Id == userId && u.RoleId == (int)Roles.Captain);
            if (!isCaptain)
                return HandleForbidden("Только капитан команды может отклонять завершение задач");

            if (task.StatusId != 3) // На проверке
                return HandleError("Задача должна быть в статусе 'На проверке'");

            task.StatusId = 2; // Возвращаем в статус "В процессе"
            await _context.SaveChangesAsync();

            // УВЕДОМЛЕНИЕ: Отклонение завершения задачи для исполнителя
            try
            {
                if (task.AssignedToId.HasValue)
                {
                    await _notificationHelper.NotifyTaskRejection(
                        task.AssignedToId.Value,
                        task.Title,
                        task.Id);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании уведомления: {ex.Message}");
            }

            return HandleSuccess("Завершение задачи отклонено. Задача возвращена в работу");
        }
        catch (DbUpdateException ex)
        {
            return HandleError("Ошибка базы данных при отклонении завершения задачи");
        }
        catch (InvalidOperationException ex)
        {
            return HandleUnauthorized("Пользователь не найден");
        }
        catch (Exception ex)
        {
            return HandleError("Внутренняя ошибка сервера при отклонении завершения задачи");
        }
    }

    /// <summary>
    /// Подтвердить завершение задачи
    /// </summary>
    [HttpPost("{taskId}/confirm-completion")]
    public async Task<ActionResult<ApiResponse>> ConfirmCompletion(int taskId)
    {
        try
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return HandleUnauthorized("Пользователь не найден");

            var task = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.Team)
                .ThenInclude(t => t.Users)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return HandleNotFound("Задача не найдена");

            // Проверяем, что пользователь - капитан команды
            var isCaptain = task.Team.Users.Any(u => u.Id == userId && u.RoleId == (int)Roles.Captain);
            if (!isCaptain)
                return HandleForbidden("Только капитан команды может подтверждать завершение задач");

            if (task.StatusId != (int)TaskStatuses.InReview)
                return HandleError("Задача должна быть в статусе 'На проверке'");

            task.StatusId = (int)TaskStatuses.Completed; // Завершена
            await _context.SaveChangesAsync();

            // УВЕДОМЛЕНИЕ: Подтверждение завершения задачи для исполнителя
            try
            {
                if (task.AssignedToId.HasValue)
                {
                    await _notificationHelper.NotifyTaskConfirmation(
                        task.AssignedToId.Value,
                        task.Title,
                        task.Id);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании уведомления: {ex.Message}");
            }

            return HandleSuccess("Задача успешно подтверждена");
        }
        catch (DbUpdateException ex)
        {
            return HandleError("Ошибка базы данных при подтверждении завершения задачи");
        }
        catch (InvalidOperationException ex)
        {
            return HandleUnauthorized("Пользователь не найден");
        }
        catch (Exception ex)
        {
            return HandleError("Внутренняя ошибка сервера при подтверждении завершения задачи");
        }
    }

    /// <summary>
    /// Отменить задачу
    /// </summary>
    [HttpPost("{taskId}/cancel")]
    public async Task<ActionResult<ApiResponse>> TaskCancelAsync(int taskId)
    {
        try
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return HandleUnauthorized("Пользователь не найден");

            var task = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.Team)
                .ThenInclude(t => t.Users)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return HandleNotFound("Задача не найдена");

            // Проверяем, что пользователь - капитан команды
            var isCaptain = task.Team.Users.Any(u => u.Id == userId && u.RoleId == (int)Roles.Captain);
            if (!isCaptain)
                return HandleForbidden("Только капитан команды может отменить задач");

            if (task.StatusId == (int)TaskStatuses.Cancelled) // Уже отменена
                return HandleError("Задача уже отменена");

            if (task.StatusId == (int)TaskStatuses.Completed) // Уже завершена
                return HandleError("Нельзя отменить завершенную задачу");

            task.StatusId = (int)TaskStatuses.Cancelled; // Отменена
            await _context.SaveChangesAsync();

            // УВЕДОМЛЕНИЕ: Отмена задачи для исполнителя
            try
            {
                if (task.AssignedToId.HasValue)
                {
                    await _notificationHelper.NotifyTaskCancellation(
                        task.AssignedToId.Value,
                        task.Title,
                        task.Id);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании уведомления: {ex.Message}");
            }

            return HandleSuccess("Задача успешно отменена");
        }
        catch (DbUpdateException ex)
        {
            return HandleError("Ошибка базы данных при отмене задачи");
        }
        catch (InvalidOperationException ex)
        {
            return HandleUnauthorized("Пользователь не найден");
        }
        catch (Exception ex)
        {
            return HandleError("Внутренняя ошибка сервера при отмене задачи");
        }
    }

    /// <summary>
    /// Удалить задачу
    /// </summary>
    [HttpDelete("{taskId}")]
    public async Task<ActionResult<ApiResponse>> DeleteTask(int taskId)
    {
        try
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

            if (user.RoleId != (int)Roles.Captain && !task.Team.Users.Any(u => u.Id == userId && u.RoleId == (int)Roles.Captain))
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
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                return HandleError("Ошибка базы данных при удалении задачи");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return HandleError("Ошибка при удалении задачи");
            }
        }
        catch (InvalidOperationException ex)
        {
            return HandleUnauthorized("Пользователь не найден");
        }
        catch (Exception ex)
        {
            return HandleError("Внутренняя ошибка сервера при удалении задачи");
        }
    }
}