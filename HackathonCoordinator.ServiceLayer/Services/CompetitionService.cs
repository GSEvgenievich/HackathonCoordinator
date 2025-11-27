using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Storages;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public class CompetitionService
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "http://localhost:5046/api/";

        public CompetitionService()
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

        public async Task<List<CompetitionDto>> GetCompetitionsAsync()
        {
            SetAuthHeader();
            var response = await _client.GetAsync("competitions");
            if (!response.IsSuccessStatusCode) return new List<CompetitionDto>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<CompetitionDto>>(json) ?? new List<CompetitionDto>();
        }

        public async Task<CompetitionDto> GetCompetitionAsync(int id)
        {
            SetAuthHeader();
            var response = await _client.GetAsync($"competitions/{id}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<CompetitionDto>(json);
        }

        public async Task<(bool Success, string Message)> CreateCompetitionAsync(CreateCompetitionDto dto)
        {
            SetAuthHeader();
            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("competitions", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Ошибка: {body}");

            return (true, "Соревнование успешно создано");
        }

        public async Task<(bool Success, string Message)> UpdateCompetitionAsync(int id, CreateCompetitionDto dto)
        {
            SetAuthHeader();
            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PutAsync($"competitions/{id}", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Ошибка: {body}");

            return (true, "Соревнование успешно обновлено");
        }

        public async Task<(bool Success, string Message)> CreateTeamAsync(int competitionId, string teamName)
        {
            SetAuthHeader();

            try
            {
                var json = JsonConvert.SerializeObject(new { Name = teamName });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync($"competitions/{competitionId}/teams", content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, $"Ошибка: {body}");

                return (true, "Команда успешно создана");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка при создании команды: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteTeamAsync(int teamId)
        {
            SetAuthHeader();

            var response = await _client.DeleteAsync($"teams/{teamId}");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, $"Ошибка: {body}");

            return (true, "Команда успешно удалена");
        }

        public async Task<CompetitionExportDataDto> GetCompetitionExportDataAsync(int competitionId)
        {
            SetAuthHeader();
            var response = await _client.GetAsync($"export/competition-data/{competitionId}");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<CompetitionExportDataDto>(json);
        }
    }

    public class CompetitionExportDataDto
    {
        public CompetitionDto Competition { get; set; }
        public List<TeamExportDto> Teams { get; set; }
        public CompetitionStatsDto Stats { get; set; }
        public string SuggestedFileName { get; set; }
    }

    public class TeamExportDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TeamMemberDto> Members { get; set; }
        public List<TaskExportDto> Tasks { get; set; }
        public TeamStatsDto TeamStats { get; set; }
    }

    public class TeamMemberDto
    {
        public string Username { get; set; }
        public string Role { get; set; }
        public bool IsCaptain { get; set; }
    }

    public class TaskExportDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string AssignedTo { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TeamStatsDto
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int PlannedTasks { get; set; }
        public int CompletionPercentage { get; set; }
    }

    public class CompetitionStatsDto
    {
        public int TotalParticipants { get; set; }
        public int TotalTasks { get; set; }
        public int TotalCompletedTasks { get; set; }
        public int TotalCompletionPercentage { get; set; }
        public int AverageTeamProgress { get; set; }
    }
}