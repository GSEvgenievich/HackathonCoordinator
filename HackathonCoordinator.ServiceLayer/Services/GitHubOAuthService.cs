using System.Net.Http.Json;

namespace HackathonCoordinator.WPFClient.Services
{
    public class GitHubOAuthService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://localhost:5046/api/";

        public GitHubOAuthService()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        }

        public async Task<string> GetGitHubAuthUrlAsync(string state)
        {
            var response = await _httpClient.GetFromJsonAsync<GitHubAuthUrlResponse>($"oauth/github-auth-url?state={state}");

            return response.AuthUrl;
        }

        public async Task<GitHubAuthResult> ExchangeCodeAsync(string code)
        {
            var response = await _httpClient.PostAsJsonAsync("oauth/github-exchange-code", new { Code = code });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to exchange code: {error}");
            }

            return await response.Content.ReadFromJsonAsync<GitHubAuthResult>();
        }
    }

    public class GitHubAuthUrlResponse
    {
        public string AuthUrl { get; set; }
    }

    public class GitHubAuthResult
    {
        public string AccessToken { get; set; }
        public GitHubUserInfo UserInfo { get; set; }
    }

    public class GitHubUserInfo
    {
        public string Login { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
    }
}