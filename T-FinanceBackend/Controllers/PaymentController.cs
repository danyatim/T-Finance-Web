using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TFinanceBackend.Data;
using TFinanceBackend.Models;
using TFinanceBackend.Services;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TFinanceBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly TFinanceDbContext _context;
        private readonly YooKassaService _yooKassaService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            TFinanceDbContext context,
            YooKassaService yooKassaService,
            IConfiguration configuration,
            ILogger<PaymentController> logger)
        {
            _context = context;
            _yooKassaService = yooKassaService;
            _configuration = configuration;
            _logger = logger;
        }

        [Authorize(Roles = "User")]
        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment()
        {
            var login = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(login))
                return Unauthorized(new { message = "Не удалось определить пользователя" });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
            if (user == null)
                return BadRequest(new { message = "Пользователя не существует" });

            if (user.IsPremium)
                return BadRequest(new { message = "У вас уже есть Premium подписка" });

            // Получаем цену из конфигурации
            var premiumPrice = decimal.TryParse(
                Environment.GetEnvironmentVariable("PREMIUM_PRICE") 
                ?? _configuration["Premium:Price"], 
                out var price) 
                ? price 
                : 999.00m; // Цена по умолчанию

            try
            {
                var paymentResponse = await _yooKassaService.CreatePaymentAsync(
                    amount: premiumPrice,
                    currency: "RUB",
                    description: $"Premium подписка для пользователя {user.Login}",
                    userId: user.Id.ToString()
                );

                // Сохраняем платеж в базу данных
                var payment = new Payment
                {
                    UserId = user.Id,
                    YooKassaPaymentId = paymentResponse.Id,
                    Status = paymentResponse.Status,
                    Amount = premiumPrice,
                    Currency = "RUB",
                    Description = paymentResponse.Description,
                    CreatedAt = paymentResponse.CreatedAt ?? DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    paymentId = paymentResponse.Id,
                    confirmationUrl = paymentResponse.Confirmation?.ConfirmationUrl,
                    status = paymentResponse.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании платежа");
                return StatusCode(500, new { message = "Ошибка при создании платежа" });
            }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            // Проверка подписи от YooKassa (опционально, но рекомендуется)
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
            _logger.LogInformation("YooKassa webhook received: {Body}", requestBody);

            try
            {
                var webhookData = JsonSerializer.Deserialize<YooKassaWebhook>(requestBody);
                if (webhookData == null || webhookData.Event == null || webhookData.Object == null)
                {
                    _logger.LogWarning("Неверный формат webhook от YooKassa");
                    return BadRequest(new { message = "Неверный формат данных" });
                }

                var paymentId = webhookData.Object.Id;
                var status = webhookData.Object.Status;

                // Находим платеж в базе данных
                var payment = await _context.Payments
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.YooKassaPaymentId == paymentId);

                if (payment == null)
                {
                    _logger.LogWarning("Платеж {PaymentId} не найден в базе данных", paymentId);
                    return NotFound(new { message = "Платеж не найден" });
                }

                // Обновляем статус платежа
                payment.Status = status;
                if (status == "succeeded" && webhookData.Object.PaidAt.HasValue)
                {
                    payment.PaidAt = webhookData.Object.PaidAt.Value;
                    
                    // Выдаем Premium статус пользователю
                    if (payment.User != null && !payment.User.IsPremium)
                    {
                        payment.User.IsPremium = true;
                        _logger.LogInformation("Premium статус выдан пользователю {UserId}", payment.UserId);
                    }
                }

                _context.Payments.Update(payment);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Webhook обработан" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке webhook от YooKassa");
                return StatusCode(500, new { message = "Ошибка при обработке webhook" });
            }
        }

        [Authorize(Roles = "User")]
        [HttpGet("status/{paymentId}")]
        public async Task<IActionResult> GetPaymentStatus(string paymentId)
        {
            var login = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(login))
                return Unauthorized(new { message = "Не удалось определить пользователя" });

            var payment = await _context.Payments
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.YooKassaPaymentId == paymentId);

            if (payment == null)
                return NotFound(new { message = "Платеж не найден" });

            // Проверяем, что платеж принадлежит текущему пользователю
            if (payment.User?.Login != login)
                return Forbid();

            return Ok(new
            {
                paymentId = payment.YooKassaPaymentId,
                status = payment.Status,
                amount = payment.Amount,
                currency = payment.Currency,
                createdAt = payment.CreatedAt,
                paidAt = payment.PaidAt
            });
        }
    }

    public class YooKassaWebhook
    {
        public string? Type { get; set; }
        public string? Event { get; set; }
        public YooKassaWebhookObject? Object { get; set; }
    }

    public class YooKassaWebhookObject
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }
}

