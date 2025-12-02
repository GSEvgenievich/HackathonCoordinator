using System.Text.Json.Serialization;

namespace HackathonCoordinator.WebAPI.Services
{
    /// <summary>
    /// Интерфейс для работы с GitHub API
    /// </summary>
    public interface IGitHubService
    {
        // --- OAuth авторизация ---

        /// <summary>
        /// Обмен кода авторизации на access token
        /// </summary>
        Task<GitHubAuthResult> ExchangeCodeAsync(string code);

        /// <summary>
        /// Получение URL для авторизации через GitHub
        /// </summary>
        string GetAuthorizationUrl(string state);

        // --- Работа с репозиториями ---

        /// <summary>
        /// Создание нового репозитория на GitHub
        /// </summary>
        Task<GitHubRepoResult> CreateRepositoryAsync(
            string accessToken,
            string repoName,
            string description,
            bool isPrivate);

        /// <summary>
        /// Проверка существования репозитория
        /// </summary>
        Task<bool> RepositoryExistsAsync(
            string accessToken,
            string owner,
            string repoName);

        // --- Работа с ветками ---

        /// <summary>
        /// Создание новой ветки в репозитории
        /// </summary>
        Task<GitHubBranchResult> CreateBranchAsync(
            string accessToken,
            string owner,
            string repoName,
            string branchName);

        /// <summary>
        /// Проверка существования ветки
        /// </summary>
        Task<bool> BranchExistsAsync(
            string accessToken,
            string owner,
            string repoName,
            string branchName);

        /// <summary>
        /// Получение названия основной ветки репозитория
        /// </summary>
        Task<string> GetDefaultBranchAsync(
            string accessToken,
            string owner,
            string repoName);

        /// <summary>
        /// Получение SHA коммита для указанной ветки
        /// </summary>
        Task<string> GetBaseShaAsync(
            string accessToken,
            string owner,
            string repoName,
            string branch);

        // --- Валидация ---

        /// <summary>
        /// Проверка корректности названия репозитория
        /// </summary>
        bool IsValidRepoName(string repoName);

        /// <summary>
        /// Проверка корректности названия ветки
        /// </summary>
        bool IsValidBranchName(string branchName);

        // --- Получение информации о пользователе ---

        /// <summary>
        /// Получение информации о пользователе GitHub
        /// </summary>
        Task<GitHubUserInfo> GetUserInfoAsync(string accessToken);
    }

    /// <summary>
    /// Результат авторизации через GitHub OAuth
    /// </summary>
    public class GitHubAuthResult
    {
        public bool Success { get; set; }
        public string AccessToken { get; set; }
        public GitHubUserInfo UserInfo { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Результат создания репозитория на GitHub
    /// </summary>
    public class GitHubRepoResult
    {
        public bool Success { get; set; }
        public string RepoUrl { get; set; }
        public string RepoName { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Результат создания ветки на GitHub
    /// </summary>
    public class GitHubBranchResult
    {
        public bool Success { get; set; }
        public string BranchUrl { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Информация о пользователе GitHub
    /// </summary>
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