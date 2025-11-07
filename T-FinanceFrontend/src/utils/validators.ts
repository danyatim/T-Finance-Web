export interface ValidationErrors {
  email?: string;
  username?: string;
  password?: string;
}

export const validateEmail = (email: string): string | undefined => {
  if (!email) {
    return 'Введите email';
  }
  if (!/\S+@\S+\.\S+/.test(email)) {
    return 'Некорректный формат email';
  }
  return undefined;
};

export const validateUsername = (username: string): string | undefined => {
  if (!username) {
    return 'Введите логин';
  }
  if (username.length < 3) {
    return 'Логин должен быть минимум 3 символа';
  }
  return undefined;
};

export const validatePassword = (password: string): string | undefined => {
  if (!password || password.length < 8) {
    return 'Пароль должен быть минимум 8 символов';
  }
  return undefined;
};

export const validateLoginForm = (username: string, password: string): ValidationErrors => {
  const errors: ValidationErrors = {};
  
  const usernameError = validateUsername(username);
  if (usernameError) {
    errors.username = usernameError;
  }

  const passwordError = validatePassword(password);
  if (passwordError) {
    errors.password = passwordError;
  }

  return errors;
};

export const validateRegisterForm = (
  email: string,
  username: string,
  password: string
): ValidationErrors => {
  const errors: ValidationErrors = {};

  const emailError = validateEmail(email);
  if (emailError) {
    errors.email = emailError;
  }

  const usernameError = validateUsername(username);
  if (usernameError) {
    errors.username = usernameError;
  }

  const passwordError = validatePassword(password);
  if (passwordError) {
    errors.password = passwordError;
  }

  return errors;
};

