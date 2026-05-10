import { createContext, useContext } from 'react';

export type ApiErrorContextValue = {
  showApiError: (message: string) => void;
};

export const ApiErrorContext = createContext<ApiErrorContextValue>({
  showApiError: () => undefined,
});

export const useApiError = () => useContext(ApiErrorContext);
