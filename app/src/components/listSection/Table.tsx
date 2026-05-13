import { Fragment, useCallback, useMemo, useState } from 'react';
import { faMinus, faPlus } from '@fortawesome/free-solid-svg-icons';
import type { ListItemSearchResult } from '../../api/lists/models';
import { isPast, toDateInputValue } from '../../utils/dateUtils';
import TaskCreateModal from '../sidebar/TaskCreateModal';
import { IconButton } from '../shared/IconButtons';
import Pagination from '../shared/Pagination';
import type { EditState } from './EditableCell';
import SortableHeader from './SortableHeader';
import TaskTableRow from './TaskTableRow';
import './Table.css';

export type TaskSortField = 'name' | 'dueDate' | 'status';
export type SortDirection = 'asc' | 'desc';

type TableProps = {
  items: ListItemSearchResult[];
  itemOffset: number;
  pageSize: number;
  sortDirection: SortDirection;
  sortField: TaskSortField;
  totalItemCount: number;
  onNextPage: () => void;
  onPreviousPage: () => void;
  onSaveDueDate: (item: ListItemSearchResult, dueDate: string | null) => void | Promise<void>;
  onSaveName: (item: ListItemSearchResult, name: string) => void | Promise<void>;
  onSortChange: (field: TaskSortField) => void;
  onToggleCompletion: (item: ListItemSearchResult) => void | Promise<void>;
  onDeleteItem: (item: ListItemSearchResult) => void | Promise<void>;
  onCreateChildItem: (item: ListItemSearchResult, name: string, dueDate?: string) => void | Promise<void>;
  onLoadChildItems: (item: ListItemSearchResult) => Promise<ListItemSearchResult[]>;
};

const getTaskStatus = (item: ListItemSearchResult) => {
  if (item.isCompleted) {
    return 'Completed';
  }

  if (isPast(item.dueDate)) {
    return 'Overdue';
  }

  return 'Pending';
};

const Table = ({
  items,
  itemOffset,
  pageSize,
  sortDirection,
  sortField,
  totalItemCount,
  onNextPage,
  onPreviousPage,
  onSaveDueDate,
  onSaveName,
  onSortChange,
  onToggleCompletion,
  onDeleteItem,
  onCreateChildItem,
  onLoadChildItems,
}: TableProps) => {
  const [nameEditState, setNameEditState] = useState<EditState | null>(null);
  const [dueDateEditState, setDueDateEditState] = useState<EditState | null>(null);
  const [expandedItemIds, setExpandedItemIds] = useState<Set<string>>(() => new Set());
  const [loadingChildrenIds, setLoadingChildrenIds] = useState<Set<string>>(() => new Set());
  const [childCreationParent, setChildCreationParent] = useState<ListItemSearchResult | null>(null);
  const [childItemsByParent, setChildItemsByParent] = useState<Record<string, ListItemSearchResult[]>>({});

  const visibleItemIds = useMemo(() => new Set(items.map((item) => item.id)), [items]);
  const visibleExpandedItemIds = useMemo(() => new Set(
    [...expandedItemIds].filter((itemId) => visibleItemIds.has(itemId))
  ), [expandedItemIds, visibleItemIds]);
  const visibleChildItemsByParent = useMemo(() => Object.fromEntries(
    Object.entries(childItemsByParent).filter(([parentId]) => visibleItemIds.has(parentId))
  ), [childItemsByParent, visibleItemIds]);
  const expandableItems = useMemo(() => items.filter((item) => item.totalChildren > 0), [items]);
  const hasChildItemsInTable = expandableItems.length > 0;
  const areAllExpandableItemsExpanded = expandableItems.length > 0
    && expandableItems.every((item) => visibleExpandedItemIds.has(item.id));

  const updateCachedChildItem = useCallback((itemId: string, updates: Partial<ListItemSearchResult>) => {
    setChildItemsByParent((current) => Object.fromEntries(
      Object.entries(current).map(([parentId, children]) => [
        parentId,
        children.map((child) => child.id === itemId ? { ...child, ...updates } : child),
      ])
    ));
  }, []);

  const removeCachedChildItem = useCallback((itemId: string) => {
    setChildItemsByParent((current) => Object.fromEntries(
      Object.entries(current).map(([parentId, children]) => [
        parentId,
        children.filter((child) => child.id !== itemId),
      ])
    ));
  }, []);

  const handleStartNameEdit = useCallback((item: ListItemSearchResult) => {
    setNameEditState({
      itemId: item.id,
      value: item.name,
      isSaving: false,
    });
  }, []);

  const handleSaveName = useCallback(async (event: React.FormEvent<HTMLFormElement>, item: ListItemSearchResult) => {
    event.preventDefault();

    if (!nameEditState || nameEditState.itemId !== item.id) {
      return;
    }

    const trimmedName = nameEditState.value.trim();

    if (!trimmedName) {
      return;
    }

    setNameEditState({
      ...nameEditState,
      value: trimmedName,
      isSaving: true,
    });

    try {
      await onSaveName(item, trimmedName);
      updateCachedChildItem(item.id, { name: trimmedName });
      setNameEditState(null);
    } catch (error) {
      console.error('Failed to save task name:', error);
      setNameEditState((current) => (
        current?.itemId === item.id
          ? { ...current, isSaving: false }
          : current
      ));
    }
  }, [nameEditState, onSaveName, updateCachedChildItem]);

  const handleStartDueDateEdit = useCallback((item: ListItemSearchResult) => {
    setDueDateEditState({
      itemId: item.id,
      value: toDateInputValue(item.dueDate),
      isSaving: false,
    });
  }, []);

  const handleSaveDueDate = useCallback(async (event: React.FormEvent<HTMLFormElement>, item: ListItemSearchResult) => {
    event.preventDefault();

    if (!dueDateEditState || dueDateEditState.itemId !== item.id) {
      return;
    }

    setDueDateEditState({
      ...dueDateEditState,
      isSaving: true,
    });

    try {
      await onSaveDueDate(item, dueDateEditState.value || null);
      updateCachedChildItem(item.id, { dueDate: dueDateEditState.value || null });
      setDueDateEditState(null);
    } catch (error) {
      console.error('Failed to save due date:', error);
      setDueDateEditState((current) => (
        current?.itemId === item.id
          ? { ...current, isSaving: false }
          : current
      ));
    }
  }, [dueDateEditState, onSaveDueDate, updateCachedChildItem]);

  const handleCancelNameEdit = useCallback(() => {
    setNameEditState(null);
  }, []);

  const handleCancelDueDateEdit = useCallback(() => {
    setDueDateEditState(null);
  }, []);

  const handleNameChange = useCallback((value: string) => {
    setNameEditState((current) => current ? { ...current, value } : null);
  }, []);

  const handleDueDateChange = useCallback((value: string) => {
    setDueDateEditState((current) => current ? { ...current, value } : null);
  }, []);

  const handleDeleteClick = useCallback(async (item: ListItemSearchResult) => {
    const shouldDelete = window.confirm(`Delete "${item.name}"?`);

    if (!shouldDelete) {
      return;
    }

    try {
      await onDeleteItem(item);
      removeCachedChildItem(item.id);
    } catch (error) {
      console.error('Failed to delete task:', error);
    }
  }, [onDeleteItem, removeCachedChildItem]);

  const handleToggleCompletionClick = useCallback(async (item: ListItemSearchResult) => {
    try {
      await onToggleCompletion(item);
      updateCachedChildItem(item.id, { isCompleted: !item.isCompleted });
    } catch (error) {
      console.error('Failed to update task completion:', error);
    }
  }, [onToggleCompletion, updateCachedChildItem]);

  const handleExpansionClick = useCallback(async (item: ListItemSearchResult) => {
    if (item.totalChildren <= 0) {
      return;
    }

    if (expandedItemIds.has(item.id)) {
      setExpandedItemIds((current) => {
        const next = new Set(current);
        next.delete(item.id);
        return next;
      });
      return;
    }

    setExpandedItemIds((current) => new Set(current).add(item.id));

    if (childItemsByParent[item.id]) {
      return;
    }

    setLoadingChildrenIds((current) => new Set(current).add(item.id));

    try {
      const childItems = await onLoadChildItems(item);
      setChildItemsByParent((current) => ({
        ...current,
        [item.id]: childItems,
      }));
    } catch (error) {
      console.error('Failed to load child tasks:', error);
      setExpandedItemIds((current) => {
        const next = new Set(current);
        next.delete(item.id);
        return next;
      });
    } finally {
      setLoadingChildrenIds((current) => {
        const next = new Set(current);
        next.delete(item.id);
        return next;
      });
    }
  }, [childItemsByParent, expandedItemIds, onLoadChildItems]);

  const handleToggleAllExpansion = useCallback(async () => {
    if (areAllExpandableItemsExpanded) {
      setExpandedItemIds(new Set());
      return;
    }

    const expandableItemIds = expandableItems.map((item) => item.id);
    const itemsToLoad = expandableItems.filter((item) => !childItemsByParent[item.id]);

    setExpandedItemIds(new Set(expandableItemIds));

    if (itemsToLoad.length === 0) {
      return;
    }

    setLoadingChildrenIds((current) => new Set([
      ...current,
      ...itemsToLoad.map((item) => item.id),
    ]));

    const childResults = await Promise.all(
      itemsToLoad.map(async (item) => {
        try {
          return [item.id, await onLoadChildItems(item)] as const;
        } catch (error) {
          console.error('Failed to load child tasks:', error);
          return [item.id, null] as const;
        }
      })
    );

    setChildItemsByParent((current) => ({
      ...current,
      ...Object.fromEntries(
        childResults
          .filter((result): result is readonly [string, ListItemSearchResult[]] => result[1] !== null)
      ),
    }));
    setExpandedItemIds((current) => {
      const failedItemIds = new Set(childResults
        .filter(([, childItems]) => childItems === null)
        .map(([itemId]) => itemId));

      return new Set([...current].filter((itemId) => !failedItemIds.has(itemId)));
    });
    setLoadingChildrenIds((current) => {
      const loadedItemIds = new Set(itemsToLoad.map((item) => item.id));
      return new Set([...current].filter((itemId) => !loadedItemIds.has(itemId)));
    });
  }, [areAllExpandableItemsExpanded, childItemsByParent, expandableItems, onLoadChildItems]);

  const handleCreateChildItem = useCallback(async (name: string, dueDate?: string) => {
    if (!childCreationParent) {
      return;
    }

    await onCreateChildItem(childCreationParent, name, dueDate);

    const childItems = await onLoadChildItems(childCreationParent);
    setChildItemsByParent((current) => ({
      ...current,
      [childCreationParent.id]: childItems,
    }));
    setExpandedItemIds((current) => new Set(current).add(childCreationParent.id));
  }, [childCreationParent, onCreateChildItem, onLoadChildItems]);

  const renderTaskRow = (item: ListItemSearchResult, isChild = false) => {
    const status = getTaskStatus(item);
    const isExpanded = visibleExpandedItemIds.has(item.id);
    const isLoadingChildren = loadingChildrenIds.has(item.id);

    return (
      <TaskTableRow
        key={item.id}
        item={item}
        isChild={isChild}
        status={status}
        isExpanded={isExpanded}
        isLoadingChildren={isLoadingChildren}
        nameEditState={nameEditState}
        dueDateEditState={dueDateEditState}
        onToggleExpansion={() => handleExpansionClick(item)}
        onToggleCompletion={() => handleToggleCompletionClick(item)}
        onStartNameEdit={() => handleStartNameEdit(item)}
        onSaveName={(event) => handleSaveName(event, item)}
        onCancelNameEdit={handleCancelNameEdit}
        onNameChange={handleNameChange}
        onStartDueDateEdit={() => handleStartDueDateEdit(item)}
        onSaveDueDate={(event) => handleSaveDueDate(event, item)}
        onCancelDueDateEdit={handleCancelDueDateEdit}
        onDueDateChange={handleDueDateChange}
        onAddSubtask={() => setChildCreationParent(item)}
        onDelete={() => handleDeleteClick(item)}
      />
    );
  };

  return (
    <>
      <Pagination
        ariaLabel="Tasks pagination"
        className="task-table-pagination"
        itemCount={items.length}
        offset={itemOffset}
        pageSize={pageSize}
        totalCount={totalItemCount}
        onNextPage={onNextPage}
        onPreviousPage={onPreviousPage}
      />
      <table className="task-table">
        <thead>
          <tr>
            <th className="task-table-expander-header">
              {hasChildItemsInTable && (
                <IconButton
                  ariaLabel={areAllExpandableItemsExpanded ? 'Collapse all child list items' : 'Expand all child list items'}
                  className="task-table-expand-button"
                  icon={areAllExpandableItemsExpanded ? faMinus : faPlus}
                  size="small"
                  onClick={handleToggleAllExpansion}
                />
              )}
            </th>
            <SortableHeader
              field="name"
              label="Task"
              currentSortField={sortField}
              sortDirection={sortDirection}
              onSortChange={onSortChange}
            />
            <SortableHeader
              field="dueDate"
              label="Due Date"
              currentSortField={sortField}
              sortDirection={sortDirection}
              onSortChange={onSortChange}
            />
            <SortableHeader
              field="status"
              label="Status"
              currentSortField={sortField}
              sortDirection={sortDirection}
              onSortChange={onSortChange}
            />
            <th className="task-table-actions-header">Actions</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <Fragment key={item.id}>
              {renderTaskRow(item)}
              {visibleExpandedItemIds.has(item.id) && (visibleChildItemsByParent[item.id] || []).map((childItem) => renderTaskRow(childItem, true))}
            </Fragment>
          ))}
          {items.length === 0 && (
            <tr>
              <td colSpan={5}>No items in this list.</td>
            </tr>
          )}
        </tbody>
      </table>
      {childCreationParent && (
        <TaskCreateModal
          title={`New subtask for ${childCreationParent.name}`}
          submitLabel="Create subtask"
          onClose={() => setChildCreationParent(null)}
          onCreate={handleCreateChildItem}
        />
      )}
    </>
  );
};

export default Table;
