import type { ListItemSearchResult } from '../../api/lists/models';
import { faFileLines } from '@fortawesome/free-solid-svg-icons';
import { AddIconButton, IconButton } from '../shared/IconButtons';
import Table, { type SortDirection, type TaskSortField } from './Table';
import TaskCreateModal from '../sidebar/TaskCreateModal';
import './TaskList.css';

type TaskListProps = {
  items: ListItemSearchResult[];
  taskOffset: number;
  taskPageSize: number;
  taskSortField: TaskSortField;
  taskSortDirection: SortDirection;
  totalTaskCount: number;
  isCreateTaskModalOpen: boolean;
  isPrintingList: boolean;
  onAddTaskClick: () => void;
  onPrintList: () => void | Promise<void>;
  onCloseCreateModal: () => void;
  onCreateTask: (name: string, dueDate?: string) => Promise<void>;
  onSaveTaskName: (item: ListItemSearchResult, name: string) => Promise<void>;
  onSaveTaskDueDate: (item: ListItemSearchResult, dueDate: string | null) => Promise<void>;
  onToggleTaskCompletion: (item: ListItemSearchResult) => Promise<void>;
  onDeleteTask: (item: ListItemSearchResult) => Promise<void>;
  onCreateSubtask: (parentItem: ListItemSearchResult, name: string, dueDate?: string) => Promise<void>;
  onLoadChildTasks: (item: ListItemSearchResult) => Promise<ListItemSearchResult[]>;
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
  isPrintingList,
  onAddTaskClick,
  onPrintList,
  onCloseCreateModal,
  onCreateTask,
  onSaveTaskName,
  onSaveTaskDueDate,
  onToggleTaskCompletion,
  onDeleteTask,
  onCreateSubtask,
  onLoadChildTasks,
  onTaskSortChange,
  onPreviousTaskPage,
  onNextTaskPage,
}: TaskListProps) => {
  return (
    <>
      <div className="main-page-table-header">
        <IconButton
          ariaLabel="Create printable task sheet"
          className="task-list-print-button"
          disabled={isPrintingList}
          icon={faFileLines}
          title="Create printable task sheet"
          onClick={onPrintList}
        />
        <div className="main-page-table-header-actions">
          <AddIconButton
            ariaLabel="Add a list item"
            onClick={onAddTaskClick}
          />
        </div>
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
        onDeleteItem={onDeleteTask}
        onCreateChildItem={onCreateSubtask}
        onLoadChildItems={onLoadChildTasks}
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
