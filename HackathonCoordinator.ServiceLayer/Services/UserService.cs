using HackathonCoordinator.ServiceLayer.DTOs;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public class UserService : BaseService
    {
        public async Task<ApiResponse<List<IconDto>>> GetAllIconsAsync()
        {
            try
            {
                var response = await _client.GetAsync("icons");
                return await HandleResponseAsync<List<IconDto>>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<IconDto>>.Fail($"Ошибка получения иконок: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UserDto>> GetCurrentUserAsync()
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync("users/me");
                return await HandleResponseAsync<UserDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<UserDto>.Fail($"Ошибка получения пользователя: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<UserDto>>> GetAllUsersAsync()
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync("users/all");
                return await HandleResponseAsync<List<UserDto>>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<UserDto>>.Fail($"Ошибка получения пользователей: {ex.Message}");
            }
        }

        public async Task<ApiResponse> DeleteUserAsync(int userId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.DeleteAsync($"users/{userId}");
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка удаления пользователя: {ex.Message}");
            }
        }

        public async Task<ApiResponse> UpdateProfileAsync(string username, int? iconId)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(new { Username = username, IconId = iconId });
                var response = await _client.PutAsync("users/me/update", content);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка обновления профиля: {ex.Message}");
            }
        }

        public async Task<ApiResponse> LinkGitHubAccountAsync(string accessToken, string username, string avatarUrl)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(new
                {
                    GitHubAccessToken = accessToken,
                    GitHubUsername = username,
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

        public async Task<ApiResponse> UnlinkGitHubAsync()
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

        public async Task<ApiResponse<GitHubTokenResponseDto>> GetGitHubTokenAsync()
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync("users/me/github/token");
                return await HandleResponseAsync<GitHubTokenResponseDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<GitHubTokenResponseDto>.Fail($"Ошибка получения GitHub токена: {ex.Message}");
            }
        }
    }
}
