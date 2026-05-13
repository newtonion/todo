import type { FormEvent } from 'react';
import type { ListItemSearchResult } from '../../api/lists/models';
import Toggle from '../shared/Toggle';
import EditableCell, { type EditState } from './EditableCell';

type TaskNameCellProps = {
  item: ListItemSearchResult;
  editState: EditState | null;
  onToggleCompletion: () => void;
  onStartEdit: () => void;
  onSave: (event: FormEvent<HTMLFormElement>) => void;
  onCancel: () => void;
  onChange: (value: string) => void;
};

const TaskNameCell = ({
  item,
  editState,
  onToggleCompletion,
  onStartEdit,
  onSave,
  onCancel,
  onChange,
}: TaskNameCellProps) => {
  return (
    <td className="task-table-name-cell">
      <Toggle
        checked={item.isCompleted}
        hideLabel
        id={`task-complete-${item.id}`}
        label={`Complete ${item.name}`}
        onChange={onToggleCompletion}
      />
      <div className="task-table-name-value">
        <EditableCell
          item={item}
          editState={editState}
          displayValue={item.name}
          inputType="text"
          ariaLabel={`Task name for ${item.name}`}
          isValid={!!editState?.value.trim()}
          onStartEdit={onStartEdit}
          onSave={onSave}
          onCancel={onCancel}
          onChange={onChange}
        />
      </div>
    </td>
  );
};

export default TaskNameCell;
