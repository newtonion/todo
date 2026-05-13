import { faTrash } from '@fortawesome/free-solid-svg-icons';
import { AddIconButton, IconButton } from '../shared/IconButtons';

type TaskActionsProps = {
  itemName: string;
  isChild: boolean;
  onAddSubtask: () => void;
  onDelete: () => void;
};

const TaskActions = ({
  itemName,
  isChild,
  onAddSubtask,
  onDelete,
}: TaskActionsProps) => {
  return (
    <td>
      <div className="task-table-actions">
        {!isChild && (
          <AddIconButton
            ariaLabel={`Add subtask to ${itemName}`}
            className="task-table-add-subtask-button"
            size="small"
            title={`Add subtask to ${itemName}`}
            onClick={onAddSubtask}
          />
        )}
        <IconButton
          ariaLabel={`Delete ${itemName}`}
          className="task-table-delete-button"
          icon={faTrash}
          size="small"
          onClick={onDelete}
        />
      </div>
    </td>
  );
};

export default TaskActions;
