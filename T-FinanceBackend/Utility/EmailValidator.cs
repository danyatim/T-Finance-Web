using System.Text.RegularExpressions;

namespace TFinanceBackend.Utility
{
    public static partial class EmailValidator
    {
        [GeneratedRegex(@"^[a-zA-Z0-9](?:[a-zA-Z0-9._-]*[a-zA-Z0-9])?@[a-zA-Z0-9](?:[a-zA-Z0-9.-]*[a-zA-Z0-9])?\.[a-zA-Z]{2,}$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "ru-RU")]
        private static partial Regex Email();

        // RFC 5322 совместимый regex (упрощенный)
        private static readonly Regex EmailRegex = Email();

        public static (bool IsValid, string? ErrorMessage) Validate(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return (false, "Email не может быть пустым");
            }

            if (email.Length > 254) // RFC 5321 максимальная длина email
            {
                return (false, "Email слишком длинный (максимум 254 символа)");
            }

            if (!EmailRegex.IsMatch(email))
            {
                return (false, "Некорректный формат email");
            }

            return (true, null);
        }

        
    }
}

