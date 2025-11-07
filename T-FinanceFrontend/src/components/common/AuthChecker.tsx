import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthContext } from '../../contexts/AuthContext';

export default function AuthChecker() {
  const navigate = useNavigate();
  const { isAuthenticated, isLoading } = useAuthContext();

  useEffect(() => {
    if (isLoading) return;

    if (isAuthenticated) {
      navigate('/');
    } else {
      navigate('/login');
    }
  }, [isAuthenticated, isLoading, navigate]);

  return null;
}

