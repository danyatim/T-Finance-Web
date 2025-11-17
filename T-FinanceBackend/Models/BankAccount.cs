namespace TFinanceBackend.Models
{
    public class BankAccount
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Название счета
        public decimal Balance { get; set; } = decimal.Zero; // Сумма на счете
        public int UserId { get; set; } // Внешний ключ к пользователю
        public User User { get; set; } = null!;
    }
}
