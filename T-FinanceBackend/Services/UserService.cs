using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TFinanceBackend.Data;
using TFinanceBackend.Models;

namespace TFinanceBackend.Services
{
    public class UserService
    {
        private readonly TFinanceDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserService> _logger;

        private readonly YandexMailService _mailService;

        public UserService(
            TFinanceDbContext context,
            IConfiguration configuration,
            ILogger<UserService> logger,
            YandexMailService mailService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _mailService = mailService;

        }

        public async Task<bool> IsUserPremiumAsync(User user)
        {
            // Проверка статуса в базе
            bool statusValid = user.IsPremium && DateTime.UtcNow >= user.PremiumCreateAt && DateTime.UtcNow <= user.PremiumExpiresAt;

            // Или, если хотите проверить платежи:
            Payment? hasValidPayment = await _context.Payments.FirstOrDefaultAsync(p => p.UserId == user.Id && p.Status == "succeeded" && p.PaidAt != null);

            // Итоговая проверка: активен ли пользователь
            return statusValid;
        }

        public async Task<string> GenerateJwtToken(User user)
        {
            var jwtSection = _configuration.GetSection("Jwt");

            // Используем ту же логику, что и в Program.cs: приоритет переменным окружения
            var secretKey = Environment.GetEnvironmentVariable("JWT_KEY")
                ?? jwtSection["Key"]
                ?? throw new ArgumentException("JWT-секретный ключ не задан или равен null");

            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
                ?? jwtSection["Issuer"]
                ?? throw new ArgumentException("JWT Issuer не настроен");

            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                ?? jwtSection["Audience"]
                ?? throw new ArgumentException("JWT Audience не настроен");

            var expiresInHours = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRES_IN_HOURS"), out var hours)
                ? hours
                : jwtSection.GetValue<int>("ExpiresInHours", 1);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var loginClaim = user.Login ?? throw new ArgumentException("Login равен null или не задан");

            bool isPremium = await IsUserPremiumAsync(user);

            var claims = new[]
            {
                new Claim(ClaimTypes.Role, isPremium ? "Premium" : "User"),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, loginClaim),
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(expiresInHours),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async void SendEmailVerifyAsync(User user)
        {
            // Генерируем токен подтверждения email
            var token = Guid.NewGuid().ToString("N");
            var verificationToken = new EmailVerificationToken
            {
                UserId = user.Id,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24), // Токен действителен 24 часа
                IsUsed = false
            };

            _context.EmailVerificationTokens.Add(verificationToken);
            var tokenSaveResult = await _context.SaveChangesAsync();
            _logger.LogInformation("Токен подтверждения создан для пользователя {UserId}, Token: {Token}, Saved: {Saved}",
                user.Id, token, tokenSaveResult);

            // Формируем URL для подтверждения
            var baseUrl = Environment.GetEnvironmentVariable("APP_BASE_URL")
                ?? _configuration["App:BaseUrl"];

            var verificationLink = $"{baseUrl}/api/auth/verify-email?token={token}";
            _logger.LogInformation("Ссылка подтверждения сформирована: {Link}", verificationLink);

            // Отправляем письмо с подтверждением
            try
            {
                await _mailService.SendEmailVerificationAsync(
                    user.Email,
                    verificationLink,
                    user.Login ?? "Пользователь"
                );
                _logger.LogInformation("Письмо с подтверждением отправлено на {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке письма с подтверждением на {Email}", user.Email);
                // Не прерываем регистрацию, но логируем ошибку
            }
        }
    }
}
