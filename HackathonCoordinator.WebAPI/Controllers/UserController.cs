using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HackathonCoordinator.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly HackathonCoordinatorContext _context;
        private readonly IEncryptionService _encryptionService;

        public UsersController(HackathonCoordinatorContext context, IEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        // Получить текущего пользователя
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized("Пользователь не авторизован");

            var user = await _context.Users
                .Where(u => u.Id == int.Parse(userId))
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
                return NotFound("Пользователь не найден");

            return Ok(user);
        }

        // Обновить имя или иконку
        [HttpPut("me/update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized("Пользователь не авторизован");

            var user = await _context.Users.FindAsync(int.Parse(userId));

            if (user == null)
                return NotFound("Пользователь не найден");

            if (!string.IsNullOrWhiteSpace(dto.Username))
                user.Username = dto.Username;

            if (dto.IconId.HasValue)
            {
                var iconExists = await _context.ProfileIcons.AnyAsync(i => i.Id == dto.IconId.Value);

                if (!iconExists)
                    return BadRequest("Иконка не найдена");

                user.ProfileIconId = dto.IconId.Value;
            }

            await _context.SaveChangesAsync();
            return Ok("Профиль обновлён");
        }

        [HttpPost("me/github/link")]
        public async Task<IActionResult> LinkGitHubAccount([FromBody] LinkGitHubDto dto)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("Пользователь не найден");

            // ШИФРУЕМ токен перед сохранением
            user.GitHubUsername = dto.GitHubUsername;
            user.GitHubAccessToken = _encryptionService.Encrypt(dto.GitHubAccessToken);
            user.GitHubAvatarUrl = dto.GitHubAvatarUrl;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "GitHub аккаунт успешно привязан" });
        }

        [HttpPost("me/github/unlink")]
        public async Task<IActionResult> UnlinkGitHubAccount()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("Пользователь не найден");

            // Очищаем GitHub данные
            user.GitHubUsername = null;
            user.GitHubAccessToken = null;
            user.GitHubAvatarUrl = null;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "GitHub аккаунт отвязан" });
        }

        [HttpGet("me/github/info")]
        public async Task<ActionResult<GitHubUserInfoDto>> GetGitHubInfo()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new GitHubUserInfoDto
                {
                    GitHubUsername = u.GitHubUsername,
                    GitHubAvatarUrl = u.GitHubAvatarUrl
                })
                .FirstOrDefaultAsync();

            return Ok(user);
        }

        // Вспомогательный метод для получения зашифрованного токена (для использования в других сервисах)
        [HttpGet("me/github/token")]
        public async Task<IActionResult> GetGitHubToken()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.GitHubAccessToken })
                .FirstOrDefaultAsync();

            if (user == null || string.IsNullOrEmpty(user.GitHubAccessToken))
                return NotFound("GitHub токен не найден");

            // Расшифровываем токен только когда он нужен
            var decryptedToken = _encryptionService.Decrypt(user.GitHubAccessToken);

            return Ok(new { accessToken = decryptedToken });
        }

        private int GetUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var userId) ? userId : 0;
        }
    }

}
