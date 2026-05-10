import { useState, useCallback } from 'react';
import type { ListGetResult } from '../../api/lists/models';
import type { TaskSortField, SortDirection } from '../listSection/Table';
import type { EditState } from '../shared/InlineEditable';

type UseListHeaderProps = {
  selectedListId: string;
  list: ListGetResult | null;
  taskSortField: TaskSortField;
  taskSortDirection: SortDirection;
  taskOffset: number;
  onReload: () => Promise<void>;
  renameList: (id: string, request: { name: string }) => Promise<void>;
  setListCategory: (id: string, request: { category: string | null }) => Promise<void>;
  archiveList: (id: string) => Promise<void>;
  completeList: (id: string) => Promise<void>;
};

export const useListHeader = ({
  selectedListId,
  list,
  onReload,
  renameList,
  setListCategory,
  archiveList,
  completeList,
}: UseListHeaderProps) => {
  const [listNameEditState, setListNameEditState] = useState<EditState | null>(null);
  const [listCategoryEditState, setListCategoryEditState] = useState<EditState | null>(null);

  const handleEditListClick = useCallback(() => {
    if (!list || !selectedListId) {
      return;
    }

    setListNameEditState({
      itemId: selectedListId,
      value: list.name,
      isSaving: false,
    });
  }, [list, selectedListId]);

  const handleListNameChange = useCallback((name: string) => {
    setListNameEditState((current) => 
      current ? { ...current, value: name } : null
    );
  }, []);

  const handleDiscardListName = useCallback(() => {
    setListNameEditState(null);
  }, []);

  const handleSaveListName = useCallback(async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!selectedListId || listNameEditState?.itemId !== selectedListId) {
      return;
    }

    const trimmedName = listNameEditState.value.trim();

    if (!trimmedName) {
      return;
    }

    setListNameEditState({
      ...listNameEditState,
      value: trimmedName,
      isSaving: true,
    });

    try {
      await renameList(selectedListId, { name: trimmedName });
      await onReload();
      setListNameEditState(null);
    } catch {
      setListNameEditState((current) => (
        current?.itemId === selectedListId
          ? { ...current, isSaving: false }
          : current
      ));
    }
  }, [selectedListId, listNameEditState, renameList, onReload]);

  const handleEditCategoryClick = useCallback(() => {
    if (!list || !selectedListId) {
      return;
    }

    setListCategoryEditState({
      itemId: selectedListId,
      value: list.categoryId ?? '',
      isSaving: false,
    });
  }, [list, selectedListId]);

  const handleCategoryChange = useCallback((categoryId: string) => {
    setListCategoryEditState((current) => 
      current ? { ...current, value: categoryId } : null
    );
  }, []);

  const handleDiscardCategory = useCallback(() => {
    setListCategoryEditState(null);
  }, []);

  const handleSaveCategory = useCallback(async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!selectedListId || listCategoryEditState?.itemId !== selectedListId) {
      return;
    }

    setListCategoryEditState({
      ...listCategoryEditState,
      isSaving: true,
    });

    try {
      await setListCategory(selectedListId, { category: listCategoryEditState.value || null });
      await onReload();
      setListCategoryEditState(null);
    } catch {
      setListCategoryEditState((current) => (
        current?.itemId === selectedListId
          ? { ...current, isSaving: false }
          : current
      ));
    }
  }, [selectedListId, listCategoryEditState, setListCategory, onReload]);

  const handleCompletionStatusChange = useCallback(async () => {
    await completeList(selectedListId);
    await onReload();
  }, [selectedListId, completeList, onReload]);

  const handleArchiveStatusChange = useCallback(async () => {
    await archiveList(selectedListId);
    await onReload();
  }, [selectedListId, archiveList, onReload]);

  const isEditingListName = listNameEditState?.itemId === selectedListId;
  const listNameDraft = isEditingListName ? listNameEditState.value : '';
  const canSaveListName = listNameDraft.trim().length > 0 && !listNameEditState?.isSaving;
  const isEditingCategory = listCategoryEditState?.itemId === selectedListId;
  const categoryDraftId = isEditingCategory ? listCategoryEditState.value : '';
  const canSaveCategory = !listCategoryEditState?.isSaving;

  return {
    listNameEditState,
    listCategoryEditState,
    isEditingListName,
    listNameDraft,
    canSaveListName,
    isEditingCategory,
    categoryDraftId,
    canSaveCategory,
    handleEditListClick,
    handleListNameChange,
    handleDiscardListName,
    handleSaveListName,
    handleEditCategoryClick,
    handleCategoryChange,
    handleDiscardCategory,
    handleSaveCategory,
    handleCompletionStatusChange,
    handleArchiveStatusChange,
  };
};
