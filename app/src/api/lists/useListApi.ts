import { useMemo } from "react";
import useFetchWithAuth from "../shared/useFetchWithAuth";
import type {
  CreateListItemRequest,
  CreateListRequest,
  CreateListResponse,
  CountListResult,
  GetListResult,
  RenameListItemRequest,
  SearchListItemsRequest,
  SearchListItemsResponse,
  SearchListRequest,
  SearchListResponse,
  SetListItemDueDateRequest,
  UpdateListCategoryRequest,
  UpdateListRequest,
} from "./models";

export const useListApi = () => {
  const fetchWithAuth = useFetchWithAuth();

  const buildListItemsSearchEndpoint = (listId: string, request: SearchListItemsRequest) => {
    const query = new URLSearchParams({
      pageSize: request.pageSize.toString(),
      offset: request.offset.toString(),
      'orderBy.field': request.orderBy.field,
      'orderBy.ascending': request.orderBy.ascending.toString(),
    });

    if (request.text) {
      query.set('text', request.text);
    }

    return `lists/${listId}/items?${query.toString()}`;
  };

  return useMemo(
    () => ({
      createListItem: (listId: string, request: CreateListItemRequest) => fetchWithAuth<string, CreateListItemRequest>(`lists/${listId}/items`, { method: 'POST', body: request }),
      createList: (request: CreateListRequest) => fetchWithAuth<CreateListResponse, CreateListRequest>('list', { method: 'POST', body: request }),
      getList: (id: string) => fetchWithAuth<GetListResult>(`list/${id}`),
      renameList: (id: string, request: UpdateListRequest) => fetchWithAuth<void, UpdateListRequest>(`list/${id}`, { method: 'PUT', body: request }),
      searchLists: (request: SearchListRequest) => fetchWithAuth<SearchListResponse, SearchListRequest>('list/search', { method: 'POST', body: request }),
      searchListItems: (listId: string, request: SearchListItemsRequest) => fetchWithAuth<SearchListItemsResponse>(buildListItemsSearchEndpoint(listId, request)),
      setListCategory: (id: string, request: UpdateListCategoryRequest) => fetchWithAuth<void, UpdateListCategoryRequest>(`list/${id}/category`, { method: 'POST', body: request }),
      archiveList: (id: string) => fetchWithAuth<void>(`list/${id}/archive`, { method: 'POST' }),
      completeList: (id: string) => fetchWithAuth<void>(`list/${id}/complete`, { method: 'POST' }),
      deleteListItem: (listId: string, itemId: string) => fetchWithAuth<void>(`lists/${listId}/items/${itemId}`, { method: 'DELETE' }),
      renameListItem: (listId: string, itemId: string, request: RenameListItemRequest) => fetchWithAuth<void, RenameListItemRequest>(`lists/${listId}/items/${itemId}/rename`, { method: 'POST', body: request }),
      setListItemDueDate: (listId: string, itemId: string, request: SetListItemDueDateRequest) => fetchWithAuth<void, SetListItemDueDateRequest>(`lists/${listId}/items/${itemId}/due-date`, { method: 'POST', body: request }),
      toggleListItemCompletion: (listId: string, itemId: string) => fetchWithAuth<void>(`lists/${listId}/items/${itemId}/toggle`, { method: 'POST' }),
      getCounts: (listId: string) => fetchWithAuth<CountListResult>(`list/${listId}/counts`, { method: 'GET' }),
    }
),
    [fetchWithAuth]
  );
};
