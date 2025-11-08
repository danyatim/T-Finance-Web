export interface ValidationErrors {
  email?: string;
  username?: string;
  password?: string;
}

export const validateEmail = (email: string): string | undefined => {
  if (!email) {
    return 'Email не может быть пустым';
  }
  if (email.length > 254) {
    return 'Email слишком длинный (максимум 254 символа)';
  }
  // Более строгая проверка формата email
  if (!/^[a-zA-Z0-9](?:[a-zA-Z0-9._-]*[a-zA-Z0-9])?@[a-zA-Z0-9](?:[a-zA-Z0-9.-]*[a-zA-Z0-9])?\.[a-zA-Z]{2,}$/.test(email)) {
    return 'Некорректный формат email';
  }
  return undefined;
};

export const validateUsername = (username: string): string | undefined => {
  if (!username) {
    return 'Логин не может быть пустым';
  }
  if (username.length < 3) {
    return 'Логин должен содержать минимум 3 символа';
  }
  if (username.length > 50) {
    return 'Логин не может содержать более 50 символов';
  }
  if (!/^[a-zA-Z0-9_-]+$/.test(username)) {
    return 'Логин может содержать только буквы, цифры, дефис и подчеркивание';
  }
  return undefined;
};

export const validatePassword = (password: string): string | undefined => {
  if (!password) {
    return 'Пароль не может быть пустым';
  }
  if (password.length < 8) {
    return 'Пароль должен содержать минимум 8 символов';
  }
  if (password.length > 128) {
    return 'Пароль не может содержать более 128 символов';
  }
  if (!/[A-ZА-ЯЁ]/.test(password)) {
    return 'Пароль должен содержать хотя бы одну заглавную букву';
  }
  if (!/[a-zа-яё]/.test(password)) {
    return 'Пароль должен содержать хотя бы одну строчную букву';
  }
  if (!/[0-9]/.test(password)) {
    return 'Пароль должен содержать хотя бы одну цифру';
  }
  if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)) {
    return 'Пароль должен содержать хотя бы один специальный символ (!@#$%^&*()_+-=[]{};\':"\\|,.<>/?';
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

