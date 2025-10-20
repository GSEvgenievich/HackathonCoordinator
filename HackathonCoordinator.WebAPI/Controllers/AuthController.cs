using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Helpers;
using HackathonCoordinator.WebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HackathonCoordinator.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly HackathonCoordinatorContext _context;
        private readonly IConfiguration _config;
        private readonly SmtpEmailSender _emailSender;

        public AuthController(HackathonCoordinatorContext context, IConfiguration config, SmtpEmailSender emailSender)
        {
            _context = context;
            _config = config;
            _emailSender = emailSender;
        }

        // --- Регистрация ---
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email || u.Login == dto.Login))
                return BadRequest("Пользователь с таким логином или email уже существует.");

            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM PendingRegistrations WHERE IsVerified = 0 AND ExpiresAt < GETDATE()");

            var existingPending = await _context.PendingRegistrations
                .FirstOrDefaultAsync(p => p.Email == dto.Email);

            if (existingPending != null)
                _context.PendingRegistrations.Remove(existingPending);

            var code = new Random().Next(100000, 999999).ToString();

            var pending = new PendingRegistration
            {
                Username = dto.Username,
                Login = dto.Login,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                VerificationCode = code
            };

            _context.PendingRegistrations.Add(pending);
            await _context.SaveChangesAsync();

            await _emailSender.SendEmailAsync(dto.Email, code);
            return Ok("Код подтверждения отправлен на вашу почту.");
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] VerifyEmailDto dto)
        {
            var pending = await _context.PendingRegistrations
                .FirstOrDefaultAsync(p => p.Email == dto.Email && p.IsVerified == false);

            if (pending == null)
                return BadRequest("Регистрация не найдена.");

            if (pending.ExpiresAt < DateTime.Now)
                return BadRequest("Срок действия данной регистрации истёк");

            pending.IsVerified = true;

            var user = new User
            {
                Username = pending.Username,
                Login = pending.Login,
                Email = pending.Email,
                PasswordHash = pending.PasswordHash,
                RoleId = 2
            };

            _context.Users.Add(user);
            _context.PendingRegistrations.Remove(pending);
            await _context.SaveChangesAsync();

            return Ok("Регистрация успешно завершена!");
        }

        // --- Авторизация ---
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == dto.Login);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Неверный email или пароль.");

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        [HttpGet("validate")]
        [Authorize]
        public IActionResult ValidateToken()
        {
            return Ok("Token is valid");
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.RoleId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpireMinutes"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
