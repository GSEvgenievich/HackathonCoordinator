using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Storages;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public class TaskService
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "http://localhost:5046/api/";

        public TaskService()
        {
            _client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            SetAuthHeader();
        }

        private void SetAuthHeader()
        {
            var token = SecureTokenStorage.GetToken();
            _client.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
                ? null
                : new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<List<TaskTypeDto>> GetTaskTypesAsync()
        {
            SetAuthHeader();
            var response = await _client.GetAsync("tasks/types");

            if (!response.IsSuccessStatusCode)
                return new List<TaskTypeDto>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<TaskTypeDto>>(json) ?? new List<TaskTypeDto>();
        }

        public async Task<List<TaskStatusDTO>> GetTaskStatusesAsync()
        {
            SetAuthHeader();
            var response = await _client.GetAsync("tasks/statuses");

            if (!response.IsSuccessStatusCode)
                return new List<TaskStatusDTO>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<TaskStatusDTO>>(json) ?? new List<TaskStatusDTO>();
        }

        public async Task<TaskDetailsDto> GetTaskDetailsAsync(int taskId)
        {
            SetAuthHeader();
            var response = await _client.GetAsync($"tasks/{taskId}/details");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TaskDetailsDto>(json);
        }

        public async Task<(bool Success, string Message)> CreateTaskAsync(int teamId, CreateTaskDto dto)
        {
            SetAuthHeader();

            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"teams/{teamId}/tasks", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Ошибка: {body}");

            try
            {
                var result = JsonConvert.DeserializeObject<dynamic>(body);
                var message = result?.message?.ToString() ?? "Задача успешно создана";
                var warning = result?.warning?.ToString();

                if (!string.IsNullOrEmpty(warning))
                {
                    message += $"\n\n⚠️ {warning}";
                }

                return (true, message);
            }
            catch
            {
                return (true, "Задача успешно создана");
            }
        }

        public async Task<(bool Success, string Message)> UpdateTaskAsync(int taskId, CreateTaskDto dto)
        {
            SetAuthHeader();

            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PutAsync($"tasks/{taskId}", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Ошибка: {body}");

            try
            {
                var result = JsonConvert.DeserializeObject<dynamic>(body);
                var message = result?.message?.ToString() ?? "Задача успешно обновлена";
                var warning = result?.warning?.ToString();

                if (!string.IsNullOrEmpty(warning))
                {
                    message += $"\n\n⚠️ {warning}";
                }

                return (true, message);
            }
            catch
            {
                return (true, "Задача успешно обновлена");
            }
        }

        public async Task<(bool Success, string Message)> AssignTaskAsync(int taskId, int userId)
        {
            SetAuthHeader();

            var json = JsonConvert.SerializeObject(new { UserId = userId });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"tasks/{taskId}/assign", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Ошибка: {body}");

            return (true, "Задача успешно назначена");
        }

        public async Task<(bool Success, string Message)> RequestCompletionAsync(int taskId)
        {
            SetAuthHeader();

            var response = await _client.PostAsync($"tasks/{taskId}/request-completion", null);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Ошибка: {body}");

            return (true, "Запрос на завершение отправлен");
        }

        public async Task<(bool Success, string Message)> RequestCancellationAsync(int taskId)
        {
            SetAuthHeader();

            var response = await _client.PostAsync($"tasks/{taskId}/request-cancellation", null);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Ошибка: {body}");

            return (true, "Запрос на отмену отправлен");
        }

        public async Task<(bool Success, string Message)> DeleteTaskAsync(int taskId)
        {
            SetAuthHeader();

            var response = await _client.DeleteAsync($"tasks/{taskId}");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Ошибка: {body}");

            return (true, "Задача успешно удалена");
        }
    }
}
