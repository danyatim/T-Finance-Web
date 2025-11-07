import { useState } from 'react';
import { useAuth } from '../hooks/useAuth';
import { api, ApiError } from '../services/api';
import { API_ENDPOINTS } from '../utils/constants';
import './Dashboard.css';

export default function Dashboard() {
  const { logout, username } = useAuth();
  const [isLoading, setIsLoading] = useState(false);
  const [activeTab, setActiveTab] = useState('Главная');

  const handleDownloadZip = async () => {
    setIsLoading(true);
    try {
      const blob = await api.get<Blob>(API_ENDPOINTS.FILES.APP);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'T-Finance.zip';
      document.body.appendChild(a);
      a.click();
      a.remove();
      window.URL.revokeObjectURL(url);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Не удалось скачать файл';
      alert(message);
    } finally {
      setIsLoading(false);
    }
  };

  const handlePremium = async () => {
    setIsLoading(true);
    try {
      await api.post(API_ENDPOINTS.USER.PREMIUM);
      await logout();
      alert('Успех');
    } catch (error: unknown) {
      if (error instanceof ApiError) {
        if (error.status === 403) {
          alert('Доступ запрещен. Возможно, у вас уже есть премиум подписка или недостаточно прав.');
        } else if (error.status === 401) {
          alert('Сессия истекла. Пожалуйста, войдите снова.');
          await logout();
        } else {
          alert(error.message || 'Не удалось получить подписку');
        }
      } else {
        const message = error instanceof Error ? error.message : 'Не удалось получить подписку';
        alert(message);
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="dashboard-container">
      {/* Header */}
      <header className="dashboard-header">
        <div className="header-left">
          <h1 className="app-title">T-FinanceWeb</h1>
        </div>
        <div className="header-right">
          <button className="header-link">Связаться</button>
          <button className="header-link" onClick={handlePremium} disabled={isLoading}>
            Premium
          </button>
          <button className="header-link" onClick={handleDownloadZip} disabled={isLoading}>
            Скачать T-Finance
          </button>
          <button className="header-link" onClick={logout} disabled={isLoading}>
            Выйти
          </button>
        </div>
      </header>

      {/* Main Content */}
      <div className="dashboard-content">
        {/* Left Sidebar - User Section */}
        <aside className="user-section">
          <div className="user-avatar">
            <svg width="60" height="60" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <circle cx="12" cy="8" r="4" stroke="currentColor" strokeWidth="2" fill="none"/>
              <path d="M6 21v-2a4 4 0 0 1 4-4h4a4 4 0 0 1 4 4v2" stroke="currentColor" strokeWidth="2" fill="none"/>
            </svg>
          </div>
          <div className="user-name">{username ?? 'User'}</div>
        </aside>

        {/* Main Area */}
        <main className="main-area">
          {/* Navigation Tabs */}
          <div className="nav-tabs">
            <button
              className={`nav-tab ${activeTab === 'Профиль' ? 'active' : ''}`}
              onClick={() => setActiveTab('Профиль')}
            >
              Профиль
            </button>
            <button
              className={`nav-tab ${activeTab === 'Главная' ? 'active' : ''}`}
              onClick={() => setActiveTab('Главная')}
            >
              Главная
            </button>
            <button
              className={`nav-tab ${activeTab === 'Склад' ? 'active' : ''}`}
              onClick={() => setActiveTab('Склад')}
            >
              Склад
            </button>
          </div>

          {/* Financial Cards */}
          <div className="financial-cards">
            <div className="financial-card">
              <div className="card-title">Выручка</div>
              <div className="card-value">1.000.000$</div>
            </div>
            <div className="financial-card">
              <div className="card-title">Расходы</div>
              <div className="card-value">500.000$</div>
            </div>
            <div className="financial-card">
              <div className="card-title">Прибыль</div>
              <div className="card-value">500.000$</div>
            </div>
          </div>

          {/* Bottom Section */}
          <div className="bottom-section">
            {/* Accounts Card */}
            <div className="accounts-card">
              <div className="accounts-title">Счета:</div>
              <div className="accounts-content">
                {/* Accounts list will be here */}
              </div>
            </div>

            {/* Empty Content Card */}
            <div className="empty-card">
              {/* Empty content area */}
            </div>
          </div>
        </main>
      </div>

      {/* Footer */}
      <footer className="dashboard-footer">
        <p>
          Если у вас есть жалобы или предложения, пожалуйста, свяжитесь с нами, нажав кнопку «Связаться» выше.
        </p>
      </footer>
    </div>
  );
}
