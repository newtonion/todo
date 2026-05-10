import useFetchWithAuth from "../shared/useFetchWithAuth";
import { useMemo } from "react";
import type { SearchCategoryRequest, SearchCategoryResponse } from "./models";

export const useCategoryApi = () => {
  const fetchWithAuth = useFetchWithAuth();

  return useMemo(
    () => ({
      searchCategories: (request: SearchCategoryRequest) =>
        fetchWithAuth<SearchCategoryResponse, SearchCategoryRequest>('category/search', {
          method: 'POST',
          body: request,
        }),

    }
),
    [fetchWithAuth]
  );
};