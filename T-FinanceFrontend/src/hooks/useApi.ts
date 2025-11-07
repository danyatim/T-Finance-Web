import { useState } from 'react';
import { ApiError } from '../services/api';

export interface UseApiOptions<T> {
  onSuccess?: (data: T) => void;
  onError?: (error: ApiError) => void;
}

export const useApi = <T = unknown, P = unknown>(
  apiFunction: (params?: P) => Promise<T>,
  options?: UseApiOptions<T>
) => {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);

  const execute = async (params?: P) => {
    setLoading(true);
    setError(null);
    try {
      const result = await apiFunction(params);
      setData(result);
      if (options?.onSuccess) {
        options.onSuccess(result);
      }
      return result;
    } catch (err) {
      const apiError = err instanceof ApiError ? err : new ApiError('Unknown error', 0);
      setError(apiError);
      if (options?.onError) {
        options.onError(apiError);
      }
      throw apiError;
    } finally {
      setLoading(false);
    }
  };

  return { data, loading, error, execute };
};

