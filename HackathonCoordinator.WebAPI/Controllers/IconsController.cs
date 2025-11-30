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
            var icons = await _context.ProfileIcons
                .Select(i => new IconDto
                {
                    Id = i.Id,
                    Name = i.Name
                })
                .ToListAsync();

            return HandleResult(icons);
        }
    }
}