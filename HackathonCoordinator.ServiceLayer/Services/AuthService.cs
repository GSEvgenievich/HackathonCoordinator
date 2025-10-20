using HackathonCoordinator.ServiceLayer.Storages;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public class AuthService
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "http://localhost:5046/api/";

        public string Token { get; private set; }

        public AuthService()
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

        public async Task<string> LoginAsync(string login, string password)
        {
            SetAuthHeader();

            try
            {
                var json = JsonConvert.SerializeObject(new { Login = login, Password = password });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync("auth/login", content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return "Ошибка авторизации, возможно введены неверные данные";
                }

                dynamic result = JsonConvert.DeserializeObject(body);
                Token = result.token;

                SecureTokenStorage.SaveToken(Token);
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);

                return "OK";
            }
            catch (HttpRequestException)
            {
                return "Ошибка соединения с сервером";
            }
            catch (Exception ex)
            {
                return $"Неожиданная ошибка";
            }
        }

        public async Task<string> RegisterAsync(string username, string login, string email, string password)
        {
            SetAuthHeader();

            try
            {
                var json = JsonConvert.SerializeObject(new { Username = username, Login = login, Email = email, Password = password });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync("auth/register", content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return "Ошибка регистрации";
                }

                return "Ок";
            }
            catch (HttpRequestException)
            {
                return "Ошибка соединения с сервером";
            }
            catch (Exception ex)
            {
                return $"Неожиданная ошибка: {ex.Message}";
            }
        }

        public async Task<string> VerifyCodeAsync(string email, string code)
        {
            SetAuthHeader();

            try
            {
                var json = JsonConvert.SerializeObject(new { Email = email, Code = code });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync("auth/verify", content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return "Ошибка регистрации";
                }

                return "Ок";
            }
            catch (HttpRequestException)
            {
                return "Ошибка соединения с сервером";
            }
            catch (Exception ex)
            {
                return $"Неожиданная ошибка: {ex.Message}";
            }
        }

        public async Task<bool> ValidateTokenAsync()
        {
            SetAuthHeader();

            var token = SecureTokenStorage.GetToken();
            if (string.IsNullOrEmpty(token))
                return false;

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _client.GetAsync("auth/validate");
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                return false;
            }
        }

        public void Logout()
        {
            SecureTokenStorage.ClearToken();
            _client.DefaultRequestHeaders.Authorization = null;
            Token = null;
        }
    }
}
