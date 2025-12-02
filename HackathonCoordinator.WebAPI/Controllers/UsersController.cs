using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Models;
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
        private readonly IEncryptionService _encryptionService;

        public UsersController(HackathonCoordinatorContext context, IEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
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
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return HandleUnauthorized();

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return HandleNotFound("Пользователь не найден");

                user.GitHubUsername = null;
                user.GitHubAccessToken = null;
                user.GitHubAvatarUrl = null;

                await _context.SaveChangesAsync();

                return HandleSuccess("GitHub аккаунт отвязан");
            }
            catch (DbUpdateException ex)
            {
                return HandleError("Ошибка базы данных при отвязке GitHub аккаунта");
            }
            catch (InvalidOperationException ex)
            {
                return HandleUnauthorized("Пользователь не найден");
            }
            catch (Exception ex)
            {
                return HandleError("Внутренняя ошибка сервера при отвязке GitHub аккаунта");
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
                var user = await _context.Users.FindAsync(userId);

                if (user?.RoleId != 3)
                    return HandleForbidden<List<UserDto>>("Только организатор может просматривать данные всех участников");

                var users = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Team)
                    .Include(u => u.ProfileIcon)
                    .OrderBy(u => u.Username)
                    .ToListAsync();

                var result = users.Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    RoleId = u.RoleId,
                    RoleName = u.Role.Name,
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

        [HttpDelete("{memberId}")]
        public async Task<ActionResult<ApiResponse>> DeleteUser(int memberId)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user?.RoleId != 3)
                    return HandleForbidden("Только организатор может удалять участников");

                var member = await _context.Users
                    .Include(u => u.Team)
                    .FirstOrDefaultAsync(u => u.Id == memberId);

                if (member == null)
                    return HandleNotFound("Пользователь не найден");

                if (member.RoleId == 3)
                    return HandleError("Нельзя удалить организатора");

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Если пользователь - капитан
                    if (member.RoleId == (int)Roles.Captain && member.TeamId.HasValue)
                    {
                        member.Team.GitRepoName = null; // Сбрасываем GitHub репозиторий
                    }

                    _context.Users.Remove(member);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return HandleSuccess("Пользователь успешно удален");
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    return HandleError("Ошибка базы данных при удалении пользователя");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return HandleError("Ошибка при удалении пользователя");
                }
            }
            catch (InvalidOperationException ex)
            {
                return HandleUnauthorized("Пользователь не найден");
            }
            catch (Exception ex)
            {
                return HandleError("Внутренняя ошибка сервера при удалении пользователя");
            }
        }
    }
}