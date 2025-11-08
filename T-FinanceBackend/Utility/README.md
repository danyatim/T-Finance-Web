# Утилиты валидации

## PasswordValidator

Валидатор паролей с требованиями безопасности:

- **Минимальная длина**: 8 символов
- **Максимальная длина**: 128 символов
- **Требования**:
  - Хотя бы одна заглавная буква (A-Z, А-Я, Ё)
  - Хотя бы одна строчная буква (a-z, а-я, ё)
  - Хотя бы одна цифра (0-9)
  - Хотя бы один специальный символ (!@#$%^&*()_+-=[]{};':"\\|,.<>/?)

### Использование:
```csharp
var (isValid, errorMessage) = PasswordValidator.Validate(password);
if (!isValid)
{
    return BadRequest(new { message = errorMessage });
}
```

## EmailValidator

Валидатор email адресов:

- Проверка формата по RFC 5322 (упрощенный)
- Максимальная длина: 254 символа
- Проверка на пустоту

### Использование:
```csharp
var (isValid, errorMessage) = EmailValidator.Validate(email);
if (!isValid)
{
    return BadRequest(new { message = errorMessage });
}
```

## LoginValidator

Валидатор логинов:

- **Минимальная длина**: 3 символа
- **Максимальная длина**: 50 символов
- **Разрешенные символы**: буквы, цифры, дефис (-), подчеркивание (_)

### Использование:
```csharp
var (isValid, errorMessage) = LoginValidator.Validate(login);
if (!isValid)
{
    return BadRequest(new { message = errorMessage });
}
```

