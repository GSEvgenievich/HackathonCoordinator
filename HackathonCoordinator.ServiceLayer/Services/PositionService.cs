
using HackathonCoordinator.ServiceLayer.DTOs;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public class PositionService : BaseService
    {
        /// <summary>
        /// Получить все должности
        /// </summary>
        public async Task<ApiResponse<List<PositionDto>>> GetAllPositionsAsync()
        {
            try
            {
                var response = await _client.GetAsync("positions");
                return await HandleResponseAsync<List<PositionDto>>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<PositionDto>>.Fail($"Ошибка получения должностей: {ex.Message}");
            }
        }

        /// <summary>
        /// Создать новую должность
        /// </summary>
        public async Task<ApiResponse<PositionDto>> CreatePositionAsync(string name)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(new { Name = name });
                var response = await _client.PostAsync("positions", content);
                return await HandleResponseAsync<PositionDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<PositionDto>.Fail($"Ошибка создания должности: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновить должность
        /// </summary>
        public async Task<ApiResponse> UpdatePositionAsync(int id, string name)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(new { Name = name });
                var response = await _client.PutAsync($"positions/{id}", content);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка обновления должности: {ex.Message}");
            }
        }

        /// <summary>
        /// Удалить должность
        /// </summary>
        public async Task<ApiResponse> DeletePositionAsync(int id)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.DeleteAsync($"positions/{id}");
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка удаления должности: {ex.Message}");
            }
        }
    }
}