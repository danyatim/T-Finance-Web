const envApiBaseUrl = import.meta.env.VITE_API_BASE_URL;
export const API_BASE_URL = envApiBaseUrl !== undefined ? envApiBaseUrl : 'http://127.0.0.1:5000';

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
  FILES: {
    APP: '/api/files/app',
  },
} as const;

