using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using TFinanceBackend.Data;
using TFinanceBackend.Models;
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
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(user => user.Email == request.Email || user.Login == request.Login);
            if (existingUser != null)
                return BadRequest(new { message = "Пользователь с таким Email или Login уже зарегестрирован." });

            var passwordHasher = new PasswordHasher<User>();
            var user = new User
            {
                Email = request.Email,
                Login = request.Login,
                PasswordHash = "",
                IsPremium = false,
            };
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Пользователь успешно зарегестрирован" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == request.LoginOrEmail || u.Email == request.LoginOrEmail);

            if (user == null)
                return Unauthorized(new { message = "Неверный логин" });

            var passwordHasher = new PasswordHasher<User>();
            var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
                return Unauthorized(new { message = "Неверный пароль" });

            try
            {
                var token = GenerateJwtToken(user);

                var jwtSection = _configuration.GetSection("Jwt");
                var expiresInHours = jwtSection.GetValue<int>("ExpiresInHours");
                var expiresAt = DateTime.UtcNow.AddHours(expiresInHours);

                // HttpOnly токен
                Response.Cookies.Append("token", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,           // dev по http => false; prod за https => true
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    Expires = expiresAt
                });

                // Доп. инфо (username) в отдельной cookie (не HttpOnly, чтобы фронт мог прочитать при необходимости)
                Response.Cookies.Append("username", user.Login ?? "", new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.None,
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
                SameSite = SameSiteMode.None,
                Path = "/"
            };

            // Удаляем (через Delete) и дополнительно перезаписываем с истёкшей датой — для совместимости браузеров
            Response.Cookies.Delete("token", commonDelete);
            Response.Cookies.Delete("username", commonDelete);

            Response.Cookies.Append("token", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(-1)
            });

            Response.Cookies.Append("username", "", new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(-1)
            });

            return Ok(new { message = "Вы вышли из аккаунта" });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var secretKey = jwtSection.GetValue<string>("Key");
            if (string.IsNullOrEmpty(secretKey))
                throw new ArgumentException("JWT-секретный ключ не задан или равен null");

            var issuer = jwtSection.GetValue<string>("Issuer");
            var audience = jwtSection.GetValue<string>("Audience");
            var expiresInHours = jwtSection.GetValue<int>("ExpiresInHours");

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
            var secretKey = jwtSection.GetValue<string>("Key");
            if (string.IsNullOrEmpty(secretKey))
                throw new ArgumentException("JWT-секретный ключ не задан или равен null");

            var issuer = jwtSection.GetValue<string>("Issuer");
            var audience = jwtSection.GetValue<string>("Audience");

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
