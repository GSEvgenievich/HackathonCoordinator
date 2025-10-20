using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Storages;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public class UserService
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "http://localhost:5046/api/";

        public UserService()
        {
            _client = new HttpClient() { BaseAddress = new Uri(BaseUrl) };
            SetAuthHeader();
        }

        private void SetAuthHeader()
        {
            var token = SecureTokenStorage.GetToken();
            _client.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
                ? null
                : new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<List<IconDto>> GetAllIconsAsync()
        {
            try
            {
                var response = await _client.GetAsync("icons");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<IconDto>>(json) ?? new List<IconDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка GetAllIconsAsync: {ex.Message}");
                return new List<IconDto>();
            }
        }

        public async Task<UserProfileDto?> GetCurrentUserAsync()
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync("users/me");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Ошибка при получении пользователя: {response.Content.ReadAsStringAsync()}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UserProfileDto>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: GetCurrentUserAsync {ex.Message}");
                return null;
            }
        }

        public async Task<(bool Success, string Message)> UpdateProfileAsync(string username, int? iconId)
        {
            SetAuthHeader();

            try
            {
                var json = JsonConvert.SerializeObject(new
                {
                    Username = username,
                    IconId = iconId
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _client.PutAsync("users/me/update", content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, $"Ошибка: {body}");

                return (true, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка UpdateProfileAsync: {ex.Message}");
                return (false, "Ошибка соединения с сервером.");
            }
        }
    }
}
