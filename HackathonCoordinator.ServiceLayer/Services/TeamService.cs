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
    }
}
