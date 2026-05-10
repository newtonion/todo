import { useMemo } from "react";
import useFetchWithAuth from "../shared/useFetchWithAuth";

export const useUserApi = () => {
  const fetchWithAuth = useFetchWithAuth();

  return useMemo(
    () => ({
      getUser: () => fetchWithAuth<void>('user', { method: 'GET' }),
    }),
    [fetchWithAuth]
  );
};
