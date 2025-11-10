using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using TFinanceBackend.Data;
using TFinanceBackend.Models;
using TFinanceBackend.Utility;
using TFinanceBackend.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using System.Linq;

namespace TFinanceBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly TFinanceDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly YandexMailService _mailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            TFinanceDbContext context, 
            IConfiguration configuration,
            IWebHostEnvironment environment,
            YandexMailService mailService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _environment = environment;
            _mailService = mailService;
            _logger = logger;
        }

        public record RegisterRequest(string Email, string Login, string Password);
        public record LoginRequest(string LoginOrEmail, string Password);

        [HttpPost("register")]
        [EnableRateLimiting("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Проверка на null request
                if (request == null)
                {
                    _logger.LogWarning("Register: request is null");
                    return BadRequest(new { message = "Данные регистрации не указаны" });
                }

                _logger.LogInformation("Register: начало регистрации для Email: {Email}, Login: {Login}", 
                    request.Email ?? "null", request.Login ?? "null");

                // Валидация Email
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { message = "Email обязателен для заполнения" });
                }

                var emailValidation = EmailValidator.Validate(request.Email);
                if (!emailValidation.IsValid)
                {
                    return BadRequest(new { message = emailValidation.ErrorMessage });
                }

                // Валидация Login
                if (string.IsNullOrWhiteSpace(request.Login))
                {
                    return BadRequest(new { message = "Логин обязателен для заполнения" });
                }

                var loginValidation = LoginValidator.Validate(request.Login);
                if (!loginValidation.IsValid)
                {
                    return BadRequest(new { message = loginValidation.ErrorMessage });
                }

                // Валидация Password
                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { message = "Пароль обязателен для заполнения" });
                }

                var passwordValidation = PasswordValidator.Validate(request.Password);
                if (!passwordValidation.IsValid)
                {
                    return BadRequest(new { message = passwordValidation.ErrorMessage });
                }

                // Нормализация данных перед проверкой
                var normalizedEmail = request.Email.Trim();
                var normalizedLogin = request.Login.Trim();

                // Проверка на существующего пользователя
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(user => user.Email == normalizedEmail || user.Login == normalizedLogin);
                if (existingUser != null)
                {
                    _logger.LogWarning("Register: пользователь уже существует - Email: {Email}, Login: {Login}", 
                        normalizedEmail, normalizedLogin);
                    // Не раскрываем, какой именно параметр дублируется (безопасность)
                    return BadRequest(new { message = "Пользователь с таким Email или Login уже зарегистрирован." });
                }

                var passwordHasher = new PasswordHasher<User>();
                var user = new User
                {
                    Email = normalizedEmail,
                    Login = normalizedLogin,
                    PasswordHash = "",
                    IsPremium = false,
                    IsEmailConfirmed = false
                };
                user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Register: пользователь создан с Id: {UserId}", user.Id);

                // Отправляем письмо с подтверждением
                SendEmailVerifyAsync(user);

                return Ok(new { 
                    message = "Пользователь успешно зарегистрирован. Пожалуйста, проверьте вашу почту для подтверждения email адреса." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при регистрации пользователя");
                return StatusCode(500, new { message = "Произошла ошибка при регистрации. Пожалуйста, попробуйте позже." });
            }
        }

        private async void SendEmailVerifyAsync(User user)
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
                ?? _configuration["App:BaseUrl"]
                ?? (Request.IsHttps ? $"https://{Request.Host}" : $"http://{Request.Host}");

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

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Базовая валидация входных данных
            if (string.IsNullOrWhiteSpace(request.LoginOrEmail))
            {
                return BadRequest(new { message = "Логин или Email обязателен для заполнения" });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Пароль обязателен для заполнения" });
            }

            // Нормализация входных данных
            var loginOrEmail = request.LoginOrEmail.Trim();

            // Сначала пробуем точный поиск (для новых записей с нормализованными данными)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == loginOrEmail || u.Email == loginOrEmail);

            // Унифицированное сообщение об ошибке для безопасности (не раскрываем, что именно неверно)
            if (user == null)
            {
                // Имитация проверки пароля для защиты от timing attacks
                // Используем валидный BCrypt хеш для dummy пользователя
                var dummyHasher = new PasswordHasher<User>();
                var dummyUser = new User 
                { 
                    Login = "dummy",
                    Email = "dummy@example.com",
                    // Валидный BCrypt хеш (хеш от пароля "dummy")
                    PasswordHash = "$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy" 
                };
                try
                {
                    dummyHasher.VerifyHashedPassword(dummyUser, dummyUser.PasswordHash, request.Password);
                }
                catch
                {
                    // Игнорируем ошибки при проверке dummy хеша - это нормально
                }
                
                return Unauthorized(new { message = "Неверный логин или пароль" });
            }

            var passwordHasher = new PasswordHasher<User>();
            var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Неверный логин или пароль" });
            }

            // Проверяем, подтвержден ли email
            if (!user.IsEmailConfirmed)
            {
                _logger.LogError("Email адрес: {Email}. Статус: не подтвержден.", user.Email);

                return Unauthorized(new { 
                    message = "Email адрес не подтвержден. Пожалуйста, проверьте вашу почту и перейдите по ссылке для подтверждения." 
                });
            }

            try
            {
                var token = GenerateJwtToken(user);

                // Используем ту же логику, что и в Program.cs: приоритет переменным окружения
                var jwtSection = _configuration.GetSection("Jwt");
                var expiresInHours = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRES_IN_HOURS"), out var hours) 
                    ? hours 
                    : jwtSection.GetValue<int>("ExpiresInHours", 1);
                var expiresAt = DateTime.UtcNow.AddHours(expiresInHours);

                // Определяем Secure на основе схемы запроса
                // Для HTTPS всегда используем Secure=true, для HTTP - false
                var isHttps = Request.IsHttps || 
                    (Request.Headers.ContainsKey("X-Forwarded-Proto") && 
                     Request.Headers["X-Forwarded-Proto"].ToString().Equals("https", StringComparison.OrdinalIgnoreCase));
                
                // В Development на HTTP используем Secure=false, на HTTPS - true
                // В Production всегда Secure=true
                var isSecure = isHttps || !_environment.IsDevelopment();

                // Для локальной разработки с разными портами используем None (требует Secure=true)
                // Для продакшена - Strict
                var sameSite = _environment.IsDevelopment() 
                    ? (isSecure ? SameSiteMode.None : SameSiteMode.Lax)
                    : SameSiteMode.Strict;
                
                Console.WriteLine($"[Login] HTTPS: {isHttps}, Secure: {isSecure}, SameSite: {sameSite}, Environment: {_environment.EnvironmentName}");
                Console.WriteLine($"[Login] Origin: {Request.Headers["Origin"]}, Referer: {Request.Headers["Referer"]}");

                // HttpOnly токен
                Response.Cookies.Append("token", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = isSecure,
                    SameSite = sameSite,
                    Path = "/",
                    Expires = expiresAt,
                    Domain = null // Не устанавливаем домен, чтобы работало на localhost
                });

                // Доп. инфо (username) в отдельной cookie (не HttpOnly, чтобы фронт мог прочитать при необходимости)
                Response.Cookies.Append("username", user.Login ?? "", new CookieOptions
                {
                    HttpOnly = false,
                    Secure = isSecure,
                    SameSite = sameSite,
                    Path = "/",
                    Expires = expiresAt,
                    Domain = null // Не устанавливаем домен, чтобы работало на localhost
                });

                return Ok(new { message = "Успех", username = user.Login });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Определяем Secure на основе схемы запроса
            var isHttps = Request.IsHttps || 
                (Request.Headers.ContainsKey("X-Forwarded-Proto") && 
                 Request.Headers["X-Forwarded-Proto"].ToString().Equals("https", StringComparison.OrdinalIgnoreCase));
            var isSecure = isHttps || !_environment.IsDevelopment();
            var sameSite = _environment.IsDevelopment() 
                ? (isSecure ? SameSiteMode.None : SameSiteMode.Lax)
                : SameSiteMode.Strict;

            // Важно указать те же атрибуты (SameSite/Path/Secure), с которыми куки создавались
            var commonDelete = new CookieOptions
            {
                Secure = isSecure,
                SameSite = sameSite,
                Path = "/"
            };

            // Удаляем (через Delete) и дополнительно перезаписываем с истёкшей датой — для совместимости браузеров
            Response.Cookies.Delete("token", commonDelete);
            Response.Cookies.Delete("username", commonDelete);

            Response.Cookies.Append("token", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = isSecure,
                SameSite = sameSite,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(-1)
            });

            Response.Cookies.Append("username", "", new CookieOptions
            {
                HttpOnly = false,
                Secure = isSecure,
                SameSite = sameSite,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(-1)
            });

            return Ok(new { message = "Вы вышли из аккаунта" });
        }

        private string GenerateJwtToken(User user)
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

            var claims = new[]
            {
                new Claim(ClaimTypes.Role, user.IsPremium ? "Premium" : "User"),
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

        // Валидация токена из cookie (HttpOnly)
        [HttpGet("validate")]
        public IActionResult ValidateToken()
        {
            // Логируем все cookies для отладки
            var allCookies = string.Join(", ", Request.Cookies.Select(c => $"{c.Key}={c.Value}"));

            _logger.LogInformation("[ValidateToken] Все cookies: {allCookies}", allCookies);
            Console.WriteLine($"[ValidateToken] Все cookies: {allCookies}");
            
            var token = Request.Cookies["token"];
            _logger.LogInformation("[ValidateToken] Токен из cookie: {isToken}", (string.IsNullOrWhiteSpace(token) ? "ОТСУТСТВУЕТ" : "НАЙДЕН"));
            Console.WriteLine($"[ValidateToken] Токен из cookie: {(string.IsNullOrWhiteSpace(token) ? "ОТСУТСТВУЕТ" : "НАЙДЕН")}");
            
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { message = "Токен отсутствует" });
            }

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

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken _);
                return Ok(new { message = "Токен валиден" });
            }
            catch
            {
                return Unauthorized(new { message = "Токен недействителен" });
            }
        }

        // Подтверждение email адреса
        [HttpGet("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                ?? _configuration["App:FrontendUrl"]
                ?? "https://t-finance-web.ru";

            _logger.LogInformation("VerifyEmail вызван с токеном: {Token}", token ?? "null");

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Токен не указан при подтверждении email");
                return Redirect($"{frontendUrl}/login?error=token_missing");
            }

            try
            {
                var verificationToken = await _context.EmailVerificationTokens
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed);

                _logger.LogInformation("Токен найден: {Found}, IsUsed: {IsUsed}", 
                    verificationToken != null, 
                    verificationToken?.IsUsed ?? false);

                if (verificationToken == null)
                {
                    _logger.LogWarning("Токен не найден или уже использован: {Token}", token);
                    return Redirect($"{frontendUrl}/login?error=token_invalid");
                }

                if (verificationToken.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning("Токен истек: {Token}, ExpiresAt: {ExpiresAt}, Now: {Now}", 
                        token, verificationToken.ExpiresAt, DateTime.UtcNow);

                    SendEmailVerifyAsync(verificationToken.User);
                    return Redirect($"{frontendUrl}/login?error=token_expired");
                }

                if (verificationToken.User == null)
                {
                    _logger.LogError("Пользователь не найден для токена: {Token}, UserId: {UserId}", 
                        token, verificationToken.UserId);
                    return Redirect($"{frontendUrl}/login?error=user_not_found");
                }

                // Подтверждаем email
                var oldIsEmailConfirmed = verificationToken.User.IsEmailConfirmed;
                verificationToken.User.IsEmailConfirmed = true;
                verificationToken.IsUsed = true;

                _context.Users.Update(verificationToken.User);
                _context.EmailVerificationTokens.Update(verificationToken);
                
                var savedChanges = await _context.SaveChangesAsync();
                _logger.LogInformation("Изменения сохранены: {SavedChanges}, Email был подтвержден: {WasConfirmed}, теперь: {NowConfirmed}", 
                    savedChanges, oldIsEmailConfirmed, verificationToken.User.IsEmailConfirmed);

                _logger.LogInformation("Email подтвержден для пользователя {UserId}, Email: {Email}", 
                    verificationToken.UserId, verificationToken.User.Email);

                // Редирект на страницу логина с параметром успешного подтверждения
                _logger.LogInformation("Выполняется редирект на: {Url}", $"{frontendUrl}/login?verified=true");
                return Redirect($"{frontendUrl}/login?verified=true");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при подтверждении email с токеном: {Token}", token);
                return Redirect($"{frontendUrl}/login?error=server_error");
            }
        }
    }
}
