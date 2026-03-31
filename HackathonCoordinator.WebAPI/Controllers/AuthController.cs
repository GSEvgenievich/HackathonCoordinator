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
            try
            {
                // Проверка существования пользователя
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email || u.Login == dto.Login))
                    return HandleError("Пользователь с таким логином или email уже существует");

                var user = new User
                {
                    Username = dto.Username,
                    Login = dto.Login,
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    RoleId = (int)Roles.Member // По умолчанию выдаётся роль участника
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return HandleSuccess("Регистрация успешно завершена");
            }
            catch (DbUpdateException ex)
            {
                return HandleError("Ошибка базы данных при регистрации пользователя");
            }
            catch (ArgumentNullException ex)
            {
                return HandleError("Некорректные данные пользователя");
            }
            catch (Exception ex)
            {
                return HandleError("Внутренняя ошибка сервера при регистрации");
            }
        }

        /// <summary>
        /// Авторизация пользователя
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginDto dto)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == dto.Login);

                if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                    return HandleError<LoginResponseDto>("Неверный логин или пароль");

                var token = GenerateJwtToken(user);
                var response = new LoginResponseDto
                {
                    Token = token,
                    Username = user.Username
                };

                return HandleResult(response, "Авторизация успешна");
            }
            catch (DbUpdateException ex)
            {
                return HandleError<LoginResponseDto>("Ошибка базы данных при авторизации");
            }
            catch (ArgumentNullException ex)
            {
                return HandleError<LoginResponseDto>("Некорректные данные для входа");
            }
            catch (Exception ex)
            {
                return HandleError<LoginResponseDto>("Внутренняя ошибка сервера при авторизации");
            }
        }

        /// <summary>
        /// Валидация JWT токена
        /// </summary>
        [HttpGet("validate")]
        [Authorize]
        public ActionResult<ApiResponse> ValidateToken()
        {
            try
            {
                return HandleSuccess("Token is valid");
            }
            catch (SecurityTokenException ex)
            {
                return HandleUnauthorized("Недопустимый токен");
            }
            catch (Exception ex)
            {
                return HandleError("Ошибка при валидации токена");
            }
        }

        /// <summary>
        /// Генерация JWT токена для пользователя
        /// </summary>
        private string GenerateJwtToken(User user)
        {
            try
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
            catch (ArgumentNullException ex)
            {
                throw new Exception("Отсутствуют настройки JWT в конфигурации");
            }
            catch (FormatException ex)
            {
                throw new Exception("Некорректный формат настроек JWT");
            }
        }
    }
}