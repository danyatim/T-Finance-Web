using System.Text.RegularExpressions;

namespace TFinanceBackend.Utility
{
    public static class LoginValidator
    {
        private const int MinLength = 3;
        private const int MaxLength = 50;

        // Разрешаем только буквы, цифры, дефис и подчеркивание
        private static readonly Regex LoginRegex = new(
            @"^[a-zA-Z0-9_-]+$",
            RegexOptions.Compiled);

        public static (bool IsValid, string? ErrorMessage) Validate(string? login)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                return (false, "Логин не может быть пустым");
            }

            if (login.Length < MinLength)
            {
                return (false, $"Логин должен содержать минимум {MinLength} символа");
            }

            if (login.Length > MaxLength)
            {
                return (false, $"Логин не может содержать более {MaxLength} символов");
            }

            if (!LoginRegex.IsMatch(login))
            {
                return (false, "Логин может содержать только буквы, цифры, дефис и подчеркивание");
            }

            return (true, null);
        }
    }
}

