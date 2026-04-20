using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Helpers;
using HackathonCoordinator.WebAPI.Models;
using HackathonCoordinator.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HackathonCoordinator.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TeamsController : BaseApiController
    {
        private readonly HackathonCoordinatorContext _context;
        private readonly NotificationHelperService _notificationHelper;
        private readonly IEncryptionService _encryptionService;
        private readonly IGitHubService _gitHubService;

        public TeamsController(
            HackathonCoordinatorContext context,
            NotificationHelperService notificationHelper,
            IEncryptionService encryptionService,
            IGitHubService gitHubService)
        {
            _context = context;
            _encryptionService = encryptionService;
            _gitHubService = gitHubService;
            _notificationHelper = notificationHelper;
        }

        /// <summary>
        /// Присоединиться к команде по коду приглашения
        /// </summary>
        [HttpPost("join")]
        public async Task<ActionResult<ApiResponse>> JoinTeam([FromBody] JoinTeamDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.InviteCode))
                    return HandleError("Код приглашения не может быть пустым");

                var userId = GetUserId();
                if (userId == 0)
                    return HandleUnauthorized("Не удалось определить пользователя");

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return HandleNotFound("Пользователь не найден");

                if (user.TeamId != null)
                    return HandleError("Вы уже состоите в команде. Сначала покиньте текущую");

                var team = await _context.Teams
                    .FirstOrDefaultAsync(t => t.InviteCode == dto.InviteCode.Trim());

                if (team == null)
                    return HandleNotFound("Команда с таким кодом не найдена");

                user.TeamId = team.Id;
                await _context.SaveChangesAsync();

                // Уведомление о новом участнике команды
                try
                {
                    await _notificationHelper.NotifyNewTeamMember(team.Id, user.Id, user.Username);
                }
                catch
                {
                    return HandleSuccess($"Вы успешно присоединились к команде «{team.Name}»\n!Ошибка отправки уведомления!");
                }

                return HandleSuccess($"Вы успешно присоединились к команде «{team.Name}»");
            }
            catch (Exception ex)
            {
                return HandleError($"Ошибка при присоединении к команде: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить текущую команду пользователя
        /// </summary>
        [HttpGet("current")]
        public async Task<ActionResult<ApiResponse<TeamDto>>> GetCurrentTeam()
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return HandleUnauthorized<TeamDto>("Не удалось определить пользователя");

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
                            IsCaptain = m.RoleId == (int)Roles.Captain
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
                    return HandleNotFound<TeamDto>("Вы не состоите в команде");

                var teamCaptainGitHub = "";

                if (teamData.Team.GitRepoName != null)
                {
                    teamCaptainGitHub = await _context.Users
                        .Where(u => u.RoleId == (int)Roles.Captain && u.TeamId == teamData.Team.Id)
                        .Select(t => t.GitHubUsername)
                        .FirstOrDefaultAsync();
                }

                var teamDto = new TeamDto
                {
                    Id = teamData.Team.Id,
                    Name = teamData.Team.Name,
                    ChatId = teamData.Team.ChatId,
                    InviteCode = teamData.Team.InviteCode,
                    GitHubUrl = teamData.Team.GitRepoName != null ?
                        $"https://github.com/{teamCaptainGitHub}/{teamData.Team.GitRepoName}" : null,
                    Members = teamData.Members,
                    Tasks = teamData.Tasks
                };

                return HandleResult(teamDto);
            }
            catch (Exception ex)
            {
                return HandleError<TeamDto>($"Ошибка при получении команды: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить задачи команды
        /// </summary>
        [HttpGet("{teamId}/tasks")]
        public async Task<ActionResult<ApiResponse<List<TaskDto>>>> GetTeamTasks(int teamId)
        {
            try
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

                return HandleResult(taskDtos);
            }
            catch (Exception ex)
            {
                return HandleError<List<TaskDto>>($"Ошибка при получении задач команды: {ex.Message}");
            }
        }

        /// <summary>
        /// Создать задачу в команде
        /// </summary>
        [HttpPost("{teamId}/tasks")]
        public async Task<ActionResult<ApiResponse>> CreateTask(int teamId, [FromBody] CreateTaskDto dto)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                    return HandleUnauthorized("Пользователь не найден");

                var team = await _context.Teams
                    .Include(p => p.Users)
                    .FirstOrDefaultAsync(p => p.Id == teamId);

                if (team == null)
                    return HandleNotFound("Команда не найдена");

                if (user.RoleId != (int)Roles.Captain && !team.Users.Any(u => u.Id == userId && u.RoleId == (int)Roles.Captain))
                    return HandleForbidden("Только капитан команды может создавать задачи");

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var taskChat = new Chat
                    {
                        Name = $"Чат задачи: {dto.Title.Trim()}",
                        TypeId = (int)ChatTypes.TaskChat,
                        CreatedAt = DateTime.Now
                    };
                    _context.Chats.Add(taskChat);
                    await _context.SaveChangesAsync();

                    var task = new Models.Task
                    {
                        TeamId = teamId,
                        Title = dto.Title.Trim(),
                        Description = dto.Description?.Trim(),
                        TypeId = dto.TypeId,
                        StatusId = dto.AssignedToId != null ? (int)TaskStatuses.InProgress : (int)TaskStatuses.Pending,
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
                        SentAt = DateTime.Now
                    };

                    _context.Messages.Add(welcomeMessage);
                    await _context.SaveChangesAsync();

                    string warning = null;

                    if (!string.IsNullOrEmpty(dto.GitHubBranchName) && !string.IsNullOrEmpty(team.GitRepoName))
                    {
                        try
                        {
                            var captain = await _context.Users
                                .FirstOrDefaultAsync(u => u.TeamId == team.Id && u.RoleId == (int)Roles.Captain);

                            GitHubBranchResult branchResult = null;

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

                    await transaction.CommitAsync();

                    var message = "Задача успешно создана";
                    if (!string.IsNullOrEmpty(warning))
                    {
                        message += $". Предупреждение: {warning}";
                    }

                    // Уведомления о новой задаче
                    try
                    {
                        await _notificationHelper.NotifyTeamAboutNewTask(task.Id, teamId, task.Title);

                        if (task.AssignedToId.HasValue)
                            await _notificationHelper.NotifyTaskAssignment(task.Id, task.AssignedToId.Value, task.Title);
                    }
                    catch
                    {
                        return HandleSuccess($"{message}\n!Ошибка отправки уведомления!");
                    }

                    return HandleSuccess(message);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return HandleError($"Ошибка при создании задачи: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить соревнование команды
        /// </summary>
        [HttpGet("{teamId}/competition")]
        public async Task<ActionResult<ApiResponse<CompetitionDto>>> GetTeamCompetitionAsync(int teamId)
        {
            try
            {
                var team = await _context.Teams
                    .Include(t => t.Competition)
                        .ThenInclude(c => c.CreatedBy)
                    .Include(t => t.Competition)
                        .ThenInclude(c => c.Teams)
                    .FirstOrDefaultAsync(t => t.Id == teamId);

                if (team?.Competition == null)
                    return HandleNotFound<CompetitionDto>("Соревнование для указанной команды не найдено");

                var competition = team.Competition;

                var result = new CompetitionDto
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

                return HandleResult(result);
            }
            catch (Exception ex)
            {
                return HandleError<CompetitionDto>($"Ошибка при получении соревнования команды: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить команду по ID (только организатор)
        /// </summary>
        [HttpGet("{teamId}")]
        public async Task<ActionResult<ApiResponse<TeamDto>>> GetTeamById(int teamId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return HandleUnauthorized<TeamDto>("Не удалось определить пользователя");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return HandleNotFound<TeamDto>("Пользователь не найден");

                if (user.RoleId != (int)Roles.Organizer && user.RoleId != (int)Roles.Admin)
                    return HandleForbidden<TeamDto>("Недостаточно прав для доступа к данным команд");

                var teamData = await _context.Teams
                    .Where(t => t.Id == teamId)
                    .Select(t => new
                    {
                        Team = t,
                        Members = t.Users
                            .OrderByDescending(m => m.RoleId == (int)Roles.Captain)
                            .ThenBy(m => m.Username)
                            .Select(m => new MemberDto
                            {
                                Id = m.Id,
                                Username = m.Username,
                                RoleName = m.Role.Name ?? "Участник",
                                IconName = m.ProfileIcon.Name,
                                IsCaptain = m.RoleId == (int)Roles.Captain
                            })
                            .ToList(),
                        Tasks = t.Tasks
                            .OrderByDescending(task => task.CreatedAt)
                            .Take(100)
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
                    return HandleNotFound<TeamDto>("Команда не найдена");

                var teamCaptainGitHub = await _context.Users
                    .Where(u => u.RoleId == (int)Roles.Captain && u.TeamId == teamId)
                    .Select(t => t.GitHubUsername)
                    .FirstOrDefaultAsync();

                var teamDto = new TeamDto
                {
                    Id = teamData.Team.Id,
                    Name = teamData.Team.Name,
                    ChatId = teamData.Team.ChatId,
                    InviteCode = teamData.Team.InviteCode,
                    GitHubUrl = !string.IsNullOrEmpty(teamData.Team.GitRepoName) && !string.IsNullOrEmpty(teamCaptainGitHub)
                        ? $"https://github.com/{teamCaptainGitHub}/{teamData.Team.GitRepoName}"
                        : null,
                    Members = teamData.Members,
                    Tasks = teamData.Tasks
                };

                return HandleResult(teamDto);
            }
            catch (Exception ex)
            {
                return HandleError<TeamDto>($"Ошибка при получении команды: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить ID текущей команды
        /// </summary>
        [HttpGet("current/id")]
        public async Task<ActionResult<ApiResponse<int>>> GetCurrentTeamId()
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return HandleUnauthorized<int>("Не удалось определить пользователя");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return HandleNotFound<int>("Пользователь не найден");

                if (user.TeamId == null)
                    return HandleNotFound<int>("Вы не состоите в команде");

                return HandleResult(user.TeamId.Value);
            }
            catch (Exception ex)
            {
                return HandleError<int>($"Ошибка при получении ID команды: {ex.Message}");
            }
        }

        /// <summary>
        /// Покинуть команду
        /// </summary>
        [HttpPost("leave")]
        public async Task<ActionResult<ApiResponse>> LeaveTeam()
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return HandleUnauthorized("Не удалось определить пользователя");

                var user = await _context.Users
                    .Include(u => u.Team)
                    .ThenInclude(t => t.Users)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return HandleNotFound("Пользователь не найден");

                if (user.TeamId == null)
                    return HandleError("Вы не состоите в команде");

                var team = user.Team;
                var membersCount = team.Users.Count;

                if (user.RoleId == (int)Roles.Captain)
                {
                    team.GitRepoName = null;

                    if (membersCount > 1)
                    {
                        var newCaptain = team.Users
                            .FirstOrDefault(u => u.Id != userId && u.RoleId != (int)Roles.Captain);

                        if (newCaptain != null)
                        {
                            newCaptain.RoleId = (int)Roles.Captain;
                        }
                    }
                }

                user.TeamId = null;
                user.RoleId = (int)Roles.Member;

                await _context.SaveChangesAsync();

                // Уведомление о выходе участника из команды
                try
                {
                    await _notificationHelper.NotifyTeamMemberLeft(team.Id, user.Id, user.Username);
                }
                catch
                {
                    return HandleSuccess("Вы покинули команду\n!Ошибка отправки уведомления!");
                }

                return HandleSuccess("Вы покинули команду");
            }
            catch (Exception ex)
            {
                return HandleError($"Ошибка при выходе из команды: {ex.Message}");
            }
        }

        /// <summary>
        /// Назначить капитана команды
        /// </summary>
        [HttpPost("{teamId}/assign-captain")]
        public async Task<ActionResult<ApiResponse>> AssignCaptain(int teamId, [FromBody] AssignCaptainDto dto)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                    return HandleUnauthorized("Пользователь не найден");

                if (user.RoleId == (int)Roles.Member)
                    return HandleForbidden("Только организатор или капитан может назначать капитанов");

                var team = await _context.Teams
                    .Include(t => t.Users)
                    .FirstOrDefaultAsync(t => t.Id == teamId);

                if (team == null)
                    return HandleNotFound("Команда не найдена");

                if (user.TeamId != teamId && user.RoleId != (int)Roles.Organizer && user.RoleId != (int)Roles.Admin)
                    return HandleError("Недостаточно прав для назначения капитана");

                var newCaptain = team.Users.FirstOrDefault(u => u.Id == dto.UserId);
                if (newCaptain == null)
                    return HandleError("Участник не найден в команде");

                var currentCaptain = team.Users.FirstOrDefault(u => u.RoleId == (int)Roles.Captain);
                if (currentCaptain != null)
                {
                    if (currentCaptain.Id == dto.UserId)
                        return HandleError("Участник уже является капитаном");

                    currentCaptain.RoleId = (int)Roles.Member;
                }

                newCaptain.RoleId = (int)Roles.Captain;
                team.GitRepoName = null;

                await _context.SaveChangesAsync();

                // Уведомления о назначении капитана
                try
                {
                    await _notificationHelper.NotifyCaptainAssignment(teamId, newCaptain.Id, team.Name);
                    await _notificationHelper.NotifyNewCaptainToTeam(teamId, newCaptain.Id, newCaptain.Username, team.Name);
                }
                catch
                {
                    return HandleSuccess("Капитан успешно назначен\n!Ошибка отправки уведомления!");
                }

                return HandleSuccess("Капитан успешно назначен");
            }
            catch (Exception ex)
            {
                return HandleError($"Ошибка при назначении капитана: {ex.Message}");
            }
        }

        /// <summary>
        /// Удалить команду (только организатор)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteTeam(int id)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user?.RoleId != (int)Roles.Organizer && user?.RoleId != (int)Roles.Admin)
                    return HandleForbidden("Недостаточно прав для удаления команды");

                var team = await _context.Teams
                    .Include(t => t.Competition)
                    .Include(t => t.Users)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (team == null)
                    return HandleNotFound("Команда не найдена");

                var teamName = team.Name;
                var competitionName = team.Competition?.Name;
                var competitionId = team.Competition?.Id;
                var memberIds = team.Users.Select(u => u.Id).ToList();

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var chatId = team.ChatId;

                    _context.Teams.Remove(team);
                    await _context.SaveChangesAsync();

                    var chat = await _context.Chats.FindAsync(chatId);
                    if (chat != null)
                    {
                        _context.Chats.Remove(chat);
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();

                    // Уведомления после успешного удаления
                    try
                    {
                        // Уведомляем всех бывших участников команды
                        await _notificationHelper.NotifyTeamDeleted(memberIds, teamName, user.Username);

                        // Уведомляем организаторов о удалении команды
                        if (!string.IsNullOrEmpty(competitionName))
                        {
                            await _notificationHelper.NotifyOrganizersAboutTeamDeletion(competitionId.Value, teamName, competitionName, user.Username);
                        }
                    }
                    catch
                    {
                        return HandleSuccess("Команда успешно удалена\n!Ошибка отправки уведомления!");
                    }

                    return HandleSuccess("Команда успешно удалена");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return HandleError($"Ошибка при удалении команды: {ex.Message}");
            }
        }

        /// <summary>
        /// Выгнать участника из команды
        /// </summary>
        [HttpDelete("members/{memberId}/kick")]
        public async Task<ActionResult<ApiResponse>> KickMember(int memberId)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user?.RoleId != (int)Roles.Organizer && user?.RoleId != (int)Roles.Admin)
                    return HandleForbidden("Недостаточно прав для исключения участников");

                var memberToKick = await _context.Users
                    .Include(u => u.Team)
                    .FirstOrDefaultAsync(u => u.Id == memberId);

                if (memberToKick == null)
                    return HandleNotFound("Участник не найден");

                if (memberToKick.TeamId == null)
                    return HandleError("Участник не состоит в команде");

                if (memberToKick.RoleId == (int)Roles.Captain)
                    return HandleError("Нельзя выгнать капитана команды");

                var teamName = memberToKick.Team?.Name;
                var teamId = memberToKick.Team?.Id;
                memberToKick.TeamId = null;

                await _context.SaveChangesAsync();

                // Уведомление об исключении из команды
                try
                {
                    await _notificationHelper.NotifyMemberKicked(memberId, teamName);
                }
                catch
                {
                    return HandleSuccess($"Участник {memberToKick.Username} выгнан из команды {teamName}\n!Ошибка отправки уведомления!");
                }

                return HandleSuccess($"Участник {memberToKick.Username} выгнан из команды {teamName}");
            }
            catch (Exception ex)
            {
                return HandleError($"Ошибка при исключении участника: {ex.Message}");
            }
        }

        /// <summary>
        /// Создать GitHub репозиторий для команды
        /// </summary>
        [HttpPost("{teamId}/create-github-repo")]
        public async Task<ActionResult<ApiResponse<GitHubRepoCreationResponseDto>>> CreateGitHubRepository(int teamId, [FromBody] CreateGitHubRepoDto dto)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                    return HandleUnauthorized<GitHubRepoCreationResponseDto>("Пользователь не найден");

                var team = await _context.Teams
                    .Include(t => t.Users)
                    .FirstOrDefaultAsync(t => t.Id == teamId);

                if (team == null)
                    return HandleNotFound<GitHubRepoCreationResponseDto>("Команда не найдена");

                if (user.RoleId != (int)Roles.Captain)
                    return HandleForbidden<GitHubRepoCreationResponseDto>("Только капитан команды может создавать GitHub репозиторий");

                if (string.IsNullOrEmpty(user.GitHubAccessToken))
                    return HandleError<GitHubRepoCreationResponseDto>("У капитана команды не привязан GitHub аккаунт");

                if (team.GitRepoName != null)
                    return HandleError<GitHubRepoCreationResponseDto>("К команде уже подключен GitHub репозиторий");

                var decryptedToken = _encryptionService.Decrypt(user.GitHubAccessToken);

                var result = await _gitHubService.CreateRepositoryAsync(decryptedToken, dto.RepoName, dto.Description, dto.IsPrivate);

                if (!result.Success)
                    return HandleError<GitHubRepoCreationResponseDto>(result.ErrorMessage);

                team.GitRepoName = dto.RepoName;
                await _context.SaveChangesAsync();

                var response = new GitHubRepoCreationResponseDto
                {
                    Message = result.Message,
                    RepoUrl = result.RepoUrl,
                    RepoName = result.RepoName
                };

                // Уведомление о создании репозитория
                try
                {
                    await _notificationHelper.NotifyGitHubRepoCreated(teamId, team.Name, result.RepoName, result.RepoUrl);
                }
                catch
                {
                    response.Message += "\n!Ошибка отправки уведомления!";
                    return HandleResult(response);
                }

                return HandleResult(response);
            }
            catch (Exception ex)
            {
                return HandleError<GitHubRepoCreationResponseDto>($"Ошибка при создании репозитория: {ex.Message}");
            }
        }
    }
}