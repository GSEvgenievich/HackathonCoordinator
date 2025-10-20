using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
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

        public UsersController(HackathonCoordinatorContext context)
        {
            _context = context;
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
                .Select(u => new UserProfileDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
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
    }

}
