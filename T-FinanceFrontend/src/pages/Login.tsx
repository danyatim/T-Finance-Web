import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { validateLoginForm, ValidationErrors } from '../utils/validators';
import './Auth.css';

export default function Login() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [passwordVisible, setPasswordVisible] = useState(false);
  const [formData, setFormData] = useState({ username: '', password: '' });
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
    const validationErrors = validateLoginForm(formData.username, formData.password);
    
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setIsLoading(true);
    try {
      await login({
        LoginOrEmail: formData.username,
        Password: formData.password,
      });
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Ошибка входа';
      alert(message);
    } finally {
      setIsLoading(false);
    }
  };

  const togglePasswordVisibility = () => setPasswordVisible(!passwordVisible);

  return (
    <div className="auth-container">
      <h1>Вход</h1>
      <form onSubmit={handleSubmit}>
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
          />
          {errors.username && <div className="error">{errors.username}</div>}
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
          {isLoading ? 'Вход...' : 'Войти'}
        </button>
      </form>
      <button
        className="toggle-button"
        onClick={() => navigate('/register')}
        disabled={isLoading}
      >
        Зарегистрироваться
      </button>
    </div>
  );
}

