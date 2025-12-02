using HackathonCoordinator.WebAPI.DTOs;
using HackathonCoordinator.WebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HackathonCoordinator.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OAuthController : BaseApiController
    {
        private readonly IGitHubService _gitHubService;

        public OAuthController(IGitHubService gitHubService)
        {
            _gitHubService = gitHubService;
        }

        /// <summary>
        /// Получить URL для авторизации через GitHub
        /// </summary>
        [HttpGet("github-auth-url")]
        public ActionResult<ApiResponse<GitHubAuthUrlResponseDto>> GetGitHubAuthUrl([FromQuery] string state)
        {
            var authUrl = _gitHubService.GetAuthorizationUrl(state);
            return HandleResult(new GitHubAuthUrlResponseDto { AuthUrl = authUrl });
        }

        /// <summary>
        /// Обмен кода авторизации на access token
        /// </summary>
        [HttpPost("github-exchange-code")]
        public async Task<ActionResult<ApiResponse<GitHubAuthResultDto>>> ExchangeGitHubCodeAsync([FromBody] ExchangeCodeRequestDto request)
        {
            var result = await _gitHubService.ExchangeCodeAsync(request.Code);

            if (!result.Success)
                return HandleError<GitHubAuthResultDto>(result.ErrorMessage);

            return HandleResult(new GitHubAuthResultDto
            {
                AccessToken = result.AccessToken,
                UserInfo = result.UserInfo
            });
        }

        /// <summary>
        /// Страница обратного вызова от GitHub OAuth
        /// </summary>
        [HttpGet("github-callback")]
        public IActionResult GitHubCallbackPage([FromQuery] string code, [FromQuery] string state)
        {
            var html = $@"
<!DOCTYPE html>
<html lang='ru'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>GitHub Authorization - HackathonCoordinator</title>
    <style>
        :root {{
            --primary-color: #2E86AB;
            --primary-dark: #1B6B93;
            --success-color: #27AE60;
            --text-color: #2C3E50;
            --text-secondary: #5D6D7E;
            --background: #F0F9FF;
            --card-bg: #FFFFFF;
            --control-bg: #F7FBFF;
            --border-color: #D6EAF8;
            --shadow: 0 10px 30px rgba(0,0,0,0.15);
        }}

        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}

        body {{
            font-family: 'Segoe UI', Arial, sans-serif;
            background: var(--background);
            color: var(--text-color);
            line-height: 1.6;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }}

        .container {{
            background: var(--card-bg);
            border-radius: 12px;
            box-shadow: var(--shadow);
            padding: 40px;
            max-width: 500px;
            width: 100%;
            border: 1px solid var(--border-color);
            text-align: center;
        }}

        .header {{
            margin-bottom: 30px;
        }}

        .logo {{
            font-size: 24px;
            font-weight: bold;
            color: var(--primary-color);
            margin-bottom: 10px;
        }}

        .status-icon {{
            font-size: 48px;
            margin-bottom: 20px;
        }}

        .success {{
            color: var(--success-color);
        }}

        .title {{
            font-size: 24px;
            font-weight: bold;
            margin-bottom: 15px;
            color: var(--text-color);
        }}

        .subtitle {{
            color: var(--text-secondary);
            margin-bottom: 25px;
            font-size: 16px;
        }}

        .code-container {{
            background: var(--control-bg);
            border: 2px dashed var(--primary-color);
            border-radius: 8px;
            padding: 20px;
            margin: 25px 0;
            position: relative;
        }}

        .code-label {{
            font-size: 12px;
            color: var(--text-secondary);
            margin-bottom: 8px;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }}

        .code {{
            font-family: 'Consolas', 'Monaco', monospace;
            font-size: 18px;
            font-weight: bold;
            color: var(--primary-color);
            word-break: break-all;
            user-select: all;
            cursor: text;
        }}

        .instructions {{
            background: var(--control-bg);
            border-radius: 8px;
            padding: 20px;
            margin: 25px 0;
            text-align: left;
        }}

        .instructions-title {{
            font-weight: bold;
            margin-bottom: 12px;
            color: var(--text-color);
        }}

        .step {{
            display: flex;
            align-items: flex-start;
            margin-bottom: 10px;
        }}

        .step-number {{
            background: var(--primary-color);
            color: white;
            border-radius: 50%;
            width: 24px;
            height: 24px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 12px;
            font-weight: bold;
            margin-right: 12px;
            flex-shrink: 0;
        }}

        .step-text {{
            color: var(--text-secondary);
            font-size: 14px;
        }}

        .actions {{
            margin-top: 25px;
        }}

        .btn {{
            background: var(--primary-color);
            color: white;
            border: none;
            border-radius: 8px;
            padding: 12px 24px;
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
            transition: background 0.2s;
            margin: 0 5px;
        }}

        .btn:hover {{
            background: var(--primary-dark);
        }}

        .btn-secondary {{
            background: transparent;
            color: var(--primary-color);
            border: 1px solid var(--primary-color);
        }}

        .btn-secondary:hover {{
            background: var(--control-bg);
        }}

        .security-note {{
            font-size: 12px;
            color: var(--text-secondary);
            margin-top: 20px;
            font-style: italic;
        }}

        @media (max-width: 480px) {{
            .container {{
                padding: 25px;
            }}
            
            .title {{
                font-size: 20px;
            }}
            
            .code {{
                font-size: 16px;
            }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>HackathonCoordinator</div>
            <div class='status-icon success'>✅</div>
            <h1 class='title'>Авторизация успешна!</h1>
            <p class='subtitle'>GitHub аккаунт успешно подключен к приложению</p>
        </div>

        <div class='code-container'>
            <div class='code-label'>Код авторизации</div>
            <div class='code' id='authCode'>{code}</div>
        </div>

        <div class='instructions'>
            <div class='instructions-title'>Дальнейшие действия:</div>
            
            <div class='step'>
                <div class='step-number'>1</div>
                <div class='step-text'>Скопируйте код авторизации выше</div>
            </div>
            
            <div class='step'>
                <div class='step-number'>2</div>
                <div class='step-text'>Вернитесь в приложение HackathonCoordinator</div>
            </div>
            
            <div class='step'>
                <div class='step-number'>3</div>
                <div class='step-text'>Вставьте код в поле ввода на странице привязки GitHub</div>
            </div>
            
            <div class='step'>
                <div class='step-number'>4</div>
                <div class='step-text'>Нажмите кнопку 'Подтвердить код' для завершения привязки</div>
            </div>
        </div>

        <div class='actions'>
            <button class='btn' onclick='copyCode()'>📋 Скопировать код</button>
            <button class='btn btn-secondary' onclick='closeWindow()'>✕ Закрыть</button>
        </div>

        <div class='security-note'>
            🔒 Этот код действителен только один раз и будет автоматически удален после использования
        </div>
    </div>

    <script>
        function copyCode() {{
            const codeElement = document.getElementById('authCode');
            const textArea = document.createElement('textarea');
            textArea.value = codeElement.textContent;
            document.body.appendChild(textArea);
            textArea.select();
            document.execCommand('copy');
            document.body.removeChild(textArea);
            
            // Визуальное подтверждение копирования
            const btn = event.target;
            const originalText = btn.textContent;
            btn.textContent = '✅ Скопировано!';
            btn.style.background = '#27AE60';
            
            setTimeout(() => {{
                btn.textContent = originalText;
                btn.style.background = '';
            }}, 2000);
        }}

        function closeWindow() {{
            window.close();
        }}

        document.getElementById('authCode').addEventListener('click', function() {{
            const range = document.createRange();
            range.selectNodeContents(this);
            const selection = window.getSelection();
            selection.removeAllRanges();
            selection.addRange(range);
        }});

        window.addEventListener('beforeunload', function(e) {{
            if (!navigator.userActivation.hasBeenActive) {{
                return;
            }}
            e.returnValue = 'Убедитесь, что вы скопировали код авторизации перед закрытием страницы.';
        }});
    </script>
</body>
</html>";

            return Content(html, "text/html");
        }
    }
}