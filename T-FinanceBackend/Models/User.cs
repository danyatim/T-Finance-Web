namespace TFinanceBackend.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string? Login { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public bool IsPremium { get; set; }
        public DateTime PremiumCreateAt { get; set; }
        public DateTime PremiumExpiresAt { get; set; }
        public bool IsEmailConfirmed { get; set; } = false;


    }
}
