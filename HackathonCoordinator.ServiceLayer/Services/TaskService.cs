using HackathonCoordinator.ServiceLayer.DTOs;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public class TaskService : BaseService
    {
        public async Task<ApiResponse<List<TaskTypeDto>>> GetTaskTypesAsync()
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync("tasks/types");
                return await HandleResponseAsync<List<TaskTypeDto>>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<TaskTypeDto>>.Fail($"Ошибка получения типов задач: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<TaskStatusDto>>> GetTaskStatusesAsync()
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync("tasks/statuses");
                return await HandleResponseAsync<List<TaskStatusDto>>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<TaskStatusDto>>.Fail($"Ошибка получения статусов задач: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<int>>> GetUserTasksIdsAsync()
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync("tasks/my/ids");
                return await HandleResponseAsync<List<int>>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<int>>.Fail($"Ошибка получения задач пользователя: {ex.Message}");
            }
        }

        public async Task<ApiResponse<TaskDetailsDto>> GetTaskDetailsAsync(int taskId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync($"tasks/{taskId}/details");
                return await HandleResponseAsync<TaskDetailsDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<TaskDetailsDto>.Fail($"Ошибка получения деталей задачи: {ex.Message}");
            }
        }

        public async Task<ApiResponse> CreateTaskAsync(int teamId, CreateTaskDto dto)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(dto);
                var response = await _client.PostAsync($"teams/{teamId}/tasks", content);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка создания задачи: {ex.Message}");
            }
        }

        public async Task<ApiResponse> UpdateTaskAsync(int taskId, CreateTaskDto dto)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(dto);
                var response = await _client.PutAsync($"tasks/{taskId}", content);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка обновления задачи: {ex.Message}");
            }
        }

        public async Task<ApiResponse> AssignTaskAsync(int taskId, int userId)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(new { UserId = userId });
                var response = await _client.PostAsync($"tasks/{taskId}/assign", content);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка назначения задачи: {ex.Message}");
            }
        }

        public async Task<ApiResponse> RequestCompletionAsync(int taskId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.PostAsync($"tasks/{taskId}/request-completion", null);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка запроса завершения: {ex.Message}");
            }
        }

        public async Task<ApiResponse> RequestCancellationAsync(int taskId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.PostAsync($"tasks/{taskId}/request-cancellation", null);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка запроса отмены: {ex.Message}");
            }
        }

        public async Task<ApiResponse> ConfirmCompletionAsync(int taskId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.PostAsync($"tasks/{taskId}/confirm-completion", null);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка подтверждения завершения: {ex.Message}");
            }
        }

        public async Task<ApiResponse> RejectCompletionAsync(int taskId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.PostAsync($"tasks/{taskId}/reject-completion", null);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка отклонения завершения: {ex.Message}");
            }
        }

        public async Task<ApiResponse> CancelTaskAsync(int taskId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.PostAsync($"tasks/{taskId}/cancel", null);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка отмены задачи: {ex.Message}");
            }
        }

        public async Task<ApiResponse> DeleteTaskAsync(int taskId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.DeleteAsync($"tasks/{taskId}");
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка удаления задачи: {ex.Message}");
            }
        }
    }
}