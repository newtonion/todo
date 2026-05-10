import type { FieldOrderRequest } from "../shared/models";

export interface SearchCategoryRequest {
    text: string;
    orderBy: FieldOrderRequest;
}
export interface SearchCategoryResponse {
    totalCount: number;
    pageSize: number;
    offset: number;
    items: CategorySearchResult[];
}
export interface CategorySearchResult {
    id: string;
    name: string;
}