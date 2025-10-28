using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Web;

namespace HackathonCoordinator.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OAuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public OAuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("github-auth-url")]
        public IActionResult GetGitHubAuthUrl([FromQuery] string state)
        {
            var clientId = _configuration["GitHubOAuth:ClientId"];
            var redirectUri = _configuration["GitHubOAuth:RedirectUri"];
            var scope = "user:email,public_repo,write:repo_hook";

            var authUrl = $"https://github.com/login/oauth/authorize?" +
                         $"client_id={clientId}&" +
                         $"redirect_uri={HttpUtility.UrlEncode(redirectUri)}&" +
                         $"scope={scope}&state={state}";

            return Ok(new { authUrl });
        }

        [HttpGet("github-callback")]
        public IActionResult GitHubCallbackPage([FromQuery] string code, [FromQuery] string state)
        {
            var html = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <title>GitHub Authorization - HackathonCoordinator</title>
        <style>
            body {{ 
                font-family: 'Segoe UI', Arial, sans-serif; 
                text-align: center; 
                padding: 50px; 
                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                color: white;
            }}
            .container {{
                background: white;
                color: #333;
                padding: 40px;
                border-radius: 10px;
                box-shadow: 0 10px 30px rgba(0,0,0,0.3);
                max-width: 500px;
                margin: 0 auto;
            }}
            .code {{ 
                font-size: 28px; 
                font-weight: bold; 
                color: #0366d6; 
                margin: 20px;
                padding: 15px;
                background: #f6f8fa;
                border: 2px dashed #0366d6;
                border-radius: 5px;
            }}
            .instruction {{ 
                margin: 20px; 
                color: #666;
                font-size: 16px;
            }}
            .success {{ color: #28a745; font-size: 24px; }}
        </style>
    </head>
    <body>
        <div class='container'>
            <div class='success'>✅ Authorization Successful!</div>
            <div class='instruction'>Copy this code and return to your application:</div>
            <div class='code'>{code}</div>
            <div class='instruction'>
                <strong>Steps:</strong><br/>
                1. Copy the code above<br/>
                2. Return to HackathonCoordinator app<br/>
                3. Paste the code in the input field<br/>
                4. Click 'Confirm Code'
            </div>
        </div>
    </body>
    </html>";

            return Content(html, "text/html");
        }

        [HttpPost("github-exchange-code")]
        public async Task<IActionResult> ExchangeGitHubCode([FromBody] ExchangeCodeRequest request)
        {
            var clientId = _configuration["GitHubOAuth:ClientId"];
            var clientSecret = _configuration["GitHubOAuth:ClientSecret"];
            var redirectUri = _configuration["GitHubOAuth:RedirectUri"];

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "HackathonCoordinator/1.0");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var exchangeRequest = new
            {
                client_id = clientId,
                client_secret = clientSecret,
                code = request.Code,
                redirect_uri = redirectUri
            };

            var json = System.Text.Json.JsonSerializer.Serialize(exchangeRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://github.com/login/oauth/access_token", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(new { error = "Failed to exchange code for token" });
            }

            // ИСПРАВЛЕНИЕ: GitHub теперь возвращает JSON, а не form-urlencoded!
            try
            {
                // Парсим JSON ответ
                var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<GitHubTokenResponse>(responseContent);

                if (string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    return BadRequest(new { error = "Access token not received" });
                }

                var accessToken = tokenResponse.AccessToken;

                // Получаем информацию о пользователе
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var userResponse = await httpClient.GetAsync("https://api.github.com/user");
                if (!userResponse.IsSuccessStatusCode)
                {
                    return BadRequest(new { error = "Failed to get user info" });
                }

                var userJson = await userResponse.Content.ReadAsStringAsync();
                var userInfo = System.Text.Json.JsonSerializer.Deserialize<GitHubUserInfo>(userJson);

                // Получаем email
                var emailsResponse = await httpClient.GetAsync("https://api.github.com/user/emails");
                if (emailsResponse.IsSuccessStatusCode)
                {
                    var emailsJson = await emailsResponse.Content.ReadAsStringAsync();
                    var emails = System.Text.Json.JsonSerializer.Deserialize<List<GitHubEmail>>(emailsJson);
                    userInfo.Email = emails?.FirstOrDefault(e => e.Primary)?.Email ?? userInfo.Email;
                }

                return Ok(new
                {
                    accessToken,
                    userInfo = new
                    {
                        userInfo.Login,
                        userInfo.Name,
                        userInfo.Email,
                        userInfo.AvatarUrl
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Failed to parse token response: {ex.Message}" });
            }
        }
    }

    public class ExchangeCodeRequest
    {
        public string Code { get; set; }
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

    public class GitHubEmail
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("primary")]
        public bool Primary { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("visibility")]
        public string Visibility { get; set; }
    }

    public class GitHubTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; }
    }
}