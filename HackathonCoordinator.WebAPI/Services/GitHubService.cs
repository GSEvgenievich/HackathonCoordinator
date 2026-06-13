using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace HackathonCoordinator.WebAPI.Services
{
    /// <summary>
    /// Сервис для работы с GitHub API
    /// </summary>
    public class GitHubService : IGitHubService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GitHubService> _logger;

        public GitHubService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<GitHubService> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // --- OAuth методы ---

        /// <summary>
        /// Получение URL для авторизации через GitHub OAuth
        /// </summary>
        public string GetAuthorizationUrl(string state)
        {
            var clientId = _configuration["GitHubOAuth:ClientId"];
            var redirectUri = _configuration["GitHubOAuth:RedirectUri"];
            var scope = "user:email,repo,public_repo,write:repo_hook";

            return $"https://github.com/login/oauth/authorize?" +
                   $"client_id={clientId}&" +
                   $"redirect_uri={HttpUtility.UrlEncode(redirectUri)}&" +
                   $"scope={scope}&state={state}";
        }

        /// <summary>
        /// Обмен кода авторизации на access token
        /// </summary>
        public async Task<GitHubAuthResult> ExchangeCodeAsync(string code)
        {
            var clientId = _configuration["GitHubOAuth:ClientId"];
            var clientSecret = _configuration["GitHubOAuth:ClientSecret"];
            var redirectUri = _configuration["GitHubOAuth:RedirectUri"];

            using var httpClient = _httpClientFactory.CreateClient("GitHub");

            var exchangeRequest = new
            {
                client_id = clientId,
                client_secret = clientSecret,
                code = code,
                redirect_uri = redirectUri
            };

            var json = JsonSerializer.Serialize(exchangeRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(
                    "https://github.com/login/oauth/access_token",
                    content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GitHub OAuth обмен не удался: {StatusCode} - {Content}",
                        response.StatusCode, responseContent);

                    return new GitHubAuthResult
                    {
                        Success = false,
                        ErrorMessage = "Не удалось обменять код на токен"
                    };
                }

                var tokenResponse = JsonSerializer.Deserialize<GitHubTokenResponse>(responseContent);

                if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
                {
                    return new GitHubAuthResult
                    {
                        Success = false,
                        ErrorMessage = "Access token не получен"
                    };
                }

                // Получение информации о пользователе
                var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken);

                return new GitHubAuthResult
                {
                    Success = true,
                    AccessToken = tokenResponse.AccessToken,
                    UserInfo = userInfo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обмене GitHub кода");
                return new GitHubAuthResult
                {
                    Success = false,
                    ErrorMessage = $"Ошибка обмена: {ex.Message}"
                };
            }
        }

        // --- Методы для работы с репозиториями ---

        /// <summary>
        /// Создание нового репозитория на GitHub
        /// </summary>
        public async Task<GitHubRepoResult> CreateRepositoryAsync(
            string accessToken,
            string repoName,
            string description,
            bool isPrivate)
        {
            using var httpClient = CreateAuthenticatedClient(accessToken);

            var repoData = new
            {
                name = repoName,
                description = description,
                @private = isPrivate,
                auto_init = true,
                has_issues = true,
                has_projects = true,
                has_wiki = true
            };

            var json = JsonSerializer.Serialize(repoData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync("user/repos", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var repoResponse = JsonSerializer.Deserialize<GitHubRepoResponse>(responseContent);

                    return new GitHubRepoResult
                    {
                        Success = true,
                        RepoUrl = repoResponse.HtmlUrl,
                        RepoName = repoResponse.Name,
                        Message = "Репозиторий успешно создан"
                    };
                }
                else
                {
                    var errorInfo = ParseGitHubRepoError(
                        responseContent,
                        response.StatusCode,
                        repoName,
                        "repository");

                    return new GitHubRepoResult
                    {
                        Success = false,
                        ErrorMessage = errorInfo
                    };
                }
            }
            catch (HttpRequestException ex) when
                (ex.Message.Contains("NameResolutionFailure") ||
                 ex.Message.Contains("ConnectFailure"))
            {
                return new GitHubRepoResult
                {
                    Success = false,
                    ErrorMessage = "Ошибка соединения с GitHub. Проверьте интернет-подключение."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания GitHub репозитория");
                return new GitHubRepoResult
                {
                    Success = false,
                    ErrorMessage = $"Неожиданная ошибка: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Добавить collaborator в репозиторий
        /// </summary>
        public async Task<GitHubCollaboratorResult> AddCollaboratorAsync(
            string accessToken,
            string owner,
            string repoName,
            string collaboratorUsername,
            string permission = "push")
        {
            using var httpClient = CreateAuthenticatedClient(accessToken);

            var data = new
            {
                permission = permission  // push, pull, admin, maintain, triage
            };

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PutAsync(
                    $"repos/{owner}/{repoName}/collaborators/{collaboratorUsername}",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    return new GitHubCollaboratorResult
                    {
                        Success = true,
                        Message = $"Пользователь {collaboratorUsername} добавлен в collaborators"
                    };
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                // Если пользователь уже collaborator
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return new GitHubCollaboratorResult
                    {
                        Success = true,
                        Message = $"Пользователь {collaboratorUsername} уже имеет доступ"
                    };
                }

                return new GitHubCollaboratorResult
                {
                    Success = false,
                    ErrorMessage = $"Ошибка добавления collaborator: {responseContent}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка добавления collaborator {Username}", collaboratorUsername);
                return new GitHubCollaboratorResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Проверка существования репозитория
        /// </summary>
        public async Task<bool> RepositoryExistsAsync(string accessToken, string owner, string repoName)
        {
            using var httpClient = CreateAuthenticatedClient(accessToken);

            try
            {
                var response = await httpClient.GetAsync($"repos/{owner}/{repoName}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // --- Методы для работы с ветками ---

        /// <summary>
        /// Создание новой ветки в репозитории
        /// </summary>
        public async Task<GitHubBranchResult> CreateBranchAsync(
            string accessToken,
            string owner,
            string repoName,
            string branchName)
        {
            using var httpClient = CreateAuthenticatedClient(accessToken);

            // Получение основной ветки
            var defaultBranch = await GetDefaultBranchAsync(accessToken, owner, repoName);
            var baseSha = await GetBaseShaAsync(accessToken, owner, repoName, defaultBranch);

            if (string.IsNullOrEmpty(baseSha))
            {
                return new GitHubBranchResult
                {
                    Success = false,
                    ErrorMessage = "Не удалось получить базовый коммит для создания ветки"
                };
            }

            var branchData = new
            {
                @ref = $"refs/heads/{branchName}",
                sha = baseSha
            };

            var json = JsonSerializer.Serialize(branchData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(
                    $"repos/{owner}/{repoName}/git/refs",
                    content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new GitHubBranchResult
                    {
                        Success = true,
                        BranchUrl = $"https://github.com/{owner}/{repoName}/tree/{branchName}"
                    };
                }
                else
                {
                    var errorInfo = ParseGitHubBranchError(
                        responseContent,
                        response.StatusCode,
                        branchName);

                    return new GitHubBranchResult
                    {
                        Success = false,
                        ErrorMessage = errorInfo.ErrorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания GitHub ветки");
                return new GitHubBranchResult
                {
                    Success = false,
                    ErrorMessage = $"Ошибка создания ветки: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Проверка существования ветки
        /// </summary>
        public async Task<bool> BranchExistsAsync(
            string accessToken,
            string owner,
            string repoName,
            string branchName)
        {
            using var httpClient = CreateAuthenticatedClient(accessToken);

            try
            {
                var response = await httpClient.GetAsync($"repos/{owner}/{repoName}/branches/{branchName}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Получение названия основной ветки репозитория
        /// </summary>
        public async Task<string> GetDefaultBranchAsync(
            string accessToken,
            string owner,
            string repoName)
        {
            using var httpClient = CreateAuthenticatedClient(accessToken);

            try
            {
                var response = await httpClient.GetAsync($"repos/{owner}/{repoName}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var repoInfo = JsonSerializer.Deserialize<GitHubRepoInfo>(content);
                    return repoInfo?.DefaultBranch ?? "main";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка получения основной ветки, используем 'main' по умолчанию");
            }

            // Fallback логика
            var possibleBranches = new[] { "main", "master", "develop" };
            foreach (var branch in possibleBranches)
            {
                try
                {
                    var response = await httpClient.GetAsync($"repos/{owner}/{repoName}/branches/{branch}");
                    if (response.IsSuccessStatusCode)
                        return branch;
                }
                catch
                {
                    continue;
                }
            }

            return "main";
        }

        /// <summary>
        /// Получение SHA коммита для указанной ветки
        /// </summary>
        public async Task<string> GetBaseShaAsync(
            string accessToken,
            string owner,
            string repoName,
            string branch)
        {
            using var httpClient = CreateAuthenticatedClient(accessToken);

            try
            {
                var response = await httpClient.GetAsync($"repos/{owner}/{repoName}/git/refs/heads/{branch}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var refInfo = JsonSerializer.Deserialize<GitHubRefInfo>(content);
                    return refInfo?.Object?.Sha;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка получения SHA для ветки {Branch}", branch);
            }

            return null;
        }

        // --- Методы валидации ---

        /// <summary>
        /// Проверка корректности названия репозитория
        /// </summary>
        public bool IsValidRepoName(string repoName)
        {
            return !string.IsNullOrWhiteSpace(repoName) &&
                   System.Text.RegularExpressions.Regex.IsMatch(repoName, @"^[a-zA-Z0-9._-]+$");
        }

        /// <summary>
        /// Проверить, является ли пользователь collaborator
        /// </summary>
        public async Task<bool> IsCollaboratorAsync(string accessToken, string owner, string repoName, string username)
        {
            using var httpClient = CreateAuthenticatedClient(accessToken);

            try
            {
                var response = await httpClient.GetAsync($"repos/{owner}/{repoName}/collaborators/{username}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Проверка корректности названия ветки
        /// </summary>
        public bool IsValidBranchName(string branchName)
        {
            return !string.IsNullOrWhiteSpace(branchName) &&
                   System.Text.RegularExpressions.Regex.IsMatch(branchName, @"^[a-zA-Z0-9._\/-]+$") &&
                   !branchName.StartsWith("/") && !branchName.EndsWith("/");
        }

        // --- Вспомогательные методы ---

        /// <summary>
        /// Создание HTTP клиента с аутентификацией
        /// </summary>
        private HttpClient CreateAuthenticatedClient(string accessToken)
        {
            var client = _httpClientFactory.CreateClient("GitHub");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
            return client;
        }

        /// <summary>
        /// Получение информации о пользователе GitHub
        /// </summary>
        public async Task<GitHubUserInfo> GetUserInfoAsync(string accessToken)
        {
            using var httpClient = CreateAuthenticatedClient(accessToken);

            try
            {
                var response = await httpClient.GetAsync("user");
                if (!response.IsSuccessStatusCode)
                    return null;

                var userJson = await response.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<GitHubUserInfo>(userJson);

                // Получение email пользователя
                var emailsResponse = await httpClient.GetAsync("user/emails");
                if (emailsResponse.IsSuccessStatusCode)
                {
                    var emailsJson = await emailsResponse.Content.ReadAsStringAsync();
                    var emails = JsonSerializer.Deserialize<List<GitHubEmail>>(emailsJson);
                    userInfo.Email = emails?.FirstOrDefault(e => e.Primary)?.Email ?? userInfo.Email;
                }

                return userInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения информации о пользователе GitHub");
                return null;
            }
        }

        // --- Методы обработки ошибок ---

        /// <summary>
        /// Парсинг ошибок при создании репозитория
        /// </summary>
        private string ParseGitHubRepoError(
            string responseContent,
            HttpStatusCode statusCode,
            string repoName,
            string v)
        {
            try
            {
                var errorResponse = JsonSerializer.Deserialize<GitHubErrorResponse>(responseContent);

                if (errorResponse?.Errors != null && errorResponse.Errors.Any())
                {
                    var firstError = errorResponse.Errors.First();

                    // Обработка конкретных ошибок GitHub
                    switch (firstError.Code?.ToLower())
                    {
                        case "custom" when firstError.Message?.ToLower().Contains("name already exists") == true:
                            return $"Репозиторий с названием '{repoName}' уже существует в вашем GitHub аккаунте. " +
                                   "Пожалуйста, выберите другое название.";

                        case "already_exists":
                            return $"Репозиторий '{repoName}' уже существует. Выберите другое название.";

                        case "invalid" when firstError.Field == "name":
                            return $"Недопустимое название репозитория '{repoName}'. " +
                                   "Название может содержать только буквы, цифры, точки, дефисы и подчеркивания.";

                        case "resource_not_found":
                            return "GitHub аккаунт не найден. Возможно, токен доступа устарел.";

                        default:
                            return AnalyzeErrorMessage(firstError.Message, repoName);
                    }
                }

                // Анализ по статус коду и сообщению
                return AnalyzeByStatusCodeAndMessage(statusCode, errorResponse?.Message, repoName);
            }
            catch
            {
                // Если не удалось распарсить JSON
                return "Произошла неизвестная ошибка при создании репозитория.";
            }
        }

        /// <summary>
        /// Анализ сообщения об ошибке
        /// </summary>
        private string AnalyzeErrorMessage(string errorMessage, string repoName)
        {
            if (string.IsNullOrEmpty(errorMessage))
                return "Неизвестная ошибка GitHub.";

            var lowerMessage = errorMessage.ToLower();

            if (lowerMessage.Contains("name already exists") || lowerMessage.Contains("already exists"))
                return $"Репозиторий '{repoName}' уже существует в вашем аккаунте GitHub.";

            if (lowerMessage.Contains("bad credentials") || lowerMessage.Contains("invalid token"))
                return "Неверные учетные данные GitHub. Пожалуйста, перепривяжите GitHub аккаунт в настройках профиля.";

            if (lowerMessage.Contains("rate limit") || lowerMessage.Contains("api rate limit exceeded"))
                return "Превышен лимит запросов к GitHub API. Пожалуйста, подождите 1 час и попробуйте снова.";

            if (lowerMessage.Contains("forbidden") || lowerMessage.Contains("insufficient permissions"))
                return "Недостаточно прав для создания репозитория. Убедитесь, что ваш GitHub аккаунт имеет разрешение на создание репозиториев.";

            if (lowerMessage.Contains("validation failed"))
                return "Ошибка валидации данных. Проверьте правильность введенной информации.";

            if (lowerMessage.Contains("not found"))
                return "Ресурс не найден. Возможно, GitHub аккаунт был удален или токен устарел.";

            return $"Ошибка GitHub: {errorMessage}";
        }

        /// <summary>
        /// Анализ ошибки по статус коду
        /// </summary>
        private string AnalyzeByStatusCodeAndMessage(
            HttpStatusCode statusCode,
            string message,
            string repoName)
        {
            switch (statusCode)
            {
                case HttpStatusCode.Unauthorized:
                    return "Ошибка авторизации GitHub. Пожалуйста, перепривяжите GitHub аккаунт.";

                case HttpStatusCode.Forbidden:
                    return "Доступ запрещен. Убедитесь, что ваш GitHub аккаунт имеет необходимые разрешения.";

                case HttpStatusCode.UnprocessableEntity: // 422
                    return $"Невозможно создать репозиторий '{repoName}'. " +
                           "Возможно, такое название уже существует или содержит недопустимые символы.";

                case HttpStatusCode.NotFound:
                    return "GitHub API не найден. Возможно, изменился API GitHub.";

                case HttpStatusCode.TooManyRequests: // 429
                    return "Слишком много запросов к GitHub API. Пожалуйста, подождите и попробуйте позже.";

                case HttpStatusCode.InternalServerError:
                    return "Внутренняя ошибка сервера GitHub. Пожалуйста, попробуйте позже.";

                case HttpStatusCode.BadGateway:
                case HttpStatusCode.ServiceUnavailable:
                case HttpStatusCode.GatewayTimeout:
                    return "GitHub временно недоступен. Пожалуйста, попробуйте позже.";

                default:
                    return string.IsNullOrEmpty(message)
                        ? $"Ошибка GitHub (код: {(int)statusCode})"
                        : $"Ошибка GitHub: {message}";
            }
        }

        /// <summary>
        /// Парсинг ошибок при создании ветки
        /// </summary>
        private (string ErrorType, string ErrorMessage) ParseGitHubBranchError(
            string responseContent,
            HttpStatusCode statusCode,
            string name)
        {
            try
            {
                var errorResponse = JsonSerializer.Deserialize<GitHubErrorResponse>(responseContent);
                var errorMessage = errorResponse?.Message ?? responseContent;
                var lowerMessage = errorMessage.ToLower();

                if (lowerMessage.Contains("already exists") || lowerMessage.Contains("уже существует"))
                    return ("name_already_exists", $"Ветка '{name}' уже существует");

                if (lowerMessage.Contains("bad credentials") || lowerMessage.Contains("invalid token"))
                    return ("auth_error", "Неверные учетные данные GitHub");

                if (lowerMessage.Contains("rate limit"))
                    return ("rate_limit", "Превышен лимит запросов к GitHub API");

                if (lowerMessage.Contains("validation failed"))
                    return ("validation_error", $"Недопустимое название ветки '{name}'");

                // Анализ по статус коду
                return statusCode switch
                {
                    HttpStatusCode.Unauthorized => ("auth_error", "Ошибка авторизации GitHub"),
                    HttpStatusCode.Forbidden => ("permission_error", "Недостаточно прав для выполнения операции"),
                    HttpStatusCode.NotFound => ("not_found", "Ресурс не найден"),
                    HttpStatusCode.UnprocessableEntity => ("validation_error", $"Невозможно создать ветку '{name}'"),
                    _ => ("unknown", $"Ошибка GitHub: {errorMessage}")
                };
            }
            catch
            {
                return ("unknown", "Неизвестная ошибка GitHub");
            }
        }
    }

    // --- Модели для десериализации JSON ---

    public class GitHubTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; }
    }

    public class GitHubRepoResponse
    {
        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class GitHubRepoInfo
    {
        [JsonPropertyName("default_branch")]
        public string DefaultBranch { get; set; }
    }

    public class GitHubRefInfo
    {
        [JsonPropertyName("object")]
        public GitHubRefObject Object { get; set; }
    }

    public class GitHubRefObject
    {
        [JsonPropertyName("sha")]
        public string Sha { get; set; }
    }

    public class GitHubErrorResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("errors")]
        public List<GitHubError> Errors { get; set; }
    }

    public class GitHubError
    {
        [JsonPropertyName("resource")]
        public string Resource { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("field")]
        public string Field { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class GitHubEmail
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("primary")]
        public bool Primary { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }
    }
}