using System.Text.Json.Serialization;

namespace HackathonCoordinator.WebAPI.Services
{
    public interface IGitHubService
    {
        // OAuth авторизация
        Task<GitHubAuthResult> ExchangeCodeAsync(string code);
        string GetAuthorizationUrl(string state);

        // Работа с репозиториями
        Task<GitHubRepoResult> CreateRepositoryAsync(string accessToken, string repoName, string description, bool isPrivate);
        Task<bool> RepositoryExistsAsync(string accessToken, string owner, string repoName);

        // Работа с ветками
        Task<GitHubBranchResult> CreateBranchAsync(string accessToken, string owner, string repoName, string branchName);
        Task<bool> BranchExistsAsync(string accessToken, string owner, string repoName, string branchName);
        Task<string> GetDefaultBranchAsync(string accessToken, string owner, string repoName);
        Task<string> GetBaseShaAsync(string accessToken, string owner, string repoName, string branch);

        // Валидация
        bool IsValidRepoName(string repoName);
        bool IsValidBranchName(string branchName);

        // Получение информации о пользователе
        Task<GitHubUserInfo> GetUserInfoAsync(string accessToken);
    }

    public class GitHubAuthResult
    {
        public bool Success { get; set; }
        public string AccessToken { get; set; }
        public GitHubUserInfo UserInfo { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class GitHubRepoResult
    {
        public bool Success { get; set; }
        public string RepoUrl { get; set; }
        public string RepoName { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class GitHubBranchResult
    {
        public bool Success { get; set; }
        public string BranchUrl { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class GitHubUserInfo
    {
        [JsonPropertyName("login")]
        public string Login { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; }
    }
}