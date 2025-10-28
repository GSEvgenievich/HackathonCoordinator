using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace HackathonCoordinator.WebAPI.Helpers
{
    public class SmtpEmailSenderService
    {
        private readonly IConfiguration _config;

        public SmtpEmailSenderService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string code)
        {
            string emailSubject = "Ваш код подтверждения для Hackathon Coordinator 🚀";
            string emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: system-ui, -apple-system, sans-serif; line-height: 1.6; color: #1a1a1a; }}
        .container {{ max-width: 500px; margin: 0 auto; padding: 40px 20px; }}
        .logo {{ text-align: center; font-size: 24px; font-weight: bold; color: #2563eb; margin-bottom: 30px; }}
        .code-box {{ 
            background: #f1f5f9; 
            padding: 25px; 
            border-radius: 12px; 
            text-align: center;
            margin: 30px 0;
            border-left: 4px solid #2563eb;
        }}
        .code {{ 
            font-size: 36px; 
            font-weight: 800; 
            color: #dc2626;
            font-family: 'Courier New', monospace;
            letter-spacing: 6px;
        }}
        .note {{ color: #64748b; font-size: 14px; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='logo'>Hackathon Coordinator</div>
        
        <p>Для завершения регистрации используйте код:</p>
        
        <div class='code-box'>
            <div class='code'>{code}</div>
        </div>
        
        <p class='note'>Код действителен 10 минут</p>
        
        <p style='text-align: center; color: #6b7280; margin-top: 40px;'>
            С уважением,<br>Команда Hackathon Coordinator
        </p>
    </div>
</body>
</html>";

            var fromAddress = _config["Email:Address"];
            var password = _config["Email:Password"];
            var host = _config["Email:Host"];
            var port = int.Parse(_config["Email:Port"]);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Hackathon Coordinator", fromAddress));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = emailSubject;

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = emailBody;
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            try
            {
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(fromAddress, password);
                await client.SendAsync(message);
            }
            finally
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true);
                }
            }
        }
    }
}
