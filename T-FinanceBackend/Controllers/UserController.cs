using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;
using TFinanceBackend.Data;
using TFinanceBackend.Models;
using TFinanceBackend.Services;

namespace TFinanceBackend.Controllers
{
    [Authorize(Roles = "User")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly TFinanceDbContext _context;
        private readonly ILogger<UserController> _logger;
        private readonly UserService _userService;

        public UserController(
            TFinanceDbContext context,
            ILogger<UserController> logger,
            UserService userService)
        {
            _context = context;
            _logger = logger;
            _userService = userService;
        }

        public record AddBankAccountRequest(string Name);

        [HttpPost("accounts")]
        public async Task<IActionResult> AddBankAccount([FromBody] AddBankAccountRequest request)
        {
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Accounts: request is null");
                    return BadRequest(new { message = "Нет данных" });
                }

                var sessionId = Request.Cookies["user_session"];
                if (sessionId == null)
                    return Unauthorized(new { message = "Сессия не найдена" });

                var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
                if (session == null || session.ExpiresAt <= DateTime.UtcNow)
                    return Unauthorized(new { message = "Сессия недействительна" });

                var bankAccount = new BankAccount
                {
                    Name = request.Name,
                    Balance = decimal.Zero,
                    UserId = session.UserId,
                };

                await _context.BankAccounts.AddAsync(bankAccount);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Счет создан" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при добавлении счета");
                return StatusCode(500, new { message = "Произошла ошибка при создании счета. Пожалуйста, попробуйте позже." });
            }
        }

        [HttpGet("accounts")]
        public async Task<IActionResult> GetBankAccounts()
        {
            var sessionId = Request.Cookies["user_session"];
            if (sessionId == null)
                return Unauthorized(new { message = "Сессия не найдена" });

            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (session == null || session.ExpiresAt <= DateTime.UtcNow)
                return Unauthorized(new { message = "Сессия недействительна" });

            var accounts = await _context.BankAccounts
                .Where(a => a.UserId == session.UserId)
                .Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.Balance,
                }).ToListAsync();
            
            return Ok(accounts);
        }

        [HttpDelete("accounts/{id}")]
        public async Task<IActionResult> DeleteBankAccount(int id)
        {
            try
            {
                var sessionId = Request.Cookies["user_session"];
                if (sessionId == null)
                    return Unauthorized(new { message = "Сессия не найдена" });

                var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
                if (session == null || session.ExpiresAt <= DateTime.UtcNow)
                    return Unauthorized(new { message = "Сессия недействительна" });

                // Находим счет, принадлежащий текущему пользователю
                var account = await _context.BankAccounts
                    .FirstOrDefaultAsync(a => a.Id == id && a.UserId == session.UserId);

                if (account == null)
                {
                    _logger.LogWarning("[DeleteAccount] Account {id} not found for user {userId}", id, session.UserId);
                    return NotFound(new { message = "Счет не найден" });
                }

                // Удаляем счет
                _context.BankAccounts.Remove(account);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[DeleteAccount] Account {id} deleted successfully", id);
                return Ok(new { message = "Счет удален" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при удалении счета {id}", id);
                return StatusCode(500, new { message = "Произошла ошибка при удалении счета. Пожалуйста, попробуйте позже." });
            }
        }
    }
}
