using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Helpers;
using HackathonCoordinator.WebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HackathonCoordinator.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PositionsController : BaseApiController
    {
        private readonly HackathonCoordinatorContext _context;
        private static readonly int[] ProtectedPositionIds = { 1, 2, 3 };

        public PositionsController(HackathonCoordinatorContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получить все должности
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<PositionDto>>>> GetAllPositions()
        {
            try
            {
                var positions = await _context.Positions
                    .OrderBy(p => p.Id)
                    .Select(p => new PositionDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        IsProtected = ProtectedPositionIds.Contains(p.Id)
                    })
                    .ToListAsync();

                return HandleResult(positions);
            }
            catch (Exception ex)
            {
                return HandleError<List<PositionDto>>($"Ошибка при получении должностей: {ex.Message}");
            }
        }

        /// <summary>
        /// Создать новую должность (только администратор)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<PositionDto>>> CreatePosition([FromBody] CreatePositionDto dto)
        {
            try
            {
                var currentUserId = GetUserId();
                var currentUser = await _context.Users.FindAsync(currentUserId);

                if (currentUser?.RoleId != (int)Roles.Admin)
                    return HandleForbidden<PositionDto>("Только администратор может создавать должности");

                // Проверка на существование
                var exists = await _context.Positions.AnyAsync(p => p.Name.ToLower() == dto.Name.ToLower());
                if (exists)
                    return HandleError<PositionDto>("Должность с таким названием уже существует");

                var position = new Position
                {
                    Name = dto.Name.Trim()
                };

                _context.Positions.Add(position);
                await _context.SaveChangesAsync();

                var result = new PositionDto
                {
                    Id = position.Id,
                    Name = position.Name,
                    IsProtected = false
                };

                return HandleResult(result, "Должность успешно создана");
            }
            catch (Exception ex)
            {
                return HandleError<PositionDto>($"Ошибка при создании должности: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновить должность (только администратор, защищенные нельзя редактировать)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse>> UpdatePosition(int id, [FromBody] UpdatePositionDto dto)
        {
            try
            {
                var currentUserId = GetUserId();
                var currentUser = await _context.Users.FindAsync(currentUserId);

                if (currentUser?.RoleId != (int)Roles.Admin)
                    return HandleForbidden("Только администратор может редактировать должности");

                var position = await _context.Positions.FindAsync(id);
                if (position == null)
                    return HandleNotFound("Должность не найдена");

                // Проверка на защищенную должность
                if (ProtectedPositionIds.Contains(id))
                    return HandleError("Нельзя редактировать базовые должности (Разработчик, Дизайнер, Менеджер)");

                // Проверка на дублирование названия
                var exists = await _context.Positions.AnyAsync(p => p.Name.ToLower() == dto.Name.ToLower() && p.Id != id);
                if (exists)
                    return HandleError("Должность с таким названием уже существует");

                position.Name = dto.Name.Trim();
                await _context.SaveChangesAsync();

                return HandleSuccess("Должность успешно обновлена");
            }
            catch (Exception ex)
            {
                return HandleError($"Ошибка при обновлении должности: {ex.Message}");
            }
        }

        /// <summary>
        /// Удалить должность (только администратор, защищенные нельзя удалить)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeletePosition(int id)
        {
            try
            {
                var currentUserId = GetUserId();
                var currentUser = await _context.Users.FindAsync(currentUserId);

                if (currentUser?.RoleId != (int)Roles.Admin)
                    return HandleForbidden("Только администратор может удалять должности");

                // Проверка на защищенную должность
                if (ProtectedPositionIds.Contains(id))
                    return HandleError("Нельзя удалить базовые должности (Разработчик, Дизайнер, Менеджер)");

                var position = await _context.Positions
                    .Include(p => p.Users)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (position == null)
                    return HandleNotFound("Должность не найдена");

                // Проверка, есть ли пользователи с этой должностью
                if (position.Users.Any())
                {
                    return HandleError($"Нельзя удалить должность, так как она назначена {position.Users.Count} пользователям. Сначала измените должность у этих пользователей.");
                }

                _context.Positions.Remove(position);
                await _context.SaveChangesAsync();

                return HandleSuccess("Должность успешно удалена");
            }
            catch (Exception ex)
            {
                return HandleError($"Ошибка при удалении должности: {ex.Message}");
            }
        }
    }
}
