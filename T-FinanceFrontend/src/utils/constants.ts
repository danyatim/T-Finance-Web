const envApiBaseUrl = import.meta.env.VITE_API_BASE_URL;
export const API_BASE_URL = envApiBaseUrl !== undefined ? envApiBaseUrl : "https://localhost:7260"; //'http://127.0.0.1:5000';

// Telegram канал для связи
const envTelegramChannel = import.meta.env.VITE_TELEGRAM_CHANNEL;
export const TELEGRAM_CHANNEL_URL = envTelegramChannel !== undefined 
  ? envTelegramChannel 
  : 'https://t.me/t_finance_web'; // Замените на ваш телеграм канал

export const API_ENDPOINTS = {
  AUTH: {
    LOGIN: '/api/auth/login',
    REGISTER: '/api/auth/register',
    LOGOUT: '/api/auth/logout',
    VALIDATE: '/api/auth/validate',
  },
  USER: {
    PREMIUM: '/api/user/premium',
  },
  PAYMENT: {
    CREATE: '/api/payment/create',
    STATUS: '/api/payment/status',
  },
  FILES: {
    APP: '/api/files/app',
  },
} as const;

