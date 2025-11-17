namespace TFinanceBackend.Models
{
    public class Session
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty; // UUID
        public string JWToken { get; set; } = string.Empty;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
