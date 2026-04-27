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
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CompetitionsController : BaseApiController
    {
        private readonly HackathonCoordinatorContext _context;
        private readonly NotificationHelperService _notificationHelper;

        public CompetitionsController(
            HackathonCoordinatorContext context,
            NotificationHelperService notificationHelper)
        {
            _context = context;
            _notificationHelper = notificationHelper;
        }

        /// <summary>
        /// Получить список соревнований
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CompetitionDto>>>> GetCompetitions()
        {
            try
            {
                var competitions = await _context.Competitions
                    .Include(c => c.CreatedBy)
                    .Include(c => c.ResultsCreatedBy)
                    .Include(c => c.ResultsUpdatedBy)
                    .Include(c => c.Teams)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                var result = competitions.Select(c => new CompetitionDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    CreatedAt = c.CreatedAt,
                    CreatedById = c.CreatedById,
                    CreatedByUsername = c.CreatedBy.Username,
                    IsArchived = c.IsArchived,
                    HasResults = c.HasResults,
                    ResultsCreatedAt = c.ResultsCreatedAt,
                    ResultsCreatedById = c.ResultsCreatedById,
                    ResultsCreatedByUsername = c.ResultsCreatedBy?.Username,
                    ResultsUpdatedAt = c.ResultsUpdatedAt,
                    ResultsUpdatedById = c.ResultsUpdatedById,
                    ResultsUpdatedByUsername = c.ResultsUpdatedBy?.Username,
                    Teams = c.Teams.Select(t => new TeamDto
                    {
                        Id = t.Id,
                        CompetitionId = t.CompetitionId,
                        Name = t.Name
                    }).ToList()
                }).ToList();

                return HandleResult(result);
            }
            catch (Exception ex)
            {
                return HandleError<List<CompetitionDto>>("Ошибка при получении списка соревнований");
            }
        }

        /// <summary>
        /// Получить соревнование по ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CompetitionDto>>> GetCompetition(int id)
        {
            try
            {
                var competition = await _context.Competitions
                    .Include(c => c.CreatedBy)
                    .Include(c => c.ResultsCreatedBy)
                    .Include(c => c.ResultsUpdatedBy)
                    .Include(c => c.Teams)
                    .ThenInclude(t=>t.Results)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (competition == null)
                    return HandleNotFound<CompetitionDto>("Соревнование не найдено");

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
                    IsArchived = competition.IsArchived,
                    HasResults = competition.HasResults,
                    ResultsCreatedAt = competition.ResultsCreatedAt,
                    ResultsCreatedById = competition.ResultsCreatedById,
                    ResultsCreatedByUsername = competition.ResultsCreatedBy?.Username,
                    ResultsUpdatedAt = competition.ResultsUpdatedAt,
                    ResultsUpdatedById = competition.ResultsUpdatedById,
                    ResultsUpdatedByUsername = competition.ResultsUpdatedBy?.Username,
                    Teams = competition.Teams.Select(t => new TeamDto
                    {
                        Id = t.Id,
                        CompetitionId = t.CompetitionId,
                        Name = t.Name
                    }).ToList()
                };

                return HandleResult(result);
            }
            catch (Exception ex)
            {
                return HandleError<CompetitionDto>("Ошибка при получении соревнования");
            }
        }

        /// <summary>
        /// Создать новое соревнование (только организатор)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse>> CreateCompetition([FromBody] CreateCompetitionDto dto)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user?.RoleId != (int)Roles.Organizer && user?.RoleId != (int)Roles.Admin)
                    return HandleForbidden("Недостаточно прав для создания соревнования");

                var competition = new Competition
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    CreatedById = userId,
                    CreatedAt = DateTime.Now
                };

                _context.Competitions.Add(competition);
                await _context.SaveChangesAsync();

                // Создание уведомления для всех организаторов
                try
                {
                    await _notificationHelper.NotifyOrganizersAboutNewCompetition(
                        competition.Id,
                        competition.Name,
                        user.Username);
                }
                catch (Exception ex) { }

                return HandleSuccess("Соревнование успешно создано");
            }
            catch (Exception ex)
            {
                return HandleError("Ошибка при создании соревнования");
            }
        }

        /// <summary>
        /// Создать соревнование с этапами
        /// </summary>
        [HttpPost("create-with-stages")]
        public async Task<ActionResult<ApiResponse>> CreateCompetitionWithStages([FromBody] CreateCompetitionWithStagesDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user?.RoleId != (int)Roles.Organizer && user?.RoleId != (int)Roles.Admin)
                    return HandleForbidden("Недостаточно прав для создания соревнования");

                // Создаем соревнование
                var competition = new Competition
                {
                    Name = dto.Competition.Name,
                    Description = dto.Competition.Description,
                    StartDate = dto.Competition.StartDate,
                    EndDate = dto.Competition.EndDate,
                    CreatedById = userId,
                    CreatedAt = DateTime.Now
                };

                _context.Competitions.Add(competition);
                await _context.SaveChangesAsync();

                // Создаем этапы с переданными временами
                if (dto.Stages != null && dto.Stages.Any())
                {
                    foreach (var stageDto in dto.Stages.OrderBy(s => s.Order))
                    {
                        var stage = new Stage
                        {
                            CompetitionId = competition.Id,
                            Name = stageDto.Name,
                            Description = stageDto.Description,
                            Location = stageDto.Location,
                            Order = stageDto.Order,
                            IsFinal = stageDto.IsFinal,
                            CreatedAt = DateTime.Now,
                            IsStartNotified = false,
                            StartTime = stageDto.StartTime,
                            EndTime = stageDto.EndTime
                        };
                        _context.Stages.Add(stage);
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Уведомление организаторов
                try
                {
                    await _notificationHelper.NotifyOrganizersAboutNewCompetition(competition.Id, competition.Name, user.Username);
                }
                catch
                {
                    return HandleSuccess("Соревнование успешно создано \n!Ошибка отправки уведомления!");
                }

                return HandleSuccess("Соревнование успешно создано");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return HandleError($"Ошибка создания соревнования: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновить соревнование (только организатор)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse>> UpdateCompetition(
            int id,
            [FromBody] CreateCompetitionDto dto)
        {
            try
            {
                var competition = await _context.Competitions.FindAsync(id);
                if (competition == null)
                    return HandleNotFound("Соревнование не найдено");

                if (competition.IsArchived)
                    return HandleError("Невозможно редактировать соревнование, так как оно находится в архиве");

                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user?.RoleId != (int)Roles.Organizer && user?.RoleId != (int)Roles.Admin)
                    return HandleForbidden("Недостаточно прав для редактирования соревнования");

                competition.Name = dto.Name;
                competition.Description = dto.Description;
                competition.StartDate = dto.StartDate;
                competition.EndDate = dto.EndDate;

                await _context.SaveChangesAsync();

                return HandleSuccess("Соревнование успешно обновлено");
            }
            catch (Exception ex)
            {
                return HandleError("Ошибка при обновлении соревнования");
            }
        }

        /// <summary>
        /// Обновить соревнование с этапами
        /// </summary>
        [HttpPut("{id}/update-with-stages")]
        public async Task<ActionResult<ApiResponse>> UpdateCompetitionWithStages(int id, [FromBody] UpdateCompetitionWithStagesDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user?.RoleId != (int)Roles.Organizer && user?.RoleId != (int)Roles.Admin)
                    return HandleForbidden("Недостаточно прав для редактирования соревнования");

                var competition = await _context.Competitions.FindAsync(id);
                if (competition == null)
                    return HandleNotFound("Соревнование не найдено");

                if (competition.IsArchived)
                    return HandleError("Невозможно редактировать соревнование, так как оно находится в архиве");

                // Обновляем соревнование
                competition.Name = dto.Competition.Name;
                competition.Description = dto.Competition.Description;
                competition.StartDate = dto.Competition.StartDate;
                competition.EndDate = dto.Competition.EndDate;

                // Получаем существующие этапы
                var existingStages = await _context.Stages
                    .Where(s => s.CompetitionId == id)
                    .ToDictionaryAsync(s => s.Id);

                // Обновляем, добавляем, удаляем этапы
                foreach (var stageDto in dto.Stages)
                {
                    if (stageDto.Id.HasValue && existingStages.ContainsKey(stageDto.Id.Value))
                    {
                        // Обновляем существующий этап
                        var stage = existingStages[stageDto.Id.Value];
                        stage.Name = stageDto.Name;
                        stage.Description = stageDto.Description;
                        stage.Location = stageDto.Location;
                        stage.Order = stageDto.Order;
                        stage.IsFinal = stageDto.IsFinal;
                        stage.StartTime = stageDto.StartTime;  // Обновляем время начала
                        stage.EndTime = stageDto.EndTime;      // Обновляем время окончания
                        existingStages.Remove(stageDto.Id.Value);
                    }
                    else if (!stageDto.Id.HasValue)
                    {
                        // Добавляем новый этап
                        _context.Stages.Add(new Stage
                        {
                            CompetitionId = id,
                            Name = stageDto.Name,
                            Description = stageDto.Description,
                            Location = stageDto.Location,
                            Order = stageDto.Order,
                            IsFinal = stageDto.IsFinal,
                            CreatedAt = DateTime.Now,
                            IsStartNotified = false,
                            StartTime = stageDto.StartTime,  // Сохраняем время начала
                            EndTime = stageDto.EndTime       // Сохраняем время окончания
                        });
                    }
                }

                // Удаляем этапы, которых нет в запросе
                foreach (var stageToDelete in existingStages.Values)
                {
                    _context.Stages.Remove(stageToDelete);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return HandleSuccess("Соревнование успешно обновлено");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return HandleError($"Ошибка обновления соревнования: {ex.Message}");
            }
        }

        /// <summary>
        /// Удалить соревнование и все связанные данные
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteCompetition(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            // Собираем данные для уведомлений
            string competitionName = "";
            string deletedBy = "";
            List<int> allParticipantIds = new List<int>();
            Dictionary<int, string> teamNamesDict = new Dictionary<int, string>();
            Dictionary<int, List<int>> teamMembersDict = new Dictionary<int, List<int>>();
            List<string> notificationErrors = new List<string>();

            try
            {
                var userId = GetUserId();
                var currentUser = await _context.Users.FindAsync(userId);

                if (currentUser?.RoleId != (int)Roles.Organizer && currentUser?.RoleId != (int)Roles.Admin)
                    return HandleForbidden("Недостаточно прав для удаления соревнования");

                var competition = await _context.Competitions
                    .Include(c => c.Teams)
                        .ThenInclude(t => t.Users)
                    .Include(c => c.Teams)
                        .ThenInclude(t => t.Tasks)
                            .ThenInclude(task => task.Chat)
                                .ThenInclude(chat => chat.Messages)
                    .Include(c => c.Teams)
                        .ThenInclude(t => t.Chat)
                            .ThenInclude(chat => chat.Messages)
                    .Include(c => c.Stages)
                    .Include(c => c.Results)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (competition == null)
                    return HandleNotFound("Соревнование не найдено");

                // Сохраняем данные для уведомлений
                competitionName = competition.Name;
                deletedBy = currentUser.Username;

                foreach (var team in competition.Teams)
                {
                    var memberIds = team.Users.Select(u => u.Id).ToList();
                    teamMembersDict[team.Id] = memberIds;
                    teamNamesDict[team.Id] = team.Name;
                    allParticipantIds.AddRange(memberIds);
                }
                allParticipantIds = allParticipantIds.Distinct().ToList();

                // 1. Удаляем все уведомления, связанные с соревнованием и его командами
                var notificationIds = await _context.Notifications
                    .Where(n => n.RelatedEntityType == "competition" && n.RelatedEntityId == id ||
                               n.RelatedEntityType == "team" && competition.Teams.Select(t => t.Id).Contains(n.RelatedEntityId ?? 0))
                    .Select(n => n.Id)
                    .ToListAsync();

                if (notificationIds.Any())
                {
                    var notificationsToDelete = await _context.Notifications
                        .Where(n => notificationIds.Contains(n.Id))
                        .ToListAsync();
                    _context.Notifications.RemoveRange(notificationsToDelete);
                }

                // 2. Удаляем финальные составы команд
                var finalTeamMembers = await _context.FinalTeamMembers
                    .Where(f => competition.Teams.Select(t => t.Id).Contains(f.TeamId))
                    .ToListAsync();
                if (finalTeamMembers.Any())
                {
                    _context.FinalTeamMembers.RemoveRange(finalTeamMembers);
                }

                // 3. Обрабатываем каждую команду
                foreach (var team in competition.Teams)
                {
                    // Очищаем участников команды
                    foreach (var user in team.Users)
                    {
                        user.TeamId = null;
                        if (user.RoleId == (int)Roles.Captain)
                        {
                            user.RoleId = (int)Roles.Member;
                        }
                    }

                    // Удаляем задачи и их чаты
                    foreach (var task in team.Tasks)
                    {
                        if (task.Chat != null)
                        {
                            if (task.Chat.Messages != null && task.Chat.Messages.Any())
                            {
                                _context.Messages.RemoveRange(task.Chat.Messages);
                            }
                            _context.Chats.Remove(task.Chat);
                        }
                    }
                    _context.Tasks.RemoveRange(team.Tasks);

                    // Удаляем чат команды
                    if (team.Chat != null)
                    {
                        if (team.Chat.Messages != null && team.Chat.Messages.Any())
                        {
                            _context.Messages.RemoveRange(team.Chat.Messages);
                        }
                        _context.Chats.Remove(team.Chat);
                    }
                }

                // 4. Удаляем результаты соревнования
                if (competition.Results != null && competition.Results.Any())
                {
                    _context.Results.RemoveRange(competition.Results);
                }

                // 5. Удаляем этапы
                if (competition.Stages != null && competition.Stages.Any())
                {
                    _context.Stages.RemoveRange(competition.Stages);
                }

                // 6. Удаляем команды
                if (competition.Teams != null && competition.Teams.Any())
                {
                    _context.Teams.RemoveRange(competition.Teams);
                }

                // 7. Удаляем само соревнование
                _context.Competitions.Remove(competition);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 8. Отправляем уведомления ПОСЛЕ успешного сохранения

                // Уведомляем участников всех команд
                foreach (var teamId in teamMembersDict.Keys)
                {
                    var memberIds = teamMembersDict[teamId];
                    var teamName = teamNamesDict[teamId];

                    if (memberIds.Any())
                    {
                        try
                        {
                            await _notificationHelper.NotifyTeamDisbanded(memberIds, teamName, deletedBy);
                        }
                        catch (Exception ex)
                        {
                            notificationErrors.Add($"• Команда \"{teamName}\" (ID: {teamId})");
                        }
                    }
                }

                // Уведомляем организаторов
                try
                {
                    await _notificationHelper.NotifyCompetitionDeleted(id, competitionName, deletedBy);
                }
                catch (Exception ex)
                {
                    notificationErrors.Add("• Уведомления для организаторов");
                }

                // Формируем ответ
                if (notificationErrors.Any())
                {
                    var errorMessage = $"Соревнование \"{competitionName}\" успешно удалено.\n\n⚠️ Ошибки при отправке уведомлений:\n" + string.Join("\n", notificationErrors);
                    return HandleSuccess(errorMessage);
                }

                return HandleSuccess($"Соревнование \"{competitionName}\" успешно удалено");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return HandleError($"Ошибка удаления соревнования: {ex.Message}");
            }
        }

        /// <summary>
        /// Архивировать соревнование и очистить связанные данные
        /// </summary>
        [HttpPost("{id}/archive")]
        public async Task<ActionResult<ApiResponse>> ArchiveCompetition(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            // Словарь для хранения участников по командам (для уведомлений)
            Dictionary<int, List<int>> teamMembersDict = new Dictionary<int, List<int>>();
            Dictionary<int, string> teamNamesDict = new Dictionary<int, string>();
            List<string> notificationErrors = new List<string>();
            string archivedBy = "";
            string competitionName = "";

            try
            {
                var userId = GetUserId();
                var currentUser = await _context.Users.FindAsync(userId);

                if (currentUser?.RoleId != (int)Roles.Organizer && currentUser?.RoleId != (int)Roles.Admin)
                    return HandleForbidden("Недостаточно прав для архивирования соревнования");

                archivedBy = currentUser.Username;

                var competition = await _context.Competitions
                    .Include(c => c.Teams)
                        .ThenInclude(t => t.Users)
                    .Include(c => c.Teams)
                        .ThenInclude(t => t.Tasks)
                            .ThenInclude(task => task.Chat)
                                .ThenInclude(chat => chat.Messages)
                    .Include(c => c.Teams)
                        .ThenInclude(t => t.Chat)
                            .ThenInclude(chat => chat.Messages)
                    .Include(c => c.Stages)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (competition == null)
                    return HandleNotFound("Соревнование не найдено");

                if (competition.IsArchived)
                    return HandleError("Соревнование уже архивировано");

                competitionName = competition.Name;

                // Сохраняем данные для уведомлений
                foreach (var team in competition.Teams)
                {
                    teamMembersDict[team.Id] = team.Users.Select(u => u.Id).ToList();
                    teamNamesDict[team.Id] = team.Name;
                }

                // Очищаем данные в командах
                foreach (var team in competition.Teams)
                {
                    // Очищаем участников команды
                    foreach (var user in team.Users)
                    {
                        user.TeamId = null;
                        if (user.RoleId == (int)Roles.Captain)
                        {
                            user.RoleId = (int)Roles.Member;
                        }
                    }

                    // Удаляем задачи и их чаты
                    foreach (var task in team.Tasks)
                    {
                        if (task.Chat != null)
                        {
                            if (task.Chat.Messages != null && task.Chat.Messages.Any())
                            {
                                _context.Messages.RemoveRange(task.Chat.Messages);
                            }
                            _context.Chats.Remove(task.Chat);
                        }
                    }
                    _context.Tasks.RemoveRange(team.Tasks);

                    // Очищаем чат команды
                    if (team.Chat != null)
                    {
                        if (team.Chat.Messages != null && team.Chat.Messages.Any())
                        {
                            _context.Messages.RemoveRange(team.Chat.Messages);
                        }
                        _context.Chats.Remove(team.Chat);
                    }

                    // Очищаем GitHub репозиторий
                    team.GitRepoName = null;
                }

                // Удаляем этапы
                if (competition.Stages != null && competition.Stages.Any())
                {
                    _context.Stages.RemoveRange(competition.Stages);
                }

                // Устанавливаем флаг архивации
                competition.IsArchived = true;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Отправляем уведомления участникам всех команд
                foreach (var teamId in teamMembersDict.Keys)
                {
                    var memberIds = teamMembersDict[teamId];
                    var teamName = teamNamesDict[teamId];

                    if (memberIds.Any())
                    {
                        try
                        {
                            await _notificationHelper.NotifyTeamDisbanded(memberIds, teamName, archivedBy);
                        }
                        catch (Exception ex)
                        {
                            notificationErrors.Add($"• Команда \"{teamName}\" (ID: {teamId})");
                        }
                    }
                }

                // Отправляем уведомления организаторам
                try
                {
                    await _notificationHelper.NotifyCompetitionArchived(id, competitionName, archivedBy);
                }
                catch (Exception ex)
                {
                    notificationErrors.Add("• Уведомления для организаторов");
                }

                // Формируем ответ
                if (notificationErrors.Any())
                {
                    var errorMessage = "Соревнование успешно архивировано.\n\n⚠️ Ошибки при отправке уведомлений:\n" + string.Join("\n", notificationErrors);
                    return HandleSuccess(errorMessage);
                }

                return HandleSuccess("Соревнование успешно архивировано");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return HandleError($"Ошибка архивирования: {ex.Message}");
            }
        }

        /// <summary>
        /// Сохранить все результаты соревнования (массовое сохранение)
        /// </summary>
        [HttpPost("{id}/save-all-results")]
        public async Task<ActionResult<ApiResponse>> SaveAllResults(int id, [FromBody] List<SaveTeamResultDto> results)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var userId = GetUserId();
                var currentUser = await _context.Users.FindAsync(userId);

                if (currentUser?.RoleId != (int)Roles.Organizer && currentUser?.RoleId != (int)Roles.Admin)
                    return HandleForbidden("Недостаточно прав для подведения итогов");

                var competition = await _context.Competitions
                    .Include(c => c.Teams)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (competition == null)
                    return HandleNotFound("Соревнование не найдено");

                if (competition.IsArchived)
                    return HandleError("Невозможно подвести итоги, так как соревнование находится в архиве");

                bool hadResults = competition.HasResults;

                // Удаляем старые результаты
                var existingResults = await _context.Results
                    .Where(r => r.CompetitionId == id)
                    .ToDictionaryAsync(r => r.TeamId);

                // Сохраняем результаты
                foreach (var dto in results)
                {
                    if (existingResults.TryGetValue(dto.TeamId, out var existingResult))
                    {
                        // Обновляем существующий результат
                        existingResult.Place = dto.Place;
                        existingResult.PlaceDisplay = GetPlaceDisplay(dto.Place, competition.Teams.Count);
                        existingResult.Comment = dto.Comment ?? "";
                    }
                    else
                    {
                        // Создаем новый результат
                        var newResult = new Models.Result
                        {
                            CompetitionId = id,
                            TeamId = dto.TeamId,
                            Place = dto.Place,
                            PlaceDisplay = GetPlaceDisplay(dto.Place, competition.Teams.Count),
                            Comment = dto.Comment ?? ""
                        };
                        _context.Results.Add(newResult);

                        await FixTeamMembersAsync(dto.TeamId);
                    }
                }

                if (!competition.HasResults)
                {
                    // Первое создание результатов
                    competition.HasResults = true;
                    competition.ResultsCreatedAt = DateTime.Now;
                    competition.ResultsCreatedById = userId;
                    competition.ResultsUpdatedAt = null;
                    competition.ResultsUpdatedById = null;
                }
                else
                {
                    // Обновление результатов
                    competition.ResultsUpdatedAt = DateTime.Now;
                    competition.ResultsUpdatedById = userId;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var message = hadResults
                   ? "Результаты успешно обновлены и опубликованы"
                   : "Результаты успешно сохранены и опубликованы";

                try
                {
                    if (!hadResults)
                    {
                        // Первая публикация результатов
                        await _notificationHelper.NotifyCompetitionResultsPublished(id, competition.Name);
                    }
                    else
                    {
                        // Обновление существующих результатов
                        await _notificationHelper.NotifyCompetitionResultsUpdated(id, competition.Name);
                    }
                }
                catch (Exception ex)
                {
                    return HandleSuccess(message + "\n!Ошибка отправки уведомления!");
                }

                return HandleSuccess(message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return HandleError($"Ошибка сохранения результатов: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить расписание соревнования
        /// </summary>
        [HttpGet("{competitionId}/stages")]
        public async Task<ActionResult<ApiResponse<List<StageDto>>>> GetCompetitionStages(int competitionId)
        {
            try
            {
                var competition = await _context.Competitions.FindAsync(competitionId);
                if (competition == null)
                    return HandleNotFound<List<StageDto>>("Соревнование не найдено");

                var stages = await _context.Stages
                    .Where(s => s.CompetitionId == competitionId)
                    .OrderBy(s => s.Order)
                    .Select(s => new StageDto
                    {
                        Id = s.Id,
                        CompetitionId = s.CompetitionId,
                        Name = s.Name,
                        Description = s.Description,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        Location = s.Location,
                        Order = s.Order,
                        IsFinal = s.IsFinal,
                        IsStartNotified = s.IsStartNotified
                    })
                    .ToListAsync();

                return HandleResult(stages);
            }
            catch (Exception ex)
            {
                return HandleError<List<StageDto>>($"Ошибка получения расписания: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить результаты соревнования (для просмотра или редактирования)
        /// </summary>
        [HttpGet("{id}/results")]
        public async Task<ActionResult<ApiResponse<List<TeamResultDto>>>> GetCompetitionResults(int id)
        {
            try
            {
                var competition = await _context.Competitions
                    .Include(c => c.Results)
                        .ThenInclude(r => r.Team)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (competition == null)
                    return HandleNotFound<List<TeamResultDto>>("Соревнование не найдено");

                if (!competition.HasResults)
                    return HandleError<List<TeamResultDto>>("Результаты еще не подведены");

                var results = competition.Results
                    .OrderBy(r => r.Place)
                    .Select(r => new TeamResultDto
                    {
                        TeamId = r.TeamId,
                        TeamName = r.Team.Name,
                        Place = r.Place,
                        PlaceDisplay = r.PlaceDisplay,
                        Comment = r.Comment,
                        IsSaved = true,
                        MembersCount = r.Team.Users.Count.ToString()
                    })
                    .ToList();

                return HandleResult(results);
            }
            catch (Exception ex)
            {
                return HandleError<List<TeamResultDto>>($"Ошибка получения результатов: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task FixTeamMembersAsync(int teamId)
        {
            try
            {
                var team = await _context.Teams
                    .Include(t => t.Users)
                    .ThenInclude(u => u.Position)
                    .FirstOrDefaultAsync(t => t.Id == teamId);

                if (team == null) return;

                // Фиксируем текущий состав
                foreach (var user in team.Users)
                {
                    var finalMember = new FinalTeamMember
                    {
                        TeamId = teamId,
                        UserId = user.Id,
                        Username = user.Username,
                        PositionName = user.Position?.Name ?? "Не указана",
                        RoleId = user.RoleId,
                        FixedAt = DateTime.Now
                    };
                    _context.FinalTeamMembers.Add(finalMember);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка фиксации состава: {ex.Message}");
            }
        }

        private string GetPlaceDisplay(int place, int count)
        {
            return place switch
            {
                1 => $"🥇 1/{count}",
                2 => $"🥈 2/{count}",
                3 => $"🥉 3/{count}",
                _ => $"{place}/{count}"
            };
        }

        /// <summary>
        /// Создать команду в соревновании (только организатор)
        /// </summary>
        [HttpPost("{id}/teams")]
        public async Task<ActionResult<ApiResponse>> CreateTeamInCompetition(
            int id,
            [FromBody] CreateTeamInCompetitionDto dto)
        {
            try
            {
                var competition = await _context.Competitions.FindAsync(id);
                if (competition == null)
                    return HandleNotFound("Соревнование не найдено");

                if (competition.IsArchived)
                    return HandleError("Невозможно создать команду, так как соревнование находится в архиве");

                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user?.RoleId != (int)Roles.Organizer && user?.RoleId != (int)Roles.Admin)
                    return HandleForbidden("Недостаточно прав для создания команды");

                // Проверка существования команды с таким названием
                var teamExists = await _context.Teams
                    .AnyAsync(t => t.Name.ToLower() == dto.Name.Trim().ToLower() &&
                                   t.CompetitionId == id);

                if (teamExists)
                    return HandleError("Команда с таким названием уже существует в этом соревновании");

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Создание чата для команды
                    var teamChat = new Chat
                    {
                        Name = $"Чат команды {dto.Name.Trim()}",
                        TypeId = (int)ChatTypes.TeamChat, // Используем enum
                        CreatedAt = DateTime.Now
                    };
                    _context.Chats.Add(teamChat);
                    await _context.SaveChangesAsync();

                    // Создание команды
                    var team = new Team
                    {
                        Name = dto.Name.Trim(),
                        CompetitionId = id,
                        InviteCode = Guid.NewGuid().ToString(),
                        ChatId = teamChat.Id,
                        CreatedAt = DateTime.Now
                    };

                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();

                    // Приветственное сообщение в чат
                    var welcomeMessage = new Message
                    {
                        ChatId = teamChat.Id,
                        UserId = userId,
                        Text = $"Команда \"{dto.Name.Trim()}\" создана! Добро пожаловать в общий чат команды.",
                        SentAt = DateTime.Now
                    };
                    _context.Messages.Add(welcomeMessage);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // Уведомление о новой команде
                    try
                    {
                        await _notificationHelper.NotifyOrganizersAboutNewTeam(
                            competition.Id,
                            team.Id,
                            team.Name,
                            competition.Name,
                            user.Username);
                    }
                    catch (Exception ex) { }

                    return HandleSuccess("Команда успешно создана");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return HandleError("Ошибка при создании команды");
            }
        }
    }
}