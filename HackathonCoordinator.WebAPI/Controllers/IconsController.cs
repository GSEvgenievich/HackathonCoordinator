using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HackathonCoordinator.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IconsController : ControllerBase
    {
        private readonly HackathonCoordinatorContext _context;

        public IconsController(HackathonCoordinatorContext context)
        {
            _context = context;
        }

        // GET: api/icons
        [HttpGet]
        public async Task<ActionResult<IEnumerable<IconDto>>> GetIcons()
        {
            var icons = await _context.ProfileIcons
                .Select(i => new IconDto
                {
                    Id = i.Id,
                    Name = i.Name
                })
                .ToListAsync();

            return Ok(icons);
        }
    }
}
