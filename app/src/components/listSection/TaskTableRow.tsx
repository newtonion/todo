import type { FormEvent } from 'react';
import type { ListItemSearchResult } from '../../api/lists/models';
import { formatDueDate } from '../../utils/dateUtils';
import EditableCell, { type EditState } from './EditableCell';
import TaskActions from './TaskActions';
import TaskExpanderCell from './TaskExpanderCell';
import TaskNameCell from './TaskNameCell';

type TaskTableRowProps = {
  item: ListItemSearchResult;
  isChild?: boolean;
  status: string;
  isExpanded: boolean;
  isLoadingChildren: boolean;
  nameEditState: EditState | null;
  dueDateEditState: EditState | null;
  onToggleExpansion: () => void;
  onToggleCompletion: () => void;
  onStartNameEdit: () => void;
  onSaveName: (event: FormEvent<HTMLFormElement>) => void;
  onCancelNameEdit: () => void;
  onNameChange: (value: string) => void;
  onStartDueDateEdit: () => void;
  onSaveDueDate: (event: FormEvent<HTMLFormElement>) => void;
  onCancelDueDateEdit: () => void;
  onDueDateChange: (value: string) => void;
  onAddSubtask: () => void;
  onDelete: () => void;
};

const TaskTableRow = ({
  item,
  isChild = false,
  status,
  isExpanded,
  isLoadingChildren,
  nameEditState,
  dueDateEditState,
  onToggleExpansion,
  onToggleCompletion,
  onStartNameEdit,
  onSaveName,
  onCancelNameEdit,
  onNameChange,
  onStartDueDateEdit,
  onSaveDueDate,
  onCancelDueDateEdit,
  onDueDateChange,
  onAddSubtask,
  onDelete,
}: TaskTableRowProps) => {
  const hasChildren = !isChild && item.totalChildren > 0;

  return (
    <tr className={isChild ? 'task-table-row-child' : undefined}>
      <TaskExpanderCell
        itemName={item.name}
        hasChildren={hasChildren}
        isChild={isChild}
        isExpanded={isExpanded}
        isLoadingChildren={isLoadingChildren}
        totalChildren={item.totalChildren}
        totalChildrenCompleted={item.totalChildrenCompleted}
        onToggleExpansion={onToggleExpansion}
      />
      <TaskNameCell
        item={item}
        editState={nameEditState}
        onToggleCompletion={onToggleCompletion}
        onStartEdit={onStartNameEdit}
        onSave={onSaveName}
        onCancel={onCancelNameEdit}
        onChange={onNameChange}
      />
      <td>
        <EditableCell
          item={item}
          editState={dueDateEditState}
          displayValue={formatDueDate(item.dueDate)}
          inputType="date"
          ariaLabel={`Due date for ${item.name}`}
          onStartEdit={onStartDueDateEdit}
          onSave={onSaveDueDate}
          onCancel={onCancelDueDateEdit}
          onChange={onDueDateChange}
        />
      </td>
      <td>
        <span className={`task-table-status task-table-status-${status.toLowerCase()}`}>
          {status}
        </span>
      </td>
      <TaskActions
        itemName={item.name}
        isChild={isChild}
        onAddSubtask={onAddSubtask}
        onDelete={onDelete}
      />
    </tr>
  );
};

export default TaskTableRow;
