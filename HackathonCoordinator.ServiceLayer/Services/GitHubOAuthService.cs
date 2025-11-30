using HackathonCoordinator.ServiceLayer.DTOs;
using System.Net.Http.Json;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public class GitHubOAuthService : BaseService
    {
        public async Task<ApiResponse<GitHubAuthUrlResponseDto>> GetGitHubAuthUrlAsync(string state)
        {
            try
            {
                var response = await _client.GetAsync($"oauth/github-auth-url?state={state}");
                return await HandleResponseAsync<GitHubAuthUrlResponseDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<GitHubAuthUrlResponseDto>.Fail($"Ошибка получения URL авторизации: {ex.Message}");
            }
        }

        public async Task<ApiResponse<GitHubAuthResultDto>> ExchangeCodeAsync(string code)
        {
            try
            {
                var content = CreateJsonContent(new { Code = code });
                var response = await _client.PostAsync("oauth/github-exchange-code", content);
                return await HandleResponseAsync<GitHubAuthResultDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<GitHubAuthResultDto>.Fail($"Ошибка обмена кода: {ex.Message}");
            }
        }
    }
}