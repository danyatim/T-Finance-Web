import { useNavigate } from 'react-router-dom';
import { useAuthContext } from '../contexts/AuthContext';
import { authService, LoginRequest, RegisterRequest } from '../services/auth';

export const useAuth = () => {
  const navigate = useNavigate();
  const { validateAuth, logout: contextLogout, username, setUsername } = useAuthContext();

  const login = async (credentials: LoginRequest) => {
    try {
      const response = await authService.login(credentials);
      if (response.username) {
        setUsername(response.username);
      }
      await validateAuth();
      navigate('/');
    } catch (error) {
      throw error;
    }
  };

  const register = async (data: RegisterRequest) => {
    try {
      const response = await authService.register(data);
      return response;
    } catch (error) {
      throw error;
    }
  };

  const logout = async () => {
    await contextLogout();
    navigate('/login');
  };

  return {
    login,
    register,
    logout,
    username,
  };
};

