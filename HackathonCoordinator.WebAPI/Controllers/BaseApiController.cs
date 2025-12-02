using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HackathonCoordinator.WebAPI.Controllers
{
    /// <summary>
    /// Базовый контроллер API с общими методами
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>
        /// Получение ID текущего пользователя
        /// </summary>
        protected int GetUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var userId) ? userId : 0;
        }

        // --- Методы для успешных операций ---

        /// <summary>
        /// Обработка успешного результата с данными
        /// </summary>
        protected ActionResult<ApiResponse<T>> HandleResult<T>(T data, string message = null)
        {
            return Ok(ApiResponse<T>.Ok(data, message));
        }

        /// <summary>
        /// Обработка успешного результата без данных
        /// </summary>
        protected ActionResult<ApiResponse> HandleSuccess(string message = "Операция выполнена успешно")
        {
            return Ok(ApiResponse.Ok(message));
        }

        // --- Методы для обработки ошибок ---

        /// <summary>
        /// Обработка ошибки для методов с данными
        /// </summary>
        protected ActionResult<ApiResponse<T>> HandleError<T>(string error, List<string> errors = null)
        {
            return BadRequest(ApiResponse<T>.Fail(error, errors));
        }

        /// <summary>
        /// Обработка ошибки для методов без данных
        /// </summary>
        protected ActionResult<ApiResponse> HandleError(string error, List<string> errors = null)
        {
            return BadRequest(ApiResponse.Fail(error, errors));
        }

        /// <summary>
        /// Обработка NotFound для методов с данными
        /// </summary>
        protected ActionResult<ApiResponse<T>> HandleNotFound<T>(string message = "Ресурс не найден")
        {
            return NotFound(ApiResponse<T>.Fail(message));
        }

        /// <summary>
        /// Обработка NotFound для методов без данных
        /// </summary>
        protected ActionResult<ApiResponse> HandleNotFound(string message = "Ресурс не найден")
        {
            return NotFound(ApiResponse.Fail(message));
        }

        /// <summary>
        /// Обработка Forbidden для методов с данными
        /// </summary>
        protected ActionResult<ApiResponse<T>> HandleForbidden<T>(string message = "Доступ запрещен")
        {
            return StatusCode(403, ApiResponse<T>.Fail(message));
        }

        /// <summary>
        /// Обработка Forbidden для методов без данных
        /// </summary>
        protected ActionResult<ApiResponse> HandleForbidden(string message = "Доступ запрещен")
        {
            return StatusCode(403, ApiResponse.Fail(message));
        }

        /// <summary>
        /// Обработка Unauthorized для методов с данными
        /// </summary>
        protected ActionResult<ApiResponse<T>> HandleUnauthorized<T>(string message = "Требуется авторизация")
        {
            return Unauthorized(ApiResponse<T>.Fail(message));
        }

        /// <summary>
        /// Обработка Unauthorized для методов без данных
        /// </summary>
        protected ActionResult<ApiResponse> HandleUnauthorized(string message = "Требуется авторизация")
        {
            return Unauthorized(ApiResponse.Fail(message));
        }
    }
}