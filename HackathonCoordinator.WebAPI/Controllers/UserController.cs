using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        /// <summary>
        /// Обновить профиль пользователя
        /// </summary>
        [HttpPut("me/update")]
        public async Task<ActionResult<ApiResponse>> UpdateProfile([FromBody] UpdateProfileDto dto)
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

        /// <summary>
        /// Привязать GitHub аккаунт
        /// </summary>
        [HttpPost("me/github/link")]
        public async Task<ActionResult<ApiResponse>> LinkGitHubAccount([FromBody] LinkGitHubDto dto)
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

        /// <summary>
        /// Отвязать GitHub аккаунт
        /// </summary>
        [HttpPost("me/github/unlink")]
        public async Task<ActionResult<ApiResponse>> UnlinkGitHubAccount()
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

        /// <summary>
        /// Получить GitHub токен (расшифрованный)
        /// </summary>
        [HttpGet("me/github/token")]
        public async Task<ActionResult<ApiResponse<GitHubTokenResponseDto>>> GetGitHubToken()
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

        /// <summary>
        /// Получить данные всех пользователей
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetAllUsers()
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

        [HttpDelete("{memberId}")]
        public async Task<ActionResult<ApiResponse>> DeleteUser(int memberId)
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
                // Если пользователь - капитан, снимаем его с этой роли
                if (member.RoleId == 1 && member.TeamId.HasValue)
                {
                    member.Team.GitRepoName = null; // Сбрасываем GitHub репозиторий
                }

                _context.Users.Remove(member);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return HandleSuccess("Пользователь успешно удален");
            }
            catch
            {
                await transaction.RollbackAsync();
                return HandleError("Ошибка при удалении пользователя");
            }
        }
    }
}