import { useAuth } from '@clerk/react';
import { useCallback } from 'react';
import { useApiError } from './apiErrorContextValue';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;
const FALLBACK_ERROR_MESSAGE = 'Something has gone wrong';

function buildApiUrl(endpoint: string) {
  if (endpoint.startsWith('http')) {
    return endpoint;
  }

  return `${API_BASE_URL.replace(/\/+$/, '')}/${endpoint.replace(/^\/+/, '')}`;
}

function getErrorMessageFromPayload(payload: unknown): string | null {
  if (!payload) {
    return null;
  }

  if (typeof payload !== 'object') {
    return null;
  }

  const record = payload as Record<string, unknown>;
  const messages: string[] = [];

  if (typeof record.title === 'string') {
    messages.push(record.title);
  }

  if (record.errors && typeof record.errors === 'object') {
    const validationMessages = Object.values(record.errors as Record<string, unknown>)
      .flatMap((value) => Array.isArray(value) ? value : [value])
      .filter((value): value is string => typeof value === 'string');

    messages.push(...validationMessages);
  }

  return messages.length > 0 ? messages.join(' ') : null;
}

async function readApiErrorMessage(response: Response) {
  const contentType = response.headers.get('content-type');

  try {
    if (contentType?.includes('json')) {
      const payload = await response.json();
      return getErrorMessageFromPayload(payload) ?? FALLBACK_ERROR_MESSAGE;
    }
  } catch {
    return FALLBACK_ERROR_MESSAGE;
  }

  return FALLBACK_ERROR_MESSAGE;
}

export default function useFetchWithAuth() {
  const { getToken } = useAuth();
  const { showApiError } = useApiError();

  const fetchWithAuth = useCallback(async <TResponse = unknown, TRequest = unknown>(
    endpoint: string,
    options: Omit<RequestInit, 'body'> & { body?: TRequest } = {}
  ): Promise<TResponse> => {
    const { body, ...requestOptions } = options;
    const token = await getToken();
    const headers: HeadersInit = {
      ...requestOptions.headers,
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    };

    const config: RequestInit = {
      ...requestOptions,
      headers,
    };

    if (body) {
      config.body = JSON.stringify(body);
    }

    const url = buildApiUrl(endpoint);
    let response: Response;

    try {
      response = await fetch(url, config);
    } catch (error) {
      showApiError(FALLBACK_ERROR_MESSAGE);
      throw error;
    }

    if (!response.ok) {
      const message = await readApiErrorMessage(response);
      showApiError(message);
      throw new Error(message);
    }

    // Only parse JSON if there's a response body
    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
      return response.json();
    }
    
    return undefined as TResponse;
  }, [getToken, showApiError]);

  return fetchWithAuth;
}
