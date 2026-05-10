import { useCallback, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { ApiErrorContext } from './apiErrorContextValue';
import './ApiErrorContext.css';

type ApiErrorProviderProps = {
  children: ReactNode;
};

export const ApiErrorProvider = ({ children }: ApiErrorProviderProps) => {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const showApiError = useCallback((message: string) => {
    setErrorMessage(message);
  }, []);

  const value = useMemo(() => ({ showApiError }), [showApiError]);

  return (
    <ApiErrorContext.Provider value={value}>
      {children}
      {errorMessage && (
        <div className="api-error-toast" role="alert">
          <div className="api-error-toast-content">
            <strong>{errorMessage}</strong>
          </div>
          <button
            aria-label="Dismiss API error"
            className="api-error-toast-close"
            type="button"
            onClick={() => setErrorMessage(null)}
          >
            x
          </button>
        </div>
      )}
    </ApiErrorContext.Provider>
  );
};
