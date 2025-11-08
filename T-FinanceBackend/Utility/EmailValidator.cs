using System.Text.RegularExpressions;

namespace TFinanceBackend.Utility
{
    public static class EmailValidator
    {
        // RFC 5322 совместимый regex (упрощенный)
        private static readonly Regex EmailRegex = new(
            @"^[a-zA-Z0-9](?:[a-zA-Z0-9._-]*[a-zA-Z0-9])?@[a-zA-Z0-9](?:[a-zA-Z0-9.-]*[a-zA-Z0-9])?\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

