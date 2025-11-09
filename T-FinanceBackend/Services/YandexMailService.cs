using System.Net;
using System.Net.Mail;
using System.Text;

namespace TFinanceBackend.Services
{
    public class YandexMailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly ILogger<YandexMailService> _logger;

        public YandexMailService(IConfiguration configuration, ILogger<YandexMailService> logger)
        {
            _logger = logger;

            // Приоритет переменным окружения
            _smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST")
                ?? configuration["Smtp:Host"]
                ?? "smtp.yandex.ru";

            _smtpPort = int.TryParse(
                Environment.GetEnvironmentVariable("SMTP_PORT") ?? configuration["Smtp:Port"],
                out var port) ? port : 587;

            _smtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME")
                ?? configuration["Smtp:Username"]
                ?? string.Empty;

            _smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD")
                ?? configuration["Smtp:Password"]
                ?? string.Empty;

            var fromEmailEnv = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL");
            var fromEmailConfig = configuration["Smtp:FromEmail"];
            _fromEmail = !string.IsNullOrWhiteSpace(fromEmailEnv)
                ? fromEmailEnv
                : (!string.IsNullOrWhiteSpace(fromEmailConfig)
                    ? fromEmailConfig
                    : _smtpUsername);

            var fromNameEnv = Environment.GetEnvironmentVariable("SMTP_FROM_NAME");
            var fromNameConfig = configuration["Smtp:FromName"];
            _fromName = !string.IsNullOrWhiteSpace(fromNameEnv)
                ? fromNameEnv
                : (!string.IsNullOrWhiteSpace(fromNameConfig)
                    ? fromNameConfig
                    : "T-Finance");
        }

        private void ValidateSmtpConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_smtpUsername))
            {
                throw new InvalidOperationException("SMTP Username не настроен. Установите SMTP_USERNAME в переменных окружения или Smtp:Username в appsettings.json");
            }

            if (string.IsNullOrWhiteSpace(_smtpPassword))
            {
                throw new InvalidOperationException("SMTP Password не настроен. Установите SMTP_PASSWORD в переменных окружения или Smtp:Password в appsettings.json");
            }

            if (string.IsNullOrWhiteSpace(_fromEmail))
            {
                throw new InvalidOperationException("SMTP FromEmail не настроен. Установите SMTP_FROM_EMAIL в переменных окружения или Smtp:FromEmail в appsettings.json");
            }
        }

        public async Task SendEmailVerificationAsync(string toEmail, string verificationLink, string username)
        {
            var subject = "Подтверждение email адреса - T-Finance-Web";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>T-Finance-Web</h1>
        </div>
        <div class=""content"">
            <h2>Здравствуйте, {username}!</h2>
            <p>Спасибо за регистрацию в T-Finance. Для завершения регистрации необходимо подтвердить ваш email адрес.</p>
            <p>Пожалуйста, нажмите на кнопку ниже для подтверждения:</p>
            <p style=""text-align: center;"">
                <a href=""{verificationLink}"" class=""button"">Подтвердить email</a>
            </p>
            <p>Или скопируйте и вставьте следующую ссылку в браузер:</p>
            <p style=""word-break: break-all; color: #0066cc;"">{verificationLink}</p>
            <p><strong>Важно:</strong> Ссылка действительна в течение 24 часов.</p>
            <p>Если вы не регистрировались в T-Finance, просто проигнорируйте это письмо.</p>
        </div>
        <div class=""footer"">
            <p>&copy; {DateTime.Now.Year} T-Finance. Все права защищены.</p>
        </div>
    </div>
</body>
</html>";

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogError("Email адрес не может быть пустым");
                throw new ArgumentException("Email адрес получателя не может быть пустым", nameof(toEmail));
            }
            await SendEmailAsync(toEmail, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // Проверяем конфигурацию SMTP перед отправкой
                ValidateSmtpConfiguration();

                // Валидация входных параметров
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    throw new ArgumentException("Email адрес получателя не может быть пустым", nameof(toEmail));
                }

                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName, Encoding.UTF8),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };
                
                message.To.Add(new MailAddress(toEmail));

                await client.SendMailAsync(message);
                _logger.LogInformation("Email успешно отправлен на {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке email на {Email}", toEmail);
                throw;
            }
        }
    }
}
