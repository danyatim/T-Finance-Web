namespace TFinanceBackend.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string YooKassaPaymentId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // pending, succeeded, canceled
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
        public string? Description { get; set; }
    }
}

