import { useCallback, useEffect, useState } from 'react';
import { useListApi } from '../../api/lists/useListApi';
import type { CountListResult, GetListResult, ListItemSearchResult } from '../../api/lists/models';
import { useListHeader } from '../hooks/useListHeader';
import ListHeader from './ListHeader';
import TaskList from './TaskList';
import type { SortDirection, TaskSortField } from './Table';
import './ListSection.css';

type ListSectionProps = {
  selectedListId: string | null;
};

type ListLoadState = {
  listId: string;
  list: GetListResult | null;
  counts: CountListResult | null;
  error: string | null;
};

const TASK_PAGE_SIZE = 10;

const ListSection = ({ selectedListId }: ListSectionProps) => {
  const {
    getCounts,
    createListItem,
    getList,
    renameList,
    renameListItem,
    searchListItems,
    setListItemDueDate,
    setListCategory,
    toggleListItemCompletion,
    deleteListItem,
    archiveList,
    completeList
  } = useListApi();
  const [loadState, setLoadState] = useState<ListLoadState | null>(null);
  const [isCreateTaskModalOpen, setIsCreateTaskModalOpen] = useState(false);
  const [taskSortField, setTaskSortField] = useState<TaskSortField>('dueDate');
  const [taskSortDirection, setTaskSortDirection] = useState<SortDirection>('asc');
  const [taskOffset, setTaskOffset] = useState(0);
  const [totalTaskCount, setTotalTaskCount] = useState(0);

  const loadList = useCallback(async (
    listId: string,
    sortField: TaskSortField,
    sortDirection: SortDirection,
    offset: number
  ) => {
    const [listResult, itemsResult, countsResult] = await Promise.all([
      getList(listId),
      searchListItems(listId, {
        orderBy: {
          field: sortField,
          ascending: sortDirection === 'asc',
        },
        pageSize: TASK_PAGE_SIZE,
        offset,
      }),
      getCounts(listId),
    ]);

    setTotalTaskCount(itemsResult.totalCount);
    setLoadState({
      listId,
      list: {
        ...listResult,
        items: itemsResult.items,
      },
      counts: countsResult,
      error: null,
    });
  }, [getCounts, getList, searchListItems]);

  useEffect(() => {
    if (!selectedListId) {
      return;
    }

    let isActive = true;

    Promise.all([
      getList(selectedListId),
      searchListItems(selectedListId, {
        orderBy: {
          field: taskSortField,
          ascending: taskSortDirection === 'asc',
        },
        pageSize: TASK_PAGE_SIZE,
        offset: taskOffset,
      }),
      getCounts(selectedListId),
    ])
      .then(([listResult, itemsResult, countsResult]) => {
        if (isActive) {
          setTotalTaskCount(itemsResult.totalCount);
          setLoadState({
            listId: selectedListId,
            list: {
              ...listResult,
              items: itemsResult.items,
            },
            counts: countsResult,
            error: null,
          });
        }
      })
      .catch((err) => {
        if (isActive) {
          setLoadState({
            listId: selectedListId,
            list: null,
            counts: null,
            error: err instanceof Error ? err.message : 'Failed to load list',
          });
          setTotalTaskCount(0);
        }
      });

    return () => {
      isActive = false;
    };
  }, [getCounts, getList, searchListItems, selectedListId, taskOffset, taskSortDirection, taskSortField]);

  const list = loadState?.listId === selectedListId ? loadState.list : null;
  const listCounts = loadState?.listId === selectedListId ? loadState.counts : null;
  const listError = loadState?.listId === selectedListId ? loadState.error : null;
  const isLoadingList = !list && !listError;

  const reloadList = useCallback(async () => {
    if (selectedListId) {
      await loadList(selectedListId, taskSortField, taskSortDirection, taskOffset);
    }
  }, [loadList, selectedListId, taskSortField, taskSortDirection, taskOffset]);

  const listHeaderProps = useListHeader({
    selectedListId: selectedListId || '',
    list,
    taskSortField,
    taskSortDirection,
    taskOffset,
    onReload: reloadList,
    renameList,
    setListCategory,
    archiveList,
    completeList,
  });

  if (!selectedListId) {
    return (
      <main className="list-section-main">
        <h1>Select a list</h1>
      </main>
    );
  }

  const handleAddTaskClick = () => {
    setIsCreateTaskModalOpen(true);
  };

  const handleCreateTask = async (name: string, dueDate?: string) => {
    if (!selectedListId) {
      return;
    }

    await createListItem(selectedListId, { name, dueDate: dueDate || null });
    await reloadList();
  };

  const handleSaveTaskName = async (item: ListItemSearchResult, name: string) => {
    if (!selectedListId) {
      return;
    }

    await renameListItem(selectedListId, item.id, { name });
    await reloadList();
  };

  const handleSaveTaskDueDate = async (item: ListItemSearchResult, dueDate: string | null) => {
    if (!selectedListId) {
      return;
    }

    await setListItemDueDate(selectedListId, item.id, { dueDate });
    await reloadList();
  };

  const handleToggleTaskCompletion = async (item: ListItemSearchResult) => {
    if (!selectedListId) {
      return;
    }

    await toggleListItemCompletion(selectedListId, item.id);
    await reloadList();
  };

  const handleDeleteTask = async (item: ListItemSearchResult) => {
    if (!selectedListId) {
      return;
    }

    await deleteListItem(selectedListId, item.id);
    await reloadList();
  };

  const handleTaskSortChange = (field: TaskSortField) => {
    setTaskOffset(0);

    if (taskSortField === field) {
      setTaskSortDirection(taskSortDirection === 'asc' ? 'desc' : 'asc');
      return;
    }

    setTaskSortField(field);
    setTaskSortDirection('asc');
  };

  const handlePreviousTaskPageClick = () => {
    setTaskOffset((currentOffset) => Math.max(0, currentOffset - TASK_PAGE_SIZE));
  };

  const handleNextTaskPageClick = () => {
    setTaskOffset((currentOffset) => currentOffset + TASK_PAGE_SIZE);
  };

  return (
    <main className="list-section-main">
      {isLoadingList && <h1>Loading list...</h1>}
      {listError && <p>{listError}</p>}
      {!isLoadingList && !listError && list && (
        <>
          <ListHeader
            list={list}
            listCounts={listCounts}
            listNameEditState={listHeaderProps.listNameEditState}
            listCategoryEditState={listHeaderProps.listCategoryEditState}
            onEditListClick={listHeaderProps.handleEditListClick}
            onListNameChange={listHeaderProps.handleListNameChange}
            onDiscardListName={listHeaderProps.handleDiscardListName}
            onSaveListName={listHeaderProps.handleSaveListName}
            onEditCategoryClick={listHeaderProps.handleEditCategoryClick}
            onCategoryChange={listHeaderProps.handleCategoryChange}
            onDiscardCategory={listHeaderProps.handleDiscardCategory}
            onSaveCategory={listHeaderProps.handleSaveCategory}
            onCompletionStatusChange={listHeaderProps.handleCompletionStatusChange}
            onArchiveStatusChange={listHeaderProps.handleArchiveStatusChange}
          />
          <TaskList
            items={list.items}
            taskOffset={taskOffset}
            taskPageSize={TASK_PAGE_SIZE}
            taskSortField={taskSortField}
            taskSortDirection={taskSortDirection}
            totalTaskCount={totalTaskCount}
            isCreateTaskModalOpen={isCreateTaskModalOpen}
            onAddTaskClick={handleAddTaskClick}
            onCloseCreateModal={() => setIsCreateTaskModalOpen(false)}
            onCreateTask={handleCreateTask}
            onSaveTaskName={handleSaveTaskName}
            onSaveTaskDueDate={handleSaveTaskDueDate}
            onToggleTaskCompletion={handleToggleTaskCompletion}
            onDeleteTask={handleDeleteTask}
            onTaskSortChange={handleTaskSortChange}
            onPreviousTaskPage={handlePreviousTaskPageClick}
            onNextTaskPage={handleNextTaskPageClick}
          />
        </>
      )}
    </main>
  );
};

export default ListSection;
