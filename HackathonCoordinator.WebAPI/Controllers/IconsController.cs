using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HackathonCoordinator.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IconsController : BaseApiController
    {
        private readonly HackathonCoordinatorContext _context;

        public IconsController(HackathonCoordinatorContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получить список доступных иконок профиля
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<IconDto>>>> GetIcons()
        {
            try
            {
                var icons = await _context.ProfileIcons
                    .Select(i => new IconDto
                    {
                        Id = i.Id,
                        Name = i.Name
                    })
                    .ToListAsync();

                return HandleResult(icons);
            }
            catch (DbUpdateException ex)
            {
                return HandleError<List<IconDto>>("Ошибка базы данных при получении иконок");
            }
            catch (Exception ex)
            {
                return HandleError<List<IconDto>>("Внутренняя ошибка сервера при получении иконок");
            }
        }
    }
}