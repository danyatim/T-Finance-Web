import { useState } from 'react';
import { useAuth } from '../hooks/useAuth';
import { api, ApiError } from '../services/api';
import { API_ENDPOINTS, TELEGRAM_CHANNEL_URL } from '../utils/constants';
import NavButton from '@/components/dashboard/NavButtonComponent/NavButton';
import FinancialCard from '@/components/dashboard/FinancialCardComponent/FinancialCard';
import HeaderLink from '@/components/dashboard/LinkComponent/HeaderLink';
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
      if (error instanceof ApiError){
        if (error.status === 403){
          alert("Недостаточно прав");
        }
      } else{
        const message = error instanceof Error ? error.message : 'Не удалось скачать файл';
        alert(message);
      }
      
      
    } finally {
      setIsLoading(false);
    }
  };

  const handlePremium = async () => {
    setIsLoading(true);
    try {
      const response = await api.post<{
        paymentId: string;
        confirmationUrl: string;
        status: string;
      }>(API_ENDPOINTS.PAYMENT.CREATE);

      if (response.confirmationUrl) {
        // Перенаправляем на страницу оплаты YooKassa
        window.location.href = response.confirmationUrl;
      } else {
        alert('Ошибка: не получена ссылка на оплату');
      }
    } catch (error: unknown) {
      if (error instanceof ApiError) {
        if (error.status === 400) {
          alert(error.response?.message || 'Не удалось создать платеж. Возможно, у вас уже есть Premium подписка.');
        } else if (error.status === 401) {
          alert('Сессия истекла. Пожалуйста, войдите снова.');
          await logout();
        } else {
          alert(error.message || 'Не удалось создать платеж');
        }
      } else {
        const message = error instanceof Error ? error.message : 'Не удалось создать платеж';
        alert(message);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleContact = () => {
    window.open(TELEGRAM_CHANNEL_URL, '_blank', 'noopener,noreferrer');
  };

  return (
    <div className="dashboard-container">
      {/* Header */}
      <header className="dashboard-header">
        <div className="header-left">
          <h1 className="app-title">T-FinanceWeb</h1>
        </div>
        <div className="header-right">
          <HeaderLink onClick={handleContact} isDisable={isLoading} > Связаться </HeaderLink>
          <HeaderLink onClick={handlePremium} isDisable={isLoading} > Premium </HeaderLink>
          <HeaderLink onClick={handleDownloadZip} isDisable={isLoading} > Скачать T-Finance </HeaderLink>
          <HeaderLink onClick={logout} isDisable={isLoading} > Выйти </HeaderLink>
        </div>
      </header>

      {/* Main Content */}
      <div className="dashboard-content">
        {/* Left Sidebar - User Section */}
        <aside className="user-section">
          <div className="user-avatar">
            <svg width="60" height="60" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <circle cx="12" cy="8" r="4" stroke="currentColor" strokeWidth="1" fill="none"/>
              <path d="M6 21v-2a4 4 0 0 1 4-4h4a4 4 0 0 1 4 4v2" stroke="currentColor" strokeWidth="1" fill="none"/>
            </svg>
          </div>
          <div className="user-name">{username ?? 'User'}</div>
        </aside>

        {/* Main Area */}
        <main className="main-area">
          {/* Navigation Tabs */}
          <div className="nav-tabs">
            <NavButton current={activeTab} onClick={() => setActiveTab('Профиль')}>Профиль</NavButton>
            <NavButton current={activeTab} onClick={() => setActiveTab('Главная')}>Главная</NavButton>
            <NavButton current={activeTab} onClick={() => setActiveTab('Склад')}>Склад</NavButton>
          </div>

          {/* Financial Cards */}
          <div className="financial-cards">
            <FinancialCard CardTitle="Выручка" CardValue="1.000.000$"></FinancialCard>
            <FinancialCard CardTitle="Расходы" CardValue="500.000$"></FinancialCard>
            <FinancialCard CardTitle="Прибыль" CardValue="250.000$"></FinancialCard>
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
