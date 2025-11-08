using System.Text;
using System.Text.Json;

namespace TFinanceBackend.Services
{
    public class YooKassaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _shopId;
        private readonly string _secretKey;
        private readonly string _returnUrl;
        private readonly ILogger<YooKassaService> _logger;

        private const string YooKassaApiUrl = "https://api.yookassa.ru/v3";

        public YooKassaService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<YooKassaService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Приоритет переменным окружения
            _shopId = Environment.GetEnvironmentVariable("YOOKASSA_SHOP_ID")
                ?? configuration["YooKassa:ShopId"]
                ?? throw new InvalidOperationException("YooKassa ShopId не настроен");

            _secretKey = Environment.GetEnvironmentVariable("YOOKASSA_SECRET_KEY")
                ?? configuration["YooKassa:SecretKey"]
                ?? throw new InvalidOperationException("YooKassa SecretKey не настроен");

            _returnUrl = Environment.GetEnvironmentVariable("YOOKASSA_RETURN_URL")
                ?? configuration["YooKassa:ReturnUrl"]
                ?? throw new InvalidOperationException("YooKassa ReturnUrl не настроен");

            // Настройка базовой аутентификации для YooKassa
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_shopId}:{_secretKey}"));
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
        }

        public async Task<YooKassaPaymentResponse> CreatePaymentAsync(
            decimal amount,
            string currency,
            string description,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var paymentRequest = new
            {
                amount = new
                {
                    value = amount.ToString("F2"),
                    currency = currency
                },
                confirmation = new
                {
                    type = "redirect",
                    return_url = _returnUrl
                },
                capture = true,
                description = description,
                metadata = new
                {
                    user_id = userId
                }
            };

            var json = JsonSerializer.Serialize(paymentRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Создаем новый запрос с уникальным Idempotence-Key для каждого платежа
            var request = new HttpRequestMessage(HttpMethod.Post, $"{YooKassaApiUrl}/payments")
            {
                Content = content
            };
            request.Headers.Add("Idempotence-Key", Guid.NewGuid().ToString());

            try
            {
                var response = await _httpClient.SendAsync(request, cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("YooKassa API error: {StatusCode} - {Content}", 
                        response.StatusCode, responseContent);
                    throw new HttpRequestException($"YooKassa API error: {response.StatusCode}");
                }

                var paymentResponse = JsonSerializer.Deserialize<YooKassaPaymentResponse>(responseContent);
                if (paymentResponse == null)
                {
                    throw new InvalidOperationException("Не удалось десериализовать ответ от YooKassa");
                }

                return paymentResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании платежа в YooKassa");
                throw;
            }
        }

        public async Task<YooKassaPaymentResponse> GetPaymentAsync(
            string paymentId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{YooKassaApiUrl}/payments/{paymentId}",
                    cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("YooKassa API error: {StatusCode} - {Content}",
                        response.StatusCode, responseContent);
                    throw new HttpRequestException($"YooKassa API error: {response.StatusCode}");
                }

                var paymentResponse = JsonSerializer.Deserialize<YooKassaPaymentResponse>(responseContent);
                if (paymentResponse == null)
                {
                    throw new InvalidOperationException("Не удалось десериализовать ответ от YooKassa");
                }

                return paymentResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении платежа из YooKassa");
                throw;
            }
        }
    }

    public class YooKassaPaymentResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public YooKassaAmount? Amount { get; set; }
        public YooKassaConfirmation? Confirmation { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class YooKassaAmount
    {
        public string Value { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
    }

    public class YooKassaConfirmation
    {
        public string Type { get; set; } = string.Empty;
        public string? ConfirmationUrl { get; set; }
        public string? ReturnUrl { get; set; }
    }
}

