import { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { authService } from '../services/auth';

interface AuthContextType {
  isAuthenticated: boolean;
  isLoading: boolean;
  username: string | null;
  validateAuth: () => Promise<void>;
  logout: () => Promise<void>;
  setUsername: (username: string) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [username, setUsername] = useState<string | null>(
    localStorage.getItem('username')
  );

  const validateAuth = async () => {
    try {
      const response = await authService.validate();
      setIsAuthenticated(true);
      if (response.username) {
        setUsername(response.username);
        localStorage.setItem('username', response.username);
      }
    } catch (error) {
      // При ошибке (например, бэкенд недоступен) считаем пользователя неавторизованным
      setIsAuthenticated(false);
      setUsername(null);
      localStorage.removeItem('username');
    } finally {
      setIsLoading(false);
    }
  };

  const logout = async () => {
    try {
      await authService.logout();
    } catch {
      // Игнорируем ошибки при выходе
    } finally {
      setIsAuthenticated(false);
      setUsername(null);
      localStorage.removeItem('username');
    }
  };

  const updateUsername = (newUsername: string) => {
    setUsername(newUsername);
    localStorage.setItem('username', newUsername);
  };

  useEffect(() => {
    validateAuth();
  }, []);

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        isLoading,
        username,
        validateAuth,
        logout,
        setUsername: updateUsername,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuthContext = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuthContext must be used within an AuthProvider');
  }
  return context;
};

