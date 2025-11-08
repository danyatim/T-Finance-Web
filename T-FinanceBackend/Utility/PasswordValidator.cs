using System.Text.RegularExpressions;

namespace TFinanceBackend.Utility
{
    public static class PasswordValidator
    {
        private const int MinLength = 8;
        private const int MaxLength = 128;

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
            if (!Regex.IsMatch(password, @"[A-ZА-ЯЁ]"))
            {
                return (false, "Пароль должен содержать хотя бы одну заглавную букву");
            }

            // Проверка на наличие хотя бы одной строчной буквы
            if (!Regex.IsMatch(password, @"[a-zа-яё]"))
            {
                return (false, "Пароль должен содержать хотя бы одну строчную букву");
            }

            // Проверка на наличие хотя бы одной цифры
            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                return (false, "Пароль должен содержать хотя бы одну цифру");
            }

            // Проверка на наличие хотя бы одного спецсимвола
            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            {
                return (false, "Пароль должен содержать хотя бы один специальный символ (!@#$%^&*()_+-=[]{};':\"\\|,.<>/?");
            }

            return (true, null);
        }
    }
}

