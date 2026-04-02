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
                    Teams = c.Teams.Select(t => new TeamDto
                    {
                        Id = t.Id,
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
                    return HandleSuccess(message + "\nОшибка отправки уведомления");
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