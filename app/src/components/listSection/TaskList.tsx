import type { ListItemSearchResult } from '../../api/lists/models';
import { AddIconButton } from '../shared/IconButtons';
import Table, { type SortDirection, type TaskSortField } from './Table';
import TaskCreateModal from '../sidebar/TaskCreateModal';

type TaskListProps = {
  items: ListItemSearchResult[];
  taskOffset: number;
  taskPageSize: number;
  taskSortField: TaskSortField;
  taskSortDirection: SortDirection;
  totalTaskCount: number;
  isCreateTaskModalOpen: boolean;
  onAddTaskClick: () => void;
  onCloseCreateModal: () => void;
  onCreateTask: (name: string, dueDate?: string) => Promise<void>;
  onSaveTaskName: (item: ListItemSearchResult, name: string) => Promise<void>;
  onSaveTaskDueDate: (item: ListItemSearchResult, dueDate: string | null) => Promise<void>;
  onToggleTaskCompletion: (item: ListItemSearchResult) => Promise<void>;
  onTaskSortChange: (field: TaskSortField) => void;
  onPreviousTaskPage: () => void;
  onNextTaskPage: () => void;
};

const TaskList = ({
  items,
  taskOffset,
  taskPageSize,
  taskSortField,
  taskSortDirection,
  totalTaskCount,
  isCreateTaskModalOpen,
  onAddTaskClick,
  onCloseCreateModal,
  onCreateTask,
  onSaveTaskName,
  onSaveTaskDueDate,
  onToggleTaskCompletion,
  onTaskSortChange,
  onPreviousTaskPage,
  onNextTaskPage,
}: TaskListProps) => {
  return (
    <>
      <div className="main-page-table-header">
        <AddIconButton
          ariaLabel="Add task"
          onClick={onAddTaskClick}
        />
      </div>
      <Table
        items={items}
        itemOffset={taskOffset}
        pageSize={taskPageSize}
        sortDirection={taskSortDirection}
        sortField={taskSortField}
        totalItemCount={totalTaskCount}
        onNextPage={onNextTaskPage}
        onPreviousPage={onPreviousTaskPage}
        onSaveDueDate={onSaveTaskDueDate}
        onSaveName={onSaveTaskName}
        onSortChange={onTaskSortChange}
        onToggleCompletion={onToggleTaskCompletion}
      />
      {isCreateTaskModalOpen && (
        <TaskCreateModal
          onClose={onCloseCreateModal}
          onCreate={onCreateTask}
        />
      )}
    </>
  );
};

export default TaskList;
