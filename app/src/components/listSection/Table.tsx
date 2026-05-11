import { useCallback, useState } from 'react';
import { faTrash } from '@fortawesome/free-solid-svg-icons';
import type { ListItemSearchResult } from '../../api/lists/models';
import { formatDueDate, isPast, toDateInputValue } from '../../utils/dateUtils';
import { IconButton } from '../shared/IconButtons';
import Pagination from '../shared/Pagination';
import Toggle from '../shared/Toggle';
import EditableCell, { type EditState } from './EditableCell';
import SortableHeader from './SortableHeader';
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
};

const getTaskStatus = (item: ListItemSearchResult) => {
  if (item.completed) {
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
}: TableProps) => {
  const [nameEditState, setNameEditState] = useState<EditState | null>(null);
  const [dueDateEditState, setDueDateEditState] = useState<EditState | null>(null);

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
      setNameEditState(null);
    } catch (error) {
      console.error('Failed to save task name:', error);
      setNameEditState((current) => (
        current?.itemId === item.id
          ? { ...current, isSaving: false }
          : current
      ));
    }
  }, [nameEditState, onSaveName]);

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
      setDueDateEditState(null);
    } catch (error) {
      console.error('Failed to save due date:', error);
      setDueDateEditState((current) => (
        current?.itemId === item.id
          ? { ...current, isSaving: false }
          : current
      ));
    }
  }, [dueDateEditState, onSaveDueDate]);

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
    } catch (error) {
      console.error('Failed to delete task:', error);
    }
  }, [onDeleteItem]);

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
          {items.map((item) => {
            const status = getTaskStatus(item);

            return (
              <tr key={item.id}>
                <td>
                  <EditableCell
                    item={item}
                    editState={nameEditState}
                    displayValue={item.name}
                    inputType="text"
                    ariaLabel={`Task name for ${item.name}`}
                    isValid={!!nameEditState?.value.trim()}
                    onStartEdit={() => handleStartNameEdit(item)}
                    onSave={(event) => handleSaveName(event, item)}
                    onCancel={handleCancelNameEdit}
                    onChange={handleNameChange}
                  />
                </td>
                <td>
                  <EditableCell
                    item={item}
                    editState={dueDateEditState}
                    displayValue={formatDueDate(item.dueDate)}
                    inputType="date"
                    ariaLabel={`Due date for ${item.name}`}
                    onStartEdit={() => handleStartDueDateEdit(item)}
                    onSave={(event) => handleSaveDueDate(event, item)}
                    onCancel={handleCancelDueDateEdit}
                    onChange={handleDueDateChange}
                  />
                </td>
                <td>
                  <span className={`task-table-status task-table-status-${status.toLowerCase()}`}>
                    {status}
                  </span>
                </td>
                <td>
                  <div className="task-table-actions">
                    <Toggle
                      checked={item.completed}
                      id={`task-complete-${item.id}`}
                      label="Complete"
                      onChange={() => onToggleCompletion(item)}
                    />
                    <IconButton
                      ariaLabel={`Delete ${item.name}`}
                      className="task-table-delete-button"
                      icon={faTrash}
                      size="small"
                      onClick={() => handleDeleteClick(item)}
                    />
                  </div>
                </td>
              </tr>
            );
          })}
          {items.length === 0 && (
            <tr>
              <td colSpan={4}>No items in this list.</td>
            </tr>
          )}
        </tbody>
      </table>
    </>
  );
};

export default Table;
