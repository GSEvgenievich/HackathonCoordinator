using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
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
    public class AuthController : BaseApiController
    {
        private readonly HackathonCoordinatorContext _context;
        private readonly IConfiguration _config;

        public AuthController(HackathonCoordinatorContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse>> Register([FromBody] RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email || u.Login == dto.Login))
                return HandleError("Пользователь с таким логином или email уже существует");

            var user = new User
            {
                Username = dto.Username,
                Login = dto.Login,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = 2
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return HandleSuccess("Регистрация успешно завершена");
        }

        /// <summary>
        /// Авторизация пользователя
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == dto.Login);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return HandleError<LoginResponseDto>("Неверный логин или пароль");

            var token = GenerateJwtToken(user);
            var response = new LoginResponseDto { Token = token, Username = user.Username };

            return HandleResult(response, "Авторизация успешна");
        }

        /// <summary>
        /// Валидация JWT токена
        /// </summary>
        [HttpGet("validate")]
        [Authorize]
        public ActionResult<ApiResponse> ValidateToken()
        {
            return HandleSuccess("Token is valid");
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
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_config["Jwt:ExpireMinutes"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}