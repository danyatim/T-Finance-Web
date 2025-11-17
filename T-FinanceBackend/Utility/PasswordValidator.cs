using System.Text.RegularExpressions;

namespace TFinanceBackend.Utility
{
    public static partial class PasswordValidator
    {
        private const int MinLength = 8;
        private const int MaxLength = 128;

        [GeneratedRegex(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]")]
        private static partial Regex Password();

        [GeneratedRegex(@"[0-9]")]
        private static partial Regex PasswordIsNumber();

        [GeneratedRegex(@"[a-zа-яё]")]
        private static partial Regex PasswordLowercaseLetters();

        [GeneratedRegex(@"[A-ZА-ЯЁ]")]
        private static partial Regex PasswordCapitalLetters();


        public static (bool IsValid, string? ErrorMessage) Validate(string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return (false, "Пароль не может быть пустым");
            }

            if (password.Length < MinLength)
            {
                return (false, $"Пароль должен содержать минимум {MinLength} символов");
            }

            if (password.Length > MaxLength)
            {
                return (false, $"Пароль не может содержать более {MaxLength} символов");
            }

            // Проверка на наличие хотя бы одной заглавной буквы
            if (!PasswordCapitalLetters().IsMatch(password))
            {
                return (false, "Пароль должен содержать хотя бы одну заглавную букву");
            }

            // Проверка на наличие хотя бы одной строчной буквы
            if (!PasswordLowercaseLetters().IsMatch(password))
            {
                return (false, "Пароль должен содержать хотя бы одну строчную букву");
            }

            // Проверка на наличие хотя бы одной цифры
            if (!PasswordIsNumber().IsMatch(password))
            {
                return (false, "Пароль должен содержать хотя бы одну цифру");
            }

            // Проверка на наличие хотя бы одного спецсимвола
            if (!Password().IsMatch(password))
            {
                return (false, "Пароль должен содержать хотя бы один специальный символ (!@#$%^&*()_+-=[]{};':\"\\|,.<>/?");
            }

            return (true, null);
        }
    }
}

