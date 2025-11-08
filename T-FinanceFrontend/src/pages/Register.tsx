import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { validateRegisterForm, ValidationErrors } from '../utils/validators';
import { parseValidationError } from '../utils/errorParser';
import { ApiError } from '../services/api';
import './Auth.css';

export default function Register() {
  const navigate = useNavigate();
  const { register } = useAuth();
  const [passwordVisible, setPasswordVisible] = useState(false);
  const [formData, setFormData] = useState({ email: '', username: '', password: '' });
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [isLoading, setIsLoading] = useState(false);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
    // Очищаем ошибку при изменении поля
    if (errors[e.target.name as keyof ValidationErrors]) {
      setErrors({ ...errors, [e.target.name]: undefined });
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const validationErrors = validateRegisterForm(
      formData.email,
      formData.username,
      formData.password
    );

    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setIsLoading(true);
    setErrors({}); // Очищаем предыдущие ошибки
    
    try {
      await register({
        email: formData.email,
        login: formData.username,
        password: formData.password,
      });
      alert('Успешная регистрация');
      navigate('/login');
    } catch (error: unknown) {
      // Парсим ошибки валидации с бэкенда
      const validationErrors = parseValidationError(error);
      
      // Если есть специфичные ошибки для полей, показываем их
      if (Object.keys(validationErrors).length > 0) {
        setErrors(validationErrors);
      } else {
        // Если не удалось распарсить, показываем общую ошибку
        const message = error instanceof ApiError 
          ? (error.response?.message || error.message)
          : error instanceof Error 
          ? error.message 
          : 'Ошибка регистрации';
        setErrors({ password: message });
      }
    } finally {
      setIsLoading(false);
    }
  };

  const togglePasswordVisibility = () => setPasswordVisible(!passwordVisible);

  return (
    <div className="auth-container">
      <h1>Регистрация</h1>
      <form onSubmit={handleSubmit}>
        <div>
          <label htmlFor="email-input">Email:</label>
          <input
            id="email-input"
            name="email"
            type="email"
            value={formData.email}
            onChange={handleChange}
            autoComplete="off"
            title=""
            disabled={isLoading}
            className={errors.email ? 'error-field' : ''}
          />
          {errors.email && <label className="error">{errors.email}</label>}
        </div>
        <div>
          <label htmlFor="login-input">Логин:</label>
          <input
            id="login-input"
            name="username"
            value={formData.username}
            onChange={handleChange}
            autoComplete="off"
            title=""
            disabled={isLoading}
            className={errors.username ? 'error-field' : ''}
          />
          {errors.username && <label className="error">{errors.username}</label>}
        </div>
        <div style={{ position: 'relative' }}>
          <label htmlFor="password-input">Пароль:</label>
          <input
            type={passwordVisible ? 'text' : 'password'}
            id="password-input"
            name="password"
            value={formData.password}
            onChange={handleChange}
            autoComplete="off"
            title=""
            style={{ width: '100%' }}
            disabled={isLoading}
            className={errors.password ? 'error-field' : ''}
          />
          {errors.password && <label className="error">{errors.password}</label>}
          <button
            className="password-visible-button"
            type="button"
            onClick={togglePasswordVisibility}
            aria-label={passwordVisible ? 'Скрыть пароль' : 'Показать пароль'}
            disabled={isLoading}
          >
            {passwordVisible ? '🙈' : '👁️'}
          </button>
        </div>
        <button className="submit-btn" type="submit" disabled={isLoading}>
          {isLoading ? 'Регистрация...' : 'Зарегистрироваться'}
        </button>
      </form>
      <div className="login">
        <label className="login-label">Уже есть аккаунт?</label>
        <button
          className="toggle-button"
          onClick={() => navigate('/login')}
          disabled={isLoading}
        >
          Вход
        </button>
      </div>
    </div>
  );
}

