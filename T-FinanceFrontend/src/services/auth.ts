import { api } from './api';
import { API_ENDPOINTS } from '../utils/constants';

export interface LoginRequest {
  LoginOrEmail: string;
  Password: string;
}

export interface RegisterRequest {
  email: string;
  login: string;
  password: string;
}

export interface AuthResponse {
  message?: string;
  username?: string;
}

export const authService = {
  async login(credentials: LoginRequest): Promise<AuthResponse> {
    return api.post<AuthResponse>(API_ENDPOINTS.AUTH.LOGIN, credentials);
  },

  async register(data: RegisterRequest): Promise<AuthResponse> {
    return api.post<AuthResponse>(API_ENDPOINTS.AUTH.REGISTER, data);
  },

  async logout(): Promise<void> {
    await api.post(API_ENDPOINTS.AUTH.LOGOUT);
  },

  async validate(): Promise<AuthResponse> {
    return api.get<AuthResponse>(API_ENDPOINTS.AUTH.VALIDATE);
  },
};

