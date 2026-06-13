using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Helpers;
using HackathonCoordinator.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace HackathonCoordinator.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly HackathonCoordinatorContext _context;
        private readonly NotificationHelperService _notificationHelper;
        private readonly IEncryptionService _encryptionService;

        public UsersController(HackathonCoordinatorContext context,
            IEncryptionService encryptionService,
            NotificationHelperService notificationHelper)
        {
            _context = context;
            _encryptionService = encryptionService;
            _notificationHelper = notificationHelper;
        }

        /// <summary>
        /// Получить данные текущего пользователя
        /// </summary>
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return HandleUnauthorized<UserDto>("Пользователь не авторизован");

                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .Include(u => u.Team)
                    .Include(u => u.ProfileIcon)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Email = u.Email,
                        RoleId = u.RoleId,
                        GitHubUsername = u.GitHubUsername,
                        GitHubAccessToken = u.GitHubAccessToken,
                        TeamId = u.TeamId,
                        TeamName = u.Team != null ? u.Team.Name : null,
                        IconId = u.ProfileIconId,
                        IconName = u.ProfileIcon != null ? u.ProfileIcon.Name : null
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                    return HandleNotFound<UserDto>("Пользователь не найден");

                return HandleResult(user);
            }
            catch (DbUpdateException ex)
            {
                return HandleError<UserDto>("Ошибка базы данных при получении данных пользователя");
            }
            catch (InvalidOperationException ex)
            {
                return HandleUnauthorized<UserDto>("Пользователь не найден");
            }
            catch (Exception ex)
            {
                return HandleError<UserDto>("Внутренняя ошибка сервера при получении данных пользователя");
            }
        }

        /// <summary>
        /// Обновить профиль пользователя
        /// </summary>
        [HttpPut("me/update")]
        public async Task<ActionResult<ApiResponse>> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return HandleUnauthorized("Пользователь не авторизован");

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return HandleNotFound("Пользователь не найден");

                if (!string.IsNullOrWhiteSpace(dto.Username))
                    user.Username = dto.Username;

                if (dto.IconId.HasValue)
                {
                    var iconExists = await _context.ProfileIcons.AnyAsync(i => i.Id == dto.IconId.Value);
                    if (!iconExists)
                        return HandleError("Иконка не найдена");

                    user.ProfileIconId = dto.IconId.Value;
                }

                await _context.SaveChangesAsync();
                return HandleSuccess("Профиль обновлён");
            }
            catch (DbUpdateException ex)
            {
                return HandleError("Ошибка базы данных при обновлении профиля");
            }
            catch (ArgumentNullException ex)
            {
                return HandleError("Некорректные данные профиля");
            }
            catch (Exception ex)
            {
                return HandleError("Внутренняя ошибка сервера при обновлении профиля");
            }
        }

        /// <summary>
        /// Получить расширенный профиль пользователя с результатами
        /// </summary>
        [HttpGet("{userId}/extended")]
        public async Task<ActionResult<ApiResponse<UserProfileExtendedDto>>> GetUserProfileExtended(int userId)
        {
            try
            {
                var currentUserId = GetUserId();
                var currentUser = await _context.Users.FindAsync(currentUserId);

                if (currentUser == null)
                    return HandleUnauthorized<UserProfileExtendedDto>("Пользователь не авторизован");

                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .Include(u => u.Role)
                    .Include(u => u.Team)
                    .Include(u => u.ProfileIcon)
                    .Include(u => u.Position)
                    .Select(u => new UserProfileExtendedDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Email = u.Email,
                        RoleId = u.RoleId,
                        RoleName = u.Role.Name,
                        PositionId = u.PositionId,
                        PositionName = u.Position != null ? u.Position.Name : null,
                        TeamId = u.TeamId,
                        TeamName = u.Team != null ? u.Team.Name : null,
                        GitHubUsername = u.GitHubUsername,
                        IconId = u.ProfileIconId,
                        IconName = u.ProfileIcon != null ? u.ProfileIcon.Name : null,
                        IsCurrentUser = userId == currentUserId
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                    return HandleNotFound<UserProfileExtendedDto>("Пользователь не найден");

                // Получаем результаты пользователя
                var userResults = await _context.Results
                    .Where(r => r.Team.FinalTeamMembers.Any(m => m.UserId == userId))
                    .Include(r => r.Competition)
                    .Include(r => r.Team)
                        .ThenInclude(t => t.FinalTeamMembers)
                            .ThenInclude(ftm => ftm.Role)
                    .OrderByDescending(r => r.Competition.CreatedAt)
                    .Select(r => new UserResultDto
                    {
                        CompetitionId = r.CompetitionId,
                        CompetitionName = r.Competition.Name,
                        TeamId = r.TeamId,
                        TeamName = r.Team.Name,
                        Place = r.Place,
                        PlaceDisplay = r.PlaceDisplay,
                        Comment = r.Comment,
                        CreatedAt = r.Competition.CreatedAt,
                        FinalTeamMembers = r.Team.FinalTeamMembers
                            .Select(ftm => new FinalTeamMemberDto
                            {
                                Id = ftm.Id,
                                UserId = ftm.UserId,
                                Username = ftm.Username,
                                PositionName = ftm.PositionName,
                                RoleId = ftm.RoleId,
                                RoleName = ftm.Role.Name,
                                FixedAt = ftm.FixedAt
                            })
                            .ToList()
                    })
                    .ToListAsync();

                user.Results = userResults;

                return HandleResult(user);
            }
            catch (Exception ex)
            {
                return HandleError<UserProfileExtendedDto>($"Ошибка при получении профиля: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновить должность текущего пользователя
        /// </summary>
        [HttpPut("me/position")]
        public async Task<ActionResult<ApiResponse>> UpdateUserPosition([FromBody] ChangeUserPositioDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return HandleUnauthorized("Пользователь не авторизован");

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return HandleNotFound("Пользователь не найден");

                var position = await _context.Positions.FindAsync(dto.PositionId);
                if (position == null)
                    return HandleError("Должность не найдена");

                user.PositionId = dto.PositionId;
                await _context.SaveChangesAsync();

                return HandleSuccess("Должность обновлена");
            }
            catch (Exception ex)
            {
                return HandleError($"Ошибка обновления должности: {ex.Message}");
            }
        }

        /// <summary>
        /// Привязать GitHub аккаунт
        /// </summary>
        [HttpPost("me/github/link")]
        public async Task<ActionResult<ApiResponse>> LinkGitHubAccount([FromBody] LinkGitHubDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return HandleUnauthorized();

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return HandleNotFound("Пользователь не найден");

                user.GitHubUsername = dto.GitHubUsername;
                user.GitHubAccessToken = _encryptionService.Encrypt(dto.GitHubAccessToken);
                user.GitHubAvatarUrl = dto.GitHubAvatarUrl;

                await _context.SaveChangesAsync();

                return HandleSuccess("GitHub аккаунт успешно привязан");
            }
            catch (DbUpdateException ex)
            {
                return HandleError("Ошибка базы данных при привязке GitHub аккаунта");
            }
            catch (CryptographicException ex)
            {
                return HandleError("Ошибка шифрования GitHub токена");
            }
            catch (ArgumentNullException ex)
            {
                return HandleError("Некорректные данные GitHub аккаунта");
            }
            catch (Exception ex)
            {
                return HandleError("Внутренняя ошибка сервера при привязке GitHub аккаунта");
            }
        }

        /// <summary>
        /// Отвязать GitHub аккаунт
        /// </summary>
        [HttpPost("me/github/unlink")]
        public async Task<ActionResult<ApiResponse>> UnlinkGitHubAccount()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return HandleUnauthorized();

                var user = await _context.Users
                    .Include(u => u.Team)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return HandleNotFound("Пользователь не найден");

                // Если пользователь - капитан команды, очищаем GitHub репозиторий команды
                if (user.RoleId == (int)Roles.Captain && user.TeamId.HasValue)
                {
                    var team = await _context.Teams.FindAsync(user.TeamId.Value);
                    if (team != null)
                    {
                        team.GitRepoName = null;
                    }
                }

                user.GitHubUsername = null;
                user.GitHubAccessToken = null;
                user.GitHubAvatarUrl = null;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return HandleSuccess("GitHub аккаунт отвязан");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return HandleError($"Ошибка при отвязке GitHub аккаунта: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить GitHub токен (расшифрованный)
        /// </summary>
        [HttpGet("me/github/token")]
        public async Task<ActionResult<ApiResponse<GitHubTokenResponseDto>>> GetGitHubToken()
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return HandleUnauthorized<GitHubTokenResponseDto>();

                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.GitHubAccessToken })
                    .FirstOrDefaultAsync();

                if (user == null || string.IsNullOrEmpty(user.GitHubAccessToken))
                    return HandleNotFound<GitHubTokenResponseDto>("GitHub токен не найден");

                var decryptedToken = _encryptionService.Decrypt(user.GitHubAccessToken);

                return HandleResult(new GitHubTokenResponseDto { AccessToken = decryptedToken });
            }
            catch (DbUpdateException ex)
            {
                return HandleError<GitHubTokenResponseDto>("Ошибка базы данных при получении GitHub токена");
            }
            catch (CryptographicException ex)
            {
                return HandleError<GitHubTokenResponseDto>("Ошибка расшифровки GitHub токена");
            }
            catch (InvalidOperationException ex)
            {
                return HandleUnauthorized<GitHubTokenResponseDto>("Пользователь не найден");
            }
            catch (Exception ex)
            {
                return HandleError<GitHubTokenResponseDto>("Внутренняя ошибка сервера при получении GitHub токена");
            }
        }

        /// <summary>
        /// Получить данные всех пользователей
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetAllUsers()
        {
            try
            {
                var userId = GetUserId();
                var currentUser = await _context.Users.FindAsync(userId);

                if (currentUser == null)
                    return HandleUnauthorized<List<UserDto>>("Пользователь не найден");

                if (currentUser.RoleId != (int)Roles.Organizer && currentUser.RoleId != (int)Roles.Admin)
                    return HandleForbidden<List<UserDto>>("Недостаточно прав для просмотра всех пользователей");

                var users = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Team)
                    .Include(u => u.ProfileIcon)
                    .Include(u => u.Position)
                    .OrderBy(u => u.Username)
                    .ToListAsync();

                var result = users.Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    RoleId = u.RoleId,
                    RoleName = u.Role.Name,
                    PositionId = u.PositionId,
                    PositionName = u.Position.Name,
                    TeamId = u.TeamId,
                    TeamName = u.Team?.Name,
                    GitHubUsername = u.GitHubUsername,
                    IconName = u.ProfileIcon?.Name
                }).ToList();

                return HandleResult(result);
            }
            catch (DbUpdateException ex)
            {
                return HandleError<List<UserDto>>("Ошибка базы данных при получении списка пользователей");
            }
            catch (InvalidOperationException ex)
            {
                return HandleUnauthorized<List<UserDto>>("Пользователь не найден");
            }
            catch (Exception ex)
            {
                return HandleError<List<UserDto>>("Внутренняя ошибка сервера при получении списка пользователей");
            }
        }

        /// <summary>
        /// Назначить пользователя организатором (только для администратора)
        /// </summary>
        [HttpPost("{userId}/make-organizer")]
        public async Task<ActionResult<ApiResponse>> MakeOrganizer(int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var currentUserId = GetUserId();
                var currentUser = await _context.Users.FindAsync(currentUserId);

                if (currentUser?.RoleId != (int)Roles.Admin)
                    return HandleForbidden("Только администратор может назначать организаторов");

                var user = await _context.Users
                    .Include(u => u.Team)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return HandleNotFound("Пользователь не найден");

                if (user.RoleId == (int)Roles.Admin)
                    return HandleError("Нельзя изменить роль администратора");

                if (user.RoleId == (int)Roles.Organizer)
                    return HandleError("Пользователь уже является организатором");

                // Сохраняем старую роль для уведомления
                string oldRole = user.RoleId switch
                {
                    (int)Roles.Member => "Участник",
                    (int)Roles.Captain => "Капитан",
                    _ => "Пользователь"
                };

                string oldTeamName = null;

                // Если пользователь был капитаном команды, нужно очистить GitHub репозиторий команды
                if (user.RoleId == (int)Roles.Captain && user.TeamId.HasValue)
                {
                    var team = await _context.Teams.FindAsync(user.TeamId.Value);
                    if (team != null)
                    {
                        oldTeamName = team.Name;
                        team.GitRepoName = null;
                    }

                    // Очищаем связь пользователя с командой
                    user.TeamId = null;
                }

                user.RoleId = (int)Roles.Organizer;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var error = "";

                // Отправляем уведомление об изменении роли
                try
                {
                    await _notificationHelper.NotifyRoleChanged(
                        userId,
                        oldRole,
                        "Организатор",
                        currentUser.Username);
                }
                catch (Exception ex)
                {
                    error = $"\n!Ошибка отправки уведомления!";
                }

                var message = $"Пользователь {user.Username} назначен организатором";
                if (!string.IsNullOrEmpty(oldTeamName))
                {
                    message += $"\nПользователь был откреплен от команды \"{oldTeamName}\"";
                }

                return HandleSuccess(message + error);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return HandleError($"Ошибка при назначении организатора: {ex.Message}");
            }
        }

        /// <summary>
        /// Снять права организатора
        /// </summary>
        [HttpPost("{userId}/remove-organizer")]
        public async Task<ActionResult<ApiResponse>> RemoveOrganizer(int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var currentUserId = GetUserId();
                var currentUser = await _context.Users.FindAsync(currentUserId);

                if (currentUser?.RoleId != (int)Roles.Admin)
                    return HandleForbidden("Только администратор может снимать права организатора");

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return HandleNotFound("Пользователь не найден");

                if (user.RoleId != (int)Roles.Organizer)
                    return HandleError("Пользователь не является организатором");

                user.RoleId = (int)Roles.Member;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Отправляем уведомление об изменении роли
                try
                {
                    await _notificationHelper.NotifyRoleChanged(
                        userId,
                        "Организатор",
                        "Участник",
                        currentUser.Username);
                }
                catch (Exception ex)
                {
                    // Уведомление не отправилось, но основная операция выполнена
                }

                return HandleSuccess($"Права организатора сняты с пользователя {user.Username}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return HandleError($"Ошибка при снятии прав организатора: {ex.Message}");
            }
        }

        /// <summary>
        /// Удалить пользователя
        /// </summary>
        [HttpDelete("{memberId}")]
        public async Task<ActionResult<ApiResponse>> DeleteUser(int memberId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var userId = GetUserId();
                var currentUser = await _context.Users
                    .Include(u => u.Team)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (currentUser == null)
                    return HandleUnauthorized("Пользователь не найден");

                var member = await _context.Users
                    .Include(u => u.Team)
                    .FirstOrDefaultAsync(u => u.Id == memberId);

                if (member == null)
                    return HandleNotFound("Пользователь не найден");

                // Проверка прав на удаление
                bool canDelete = false;

                if (currentUser.RoleId == (int)Roles.Admin)
                {
                    if (member.RoleId == (int)Roles.Admin)
                        return HandleError("Нельзя удалить администратора");
                    canDelete = true;
                }
                else if (currentUser.RoleId == (int)Roles.Organizer)
                {
                    canDelete = member.RoleId == (int)Roles.Member;
                    if (!canDelete)
                        return HandleError("Организатор может удалять только обычных участников");
                }
                else
                {
                    return HandleForbidden("Недостаточно прав для удаления пользователя");
                }

                // Если пользователь - капитан, очищаем GitHub репозиторий команды
                if (member.RoleId == (int)Roles.Captain && member.TeamId.HasValue)
                {
                    var team = await _context.Teams.FindAsync(member.TeamId.Value);
                    if (team != null)
                    {
                        team.GitRepoName = null;
                    }
                }

                _context.Users.Remove(member);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return HandleSuccess("Пользователь успешно удален");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return HandleError($"Ошибка при удалении пользователя: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить пользователя по ID (для просмотра профиля)
        /// </summary>
        [HttpGet("{userId}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(int userId)
        {
            try
            {
                var currentUserId = GetUserId();
                var currentUser = await _context.Users.FindAsync(currentUserId);

                if (currentUser == null)
                    return HandleUnauthorized<UserDto>("Пользователь не авторизован");

                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .Include(u => u.Role)
                    .Include(u => u.Team)
                    .Include(u => u.ProfileIcon)
                    .Include(u => u.Position)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Email = u.Email,
                        RoleId = u.RoleId,
                        RoleName = u.Role.Name,
                        PositionId = u.PositionId,
                        PositionName = u.Position != null ? u.Position.Name : null,
                        TeamId = u.TeamId,
                        TeamName = u.Team != null ? u.Team.Name : null,
                        GitHubUsername = u.GitHubUsername,
                        IconId = u.ProfileIconId,
                        IconName = u.ProfileIcon != null ? u.ProfileIcon.Name : null
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                    return HandleNotFound<UserDto>("Пользователь не найден");

                return HandleResult(user);
            }
            catch (Exception ex)
            {
                return HandleError<UserDto>($"Ошибка при получении пользователя: {ex.Message}");
            }
        }
    }
}