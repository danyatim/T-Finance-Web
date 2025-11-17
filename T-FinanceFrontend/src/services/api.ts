import { API_BASE_URL } from '../utils/constants';

export interface ApiResponse<T = unknown> {
  message?: string;
  data?: T;
}

export interface BankAccount {
  id: number;
  name: string;
  balance: number;
}

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
    public response?: ApiResponse
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export const api = {
  async request<T = unknown>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${API_BASE_URL}${endpoint}`;
    
    const defaultOptions: RequestInit = {
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
    };

    const response = await fetch(url, {
      ...defaultOptions,
      ...options,
      headers: {
        ...defaultOptions.headers,
        ...options.headers,
      },
    })
    
    if (!response.ok) {
      let errorMessage = `HTTP Error: ${response.status}`;
      let errorData: ApiResponse | undefined;

      try {
        const text = await response.text();
        if (text) {
          errorData = JSON.parse(text);
          errorMessage = errorData?.message || errorMessage;
        }
      } catch {
        // Если не удалось распарсить JSON, используем текст
        errorMessage = response.statusText || errorMessage;
      }

      throw new ApiError(errorMessage, response.status, errorData);
    }

    // Для пустых ответов
    if (response.status === 204 || response.headers.get('content-length') === '0') {
      return {} as T;
    }

    // Для blob ответов (например, файлы)
    const contentType = response.headers.get('content-type');
    if (contentType?.includes('application/octet-stream') || contentType?.includes('application/zip')) {
      return (await response.blob()) as unknown as T;
    }

    try {
      const jsonData = await response.json();
      return jsonData;
    } catch (error) {
      return {} as T;
    }
  },

  get<T = unknown>(endpoint: string, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, { ...options, method: 'GET' });
  },

  post<T = unknown>(endpoint: string, data?: unknown, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
  },

  put<T = unknown>(endpoint: string, data?: unknown, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'PUT',
      body: data ? JSON.stringify(data) : undefined,
    });
  },

  delete<T = unknown>(endpoint: string, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, { ...options, method: 'DELETE' });
  },
};

