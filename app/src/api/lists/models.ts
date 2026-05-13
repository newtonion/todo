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
  parentListItemId?: string | null;
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
    onlyUpcomingOrOverdue: boolean;
}

export interface SearchListResponse {
    totalCount: number;
    pageSize: number;
    offset: number;
    items: SearchListResult[];
}

export interface SearchListItemsRequest {
    text?: string;
    parentListItemId?: string | null;
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
    soonestDueDate: string | null;
}

export interface ListItemSearchResult {
    id: string;
    name: string;
    isCompleted: boolean;
    dueDate?: string | null;
    totalChildren: number;
    totalChildrenCompleted: number;
    soonestChildDueDate?: string | null;
}

export interface ListPrintItemResult {
    id: string;
    name: string;
    isCompleted: boolean;
    subItems: ListPrintItemResult[];
}

export interface ListPrintResult {
    id: string;
    name: string;
    items: ListPrintItemResult[];
}

export interface GetListResult {
    id: string;
    name: string;
    category: string;
    categoryId?: string | null;
    isCompleted: boolean;
    archived: boolean;
}

export interface CountListResult {
    totalItems: number;
    completedItems: number;
}
