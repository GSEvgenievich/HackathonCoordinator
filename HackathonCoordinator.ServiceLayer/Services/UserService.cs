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

        public async Task<ApiResponse<UserDto>> GetUserByIdAsync(int userId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync($"users/{userId}");
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

        /// <summary>
        /// Назначить пользователя организатором
        /// </summary>
        public async Task<ApiResponse> MakeOrganizerAsync(int userId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.PostAsync($"users/{userId}/make-organizer", null);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка назначения организатора: {ex.Message}");
            }
        }

        /// <summary>
        /// Снять права организатора
        /// </summary>
        public async Task<ApiResponse> RemoveOrganizerAsync(int userId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.PostAsync($"users/{userId}/remove-organizer", null);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка снятия прав организатора: {ex.Message}");
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

        /// <summary>
        /// Получить расширенный профиль пользователя с результатами
        /// </summary>
        public async Task<ApiResponse<UserProfileExtendedDto>> GetUserProfileExtendedAsync(int userId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync($"users/{userId}/extended");
                return await HandleResponseAsync<UserProfileExtendedDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<UserProfileExtendedDto>.Fail($"Ошибка получения профиля: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновить должность пользователя
        /// </summary>
        public async Task<ApiResponse> UpdateUserPositionAsync(int positionId)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(new { PositionId = positionId });
                var response = await _client.PutAsync("users/me/position", content);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка обновления должности: {ex.Message}");
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
