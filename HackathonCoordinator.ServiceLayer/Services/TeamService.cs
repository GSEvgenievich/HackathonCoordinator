using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Storages;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public class TeamService
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "http://localhost:5046/api/";

        public TeamService()
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

        public async Task<(bool Success, string Message)> CreateTeamAsync(string name, bool linkToGitHub = false)
        {
            SetAuthHeader();

            var json = JsonConvert.SerializeObject(new
            {
                Name = name,
                LinkToGitHub = linkToGitHub
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("teams/create", content);

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Ошибка: {body}");

            try
            {
                dynamic data = JsonConvert.DeserializeObject(body)!;
                return (true, data.message.ToString());
            }
            catch
            {
                return (true, "Команда успешно создана");
            }
        }

        public async Task<(bool Success, string Message)> JoinTeamAsync(string inviteCode)
        {
            SetAuthHeader();

            var json = JsonConvert.SerializeObject(new { InviteCode = inviteCode });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("teams/join", content);

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Ошибка: {body}");

            try
            {
                dynamic data = JsonConvert.DeserializeObject(body)!;
                return (true, data.message.ToString());
            }
            catch
            {
                return (true, "У вас новая команда!");
            }
        }

        public async Task<(bool Success, string Message)> AssignCaptainAsync(int teamId, int userId)
        {
            SetAuthHeader();

            var json = JsonConvert.SerializeObject(new { UserId = userId });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync($"teams/{teamId}/assign-captain", content);

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Ошибка: {body}");

            return (true, "Капитан успешно назначен");
        }

        public async Task<TeamDto?> GetCurrentTeamAsync()
        {
            SetAuthHeader();

            var response = await _client.GetAsync("teams/current");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<TeamDto>();
        }

        public async Task<int?> GetCurrentTeamIdAsync()
        {
            SetAuthHeader();

            var response = await _client.GetAsync("teams/current/id");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<int?>();
        }

        public async Task<(bool Success, string Message)> LeaveTeamAsync()
        {
            SetAuthHeader();

            var response = await _client.PostAsync("teams/leave", null);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Ошибка: {body}");

            dynamic data = JsonConvert.DeserializeObject(body)!;
            return (true, data.message?.ToString() ?? "Вы покинули команду");
        }

        public async Task<TeamDto?> GetTeamByIdAsync(int? teamId)
        {
            SetAuthHeader();

            var response = await _client.GetAsync($"teams/{teamId}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<TeamDto>();
        }

        public async Task<List<TaskDto>> GetTeamTasksAsync(int teamId)
        {
            SetAuthHeader();
            var response = await _client.GetAsync($"teams/{teamId}/tasks");

            if (!response.IsSuccessStatusCode)
                return new List<TaskDto>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<TaskDto>>(json) ?? new List<TaskDto>();
        }

        public async Task<CompetitionDto> GetCompetitionByTeamIdAsync(int teamId)
        {
            SetAuthHeader();
            var response = await _client.GetAsync($"teams/{teamId}/competition");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<CompetitionDto>();
        }

        public async Task<(bool Success, string Message, string RepoUrl, string ErrorType)> CreateGitHubRepositoryAsync(
            int teamId, string repoName, string description, bool isPrivate = true)
        {
            SetAuthHeader();

            var json = JsonConvert.SerializeObject(new
            {
                RepoName = repoName,
                Description = description,
                IsPrivate = isPrivate
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _client.PostAsync($"teams/{teamId}/create-github-repo", content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string errorType = "unknown";
                    string errorMessage = "unknown";

                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<GitHubErrorResponse>(body);
                        errorType = "unknown";
                        errorMessage = errorResponse?.Error ?? body;

                        if (errorMessage.Contains("уже существует") || errorMessage.Contains("already exists"))
                            errorType = "name_already_exists";
                        else if (errorMessage.Contains("авторизации") || errorMessage.Contains("Bad credentials"))
                            errorType = "auth_error";
                        else if (errorMessage.Contains("соединения") || errorMessage.Contains("connection"))
                            errorType = "network_error";
                        else if (errorMessage.Contains("лимит") || errorMessage.Contains("rate limit"))
                            errorType = "rate_limit";
                    }
                    catch
                    {
                        // Если не удалось распарсить, оставляем unknown
                    }

                    return (false, errorMessage, null, errorType);
                }

                var result = JsonConvert.DeserializeObject<GitHubRepoCreationResult>(body);
                return (true, result?.Message ?? "Репозиторий успешно создан", result?.RepoUrl, null);
            }
            catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("NameResolutionFailure") || ex.Message.Contains("ConnectFailure"))
            {
                return (false, "Ошибка соединения с GitHub. Проверьте интернет-подключение.", null, "network_error");
            }
            catch (TaskCanceledException ex)
            {
                return (false, "Превышено время ожидания ответа от GitHub.", null, "timeout");
            }
            catch (Exception ex)
            {
                return (false, $"Неожиданная ошибка: {ex.Message}", null, "unknown");
            }
        }

        public class GitHubRepoCreationResult
        {
            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("repoUrl")]
            public string RepoUrl { get; set; }

            [JsonProperty("repoName")]
            public string RepoName { get; set; }
        }

        public class GitHubErrorResponse
        {
            [JsonProperty("error")]
            public string Error { get; set; }
        }
    }
}
