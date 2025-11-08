using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using TFinanceBackend.Data;
using TFinanceBackend.Models;
using TFinanceBackend.Utility;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Diagnostics;

namespace TFinanceBackend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController(TFinanceDbContext context, IConfiguration configuration) : ControllerBase
    {
        private readonly TFinanceDbContext _context = context;
        private readonly IConfiguration _configuration = configuration;

        public record RegisterRequest(string Email, string Login, string Password);
        public record LoginRequest(string LoginOrEmail, string Password);

        [HttpPost("register")]
        [EnableRateLimiting("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Валидация Email
            var emailValidation = EmailValidator.Validate(request.Email);
            if (!emailValidation.IsValid)
            {
                return BadRequest(new { message = emailValidation.ErrorMessage });
            }

            // Валидация Login
            var loginValidation = LoginValidator.Validate(request.Login);
            if (!loginValidation.IsValid)
            {
                return BadRequest(new { message = loginValidation.ErrorMessage });
            }

            // Валидация Password
            var passwordValidation = PasswordValidator.Validate(request.Password);
            if (!passwordValidation.IsValid)
            {
                return BadRequest(new { message = passwordValidation.ErrorMessage });
            }

            // Нормализация данных перед проверкой
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var normalizedLogin = request.Login.Trim();

            // Проверка на существующего пользователя
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(user => user.Email == normalizedEmail || user.Login == normalizedLogin);
            if (existingUser != null)
            {
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
            };
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Пользователь успешно зарегистрирован" });
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
            var loginOrEmail = request.LoginOrEmail.Trim().ToLowerInvariant();

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

            try
            {
                var token = GenerateJwtToken(user);

                // Используем ту же логику, что и в Program.cs: приоритет переменным окружения
                var jwtSection = _configuration.GetSection("Jwt");
                var expiresInHours = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRES_IN_HOURS"), out var hours) 
                    ? hours 
                    : jwtSection.GetValue<int>("ExpiresInHours", 1);
                var expiresAt = DateTime.UtcNow.AddHours(expiresInHours);

                // HttpOnly токен
                Response.Cookies.Append("token", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,           // dev по http => false; prod за https => true
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    Expires = expiresAt
                });

                // Доп. инфо (username) в отдельной cookie (не HttpOnly, чтобы фронт мог прочитать при необходимости)
                Response.Cookies.Append("username", user.Login ?? "", new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    Expires = expiresAt
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
            // Важно указать те же атрибуты (SameSite/Path/Secure), с которыми куки создавались
            var commonDelete = new CookieOptions
            {
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            };

            // Удаляем (через Delete) и дополнительно перезаписываем с истёкшей датой — для совместимости браузеров
            Response.Cookies.Delete("token", commonDelete);
            Response.Cookies.Delete("username", commonDelete);

            Response.Cookies.Append("token", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(-1)
            });

            Response.Cookies.Append("username", "", new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict,
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
            var token = Request.Cookies["token"];
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
    }
}
