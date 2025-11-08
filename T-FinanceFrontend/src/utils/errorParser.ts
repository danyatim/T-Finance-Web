import { ApiError } from '../services/api';
import { ValidationErrors } from './validators';

/**
 * Парсит сообщение об ошибке с бэкенда и сопоставляет его с полями формы
 */
export const parseValidationError = (error: unknown): ValidationErrors => {
  const errors: ValidationErrors = {};

  if (error instanceof ApiError && error.response?.message) {
    const message = error.response.message.toLowerCase();

    // Ошибки валидации пароля
    if (message.includes('пароль')) {
      if (message.includes('минимум')) {
        errors.password = 'Пароль должен содержать минимум 8 символов';
      } else if (message.includes('заглавн')) {
        errors.password = 'Пароль должен содержать хотя бы одну заглавную букву';
      } else if (message.includes('строчн')) {
        errors.password = 'Пароль должен содержать хотя бы одну строчную букву';
      } else if (message.includes('цифр')) {
        errors.password = 'Пароль должен содержать хотя бы одну цифру';
      } else if (message.includes('специальн') || message.includes('спецсимвол')) {
        errors.password = 'Пароль должен содержать хотя бы один специальный символ (!@#$%^&*()_+-=[]{};\':"\\|,.<>/?';
      } else if (message.includes('длинн')) {
        errors.password = 'Пароль слишком длинный (максимум 128 символов)';
      } else if (message.includes('пуст')) {
        errors.password = 'Пароль не может быть пустым';
      } else {
        errors.password = error.response.message;
      }
    }
    // Ошибки валидации email
    else if (message.includes('email')) {
      if (message.includes('некорректн') || message.includes('формат')) {
        errors.email = 'Некорректный формат email';
      } else if (message.includes('пуст')) {
        errors.email = 'Email не может быть пустым';
      } else if (message.includes('длинн')) {
        errors.email = 'Email слишком длинный (максимум 254 символа)';
      } else {
        errors.email = error.response.message;
      }
    }
    // Ошибки валидации логина
    else if (message.includes('логин')) {
      if (message.includes('минимум')) {
        errors.username = 'Логин должен содержать минимум 3 символа';
      } else if (message.includes('максимум') || message.includes('более')) {
        errors.username = 'Логин не может содержать более 50 символов';
      } else if (message.includes('буквы') || message.includes('цифры') || message.includes('дефис') || message.includes('подчеркивание')) {
        errors.username = 'Логин может содержать только буквы, цифры, дефис и подчеркивание';
      } else if (message.includes('пуст')) {
        errors.username = 'Логин не может быть пустым';
      } else if (message.includes('зарегистрирован') || message.includes('уже')) {
        errors.username = error.response.message;
      } else {
        errors.username = error.response.message;
      }
    }
    // Общие ошибки (например, пользователь уже существует)
    else if (message.includes('зарегистрирован') || message.includes('уже существует')) {
      // Пытаемся определить, какое поле дублируется
      if (message.includes('email')) {
        errors.email = error.response.message;
      } else if (message.includes('логин')) {
        errors.username = error.response.message;
      } else {
        // Общая ошибка - показываем под всеми полями или как общую ошибку
        errors.email = error.response.message;
        errors.username = error.response.message;
      }
    }
    // Ошибки входа
    else if (message.includes('неверный') || message.includes('неверен')) {
      errors.username = 'Неверный логин или пароль';
      errors.password = 'Неверный логин или пароль';
    }
    // Rate limiting
    else if (message.includes('rate limit') || message.includes('слишком много') || message.includes('попытк')) {
      errors.username = 'Слишком много попыток. Попробуйте позже.';
    }
    // Если не удалось определить поле, показываем общую ошибку
    else {
      // Пытаемся показать ошибку в наиболее вероятном поле
      if (error.status === 400) {
        // Bad Request - скорее всего ошибка валидации
        errors.password = error.response.message;
      } else if (error.status === 401) {
        // Unauthorized - ошибка входа
        errors.username = error.response.message;
        errors.password = error.response.message;
      }
    }
  } else if (error instanceof Error) {
    // Если это обычная ошибка, показываем общее сообщение
    errors.password = error.message;
  }

  return errors;
};

/**
 * Извлекает общее сообщение об ошибке (для alert или общего блока ошибок)
 */
export const getErrorMessage = (error: unknown): string => {
  if (error instanceof ApiError && error.response?.message) {
    return error.response.message;
  }
  if (error instanceof Error) {
    return error.message;
  }
  return 'Произошла ошибка';
};

