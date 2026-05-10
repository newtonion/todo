import type { FieldOrderRequest } from "../shared/models";

export interface CreateListRequest {
  name: string;
  categoryId?: string;
}

export interface CreateListResponse {
  id: string;
}

export interface CreateListItemRequest {
  name: string;
  dueDate?: string | null;
}

export interface RenameListItemRequest {
  name: string;
}

export interface SetListItemDueDateRequest {
  dueDate: string | null;
}

export interface UpdateListRequest {
  name: string;
}

export interface UpdateListCategoryRequest {
  category: string | null;
}

export interface SearchListRequest {
    text: string;
    orderBy: FieldOrderRequest;
    pageSize: number;
    offset: number;
    categoryText?: string;
    includeCompleted: boolean;
    includeArchived: boolean;
}

export interface SearchListResponse {
    totalCount: number;
    pageSize: number;
    offset: number;
    items: SearchListResult[];
}

export interface SearchListItemsRequest {
    text?: string;
    orderBy: FieldOrderRequest;
    pageSize: number;
    offset: number;
}

export interface SearchListItemsResponse {
    totalCount: number;
    pageSize: number;
    offset: number;
    items: ListItemSearchResult[];
}

export interface SearchListResult {
    id: string;
    name: string;
    categoryName?: string;
    isCompleted: boolean;
    archived: boolean;
    totalItems: number;
    completedItems: number;
}

export interface ListItemSearchResult {
    id: string;
    name: string;
    completed: boolean;
    dueDate?: string | null;
}

export interface ListGetResult {
    id: string;
    name: string;
    category: string;
    categoryId?: string | null;
    completed: boolean;
    archived: boolean;
    items: ListItemSearchResult[];
}

export interface ListCountResult {
    totalItems: number;
    completedItems: number;
}