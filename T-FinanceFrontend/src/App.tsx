import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuthContext } from './contexts/AuthContext';
import Login from './pages/Login';
import Register from './pages/Register';
import Dashboard from './pages/Dashboard';
import './App.css';

function App() {
  const { isAuthenticated, isLoading } = useAuthContext();

  if (isLoading) {
    return <div>Загрузка...</div>;
  }

  return (
    <Routes>
      <Route
        path="/login"
        element={isAuthenticated ? <Navigate to="/" replace /> : <Login />}
      />
      <Route
        path="/register"
        element={isAuthenticated ? <Navigate to="/" replace /> : <Register />}
      />
      <Route
        path="/"
        element={isAuthenticated ? <Dashboard /> : <Navigate to="/login" replace />}
      />
    </Routes>
  );
}

export default App;

