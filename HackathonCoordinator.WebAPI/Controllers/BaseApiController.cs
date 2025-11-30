// BaseApiController.cs
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HackathonCoordinator.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        protected int GetUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var userId) ? userId : 0;
        }

        // Для успешных операций с данными
        protected ActionResult<ApiResponse<T>> HandleResult<T>(T data, string message = null)
        {
            return Ok(ApiResponse<T>.Ok(data, message));
        }

        // Для успешных операций без данных
        protected ActionResult<ApiResponse> HandleSuccess(string message = "Операция выполнена успешно")
        {
            return Ok(ApiResponse.Ok(message));
        }

        // Ошибки для методов, возвращающих данные
        protected ActionResult<ApiResponse<T>> HandleError<T>(string error, List<string> errors = null)
        {
            return BadRequest(ApiResponse<T>.Fail(error, errors));
        }

        // Ошибки для методов без данных
        protected ActionResult<ApiResponse> HandleError(string error, List<string> errors = null)
        {
            return BadRequest(ApiResponse.Fail(error, errors));
        }

        // NotFound для методов с данными
        protected ActionResult<ApiResponse<T>> HandleNotFound<T>(string message = "Ресурс не найден")
        {
            return NotFound(ApiResponse<T>.Fail(message));
        }

        // NotFound для методов без данных
        protected ActionResult<ApiResponse> HandleNotFound(string message = "Ресурс не найден")
        {
            return NotFound(ApiResponse.Fail(message));
        }

        // Forbidden для методов с данными
        protected ActionResult<ApiResponse<T>> HandleForbidden<T>(string message = "Доступ запрещен")
        {
            return StatusCode(403, ApiResponse<T>.Fail(message));
        }

        // Forbidden для методов без данных
        protected ActionResult<ApiResponse> HandleForbidden(string message = "Доступ запрещен")
        {
            return StatusCode(403, ApiResponse.Fail(message));
        }

        // Unauthorized для методов с данными
        protected ActionResult<ApiResponse<T>> HandleUnauthorized<T>(string message = "Требуется авторизация")
        {
            return Unauthorized(ApiResponse<T>.Fail(message));
        }

        // Unauthorized для методов без данных
        protected ActionResult<ApiResponse> HandleUnauthorized(string message = "Требуется авторизация")
        {
            return Unauthorized(ApiResponse.Fail(message));
        }
    }
}