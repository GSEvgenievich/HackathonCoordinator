using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Storages;
using System.Net.Http.Headers;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public class AuthService : BaseService
    {
        public string Token { get; private set; }

        public async Task<ApiResponse<LoginResponseDto>> LoginAsync(string login, string password)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(new { Login = login, Password = password });
                var response = await _client.PostAsync("auth/login", content);
                var apiResponse = await HandleResponseAsync<LoginResponseDto>(response);

                if (apiResponse.Success && apiResponse.Data != null)
                {
                    Token = apiResponse.Data.Token;
                    SecureTokenStorage.SaveToken(Token);
                    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                }

                return apiResponse;
            }
            catch (HttpRequestException)
            {
                return ApiResponse<LoginResponseDto>.Fail("Ошибка соединения с сервером");
            }
            catch (Exception ex)
            {
                return ApiResponse<LoginResponseDto>.Fail($"Неожиданная ошибка: {ex.Message}");
            }
        }

        public async Task<ApiResponse> RegisterAsync(string username, string login, string email, string password)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(new { Username = username, Login = login, Email = email, Password = password });
                var response = await _client.PostAsync("auth/register", content);
                return await HandleResponseAsync(response);
            }
            catch (HttpRequestException)
            {
                return ApiResponse.Fail("Ошибка соединения с сервером");
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Неожиданная ошибка: {ex.Message}");
            }
        }

        public async Task<ApiResponse> ValidateTokenAsync()
        {
            SetAuthHeader();

            var token = SecureTokenStorage.GetToken();
            if (string.IsNullOrEmpty(token))
                return ApiResponse.Fail("Токен не найден");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _client.GetAsync("auth/validate");
                return await HandleResponseAsync(response);
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse.Fail($"Ошибка соединения: {ex.Message}");
            }
        }

        public void Logout()
        {
            SecureTokenStorage.ClearToken();
            _client.DefaultRequestHeaders.Authorization = null;
            Token = null;
        }

        public async Task<ApiResponse> LinkGitHubAccountAsync(string githubUsername, string accessToken, string avatarUrl)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(new
                {
                    GitHubUsername = githubUsername,
                    GitHubAccessToken = accessToken,
                    GitHubAvatarUrl = avatarUrl
                });

                var response = await _client.PostAsync("users/me/github/link", content);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка привязки GitHub: {ex.Message}");
            }
        }

        public async Task<ApiResponse> UnlinkGitHubAccountAsync()
        {
            SetAuthHeader();

            try
            {
                var response = await _client.PostAsync("users/me/github/unlink", null);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка отвязки GitHub: {ex.Message}");
            }
        }
    }
}