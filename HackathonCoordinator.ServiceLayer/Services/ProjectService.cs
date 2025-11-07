using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Storages;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using TaskStatus = HackathonCoordinator.ServiceLayer.DTOs.TaskStatus;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public class ProjectService
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "http://localhost:5046/api/";

        public ProjectService()
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

        public async Task<List<TaskDto>> GetProjectTasksAsync(int projectId)
        {
            SetAuthHeader();
            var response = await _client.GetAsync($"projects/{projectId}/tasks");

            if (!response.IsSuccessStatusCode)
                return new List<TaskDto>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<TaskDto>>(json) ?? new List<TaskDto>();
        }

        public async Task<ProjectDto> GetProjectAsync(int projectId)
        {
            SetAuthHeader();
            var response = await _client.GetAsync($"projects/{projectId}");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ProjectDto>(json);
        }

        public async Task<(bool Success, string Message)> CreateProjectAsync(int teamId, CreateProjectDto dto)
        {
            SetAuthHeader();

            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"teams/{teamId}/projects", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Ошибка: {body}");

            return (true, "Проект успешно создан");
        }

        public async Task<(bool Success, string Message)> UpdateProjectAsync(int projectId, UpdateProjectDto dto)
        {
            SetAuthHeader();

            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PutAsync($"projects/{projectId}", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Ошибка: {body}");

            return (true, "Проект успешно обновлен");
        }

        public async Task<(bool Success, string Message)> DeleteProjectAsync(int projectId)
        {
            SetAuthHeader();

            var response = await _client.DeleteAsync($"projects/{projectId}");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Ошибка: {body}");

            return (true, "Проект успешно удален");
        }

        public async Task<List<TaskType>> GetTaskTypesAsync()
        {
            SetAuthHeader();
            var response = await _client.GetAsync("tasks/types");

            if (!response.IsSuccessStatusCode)
                return new List<TaskType>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<TaskType>>(json) ?? new List<TaskType>();
        }

        public async Task<List<TaskStatus>> GetTaskStatusesAsync()
        {
            SetAuthHeader();
            var response = await _client.GetAsync("tasks/statuses");

            if (!response.IsSuccessStatusCode)
                return new List<TaskStatus>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<TaskStatus>>(json) ?? new List<TaskStatus>();
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

        public async Task<(bool Success, string Message)> CreateTaskAsync(int projectId, CreateTaskDto dto)
        {
            SetAuthHeader();

            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"projects/{projectId}/tasks", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Ошибка: {body}");

            return (true, "Задача успешно создана");
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

            return (true, "Задача успешно обновлена");
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