import { faMinus, faPlus } from '@fortawesome/free-solid-svg-icons';
import { IconButton } from '../shared/IconButtons';

type TaskExpanderCellProps = {
  itemName: string;
  hasChildren: boolean;
  isChild: boolean;
  isExpanded: boolean;
  isLoadingChildren: boolean;
  totalChildren: number;
  totalChildrenCompleted: number;
  onToggleExpansion: () => void;
};

const TaskExpanderCell = ({
  itemName,
  hasChildren,
  isChild,
  isExpanded,
  isLoadingChildren,
  totalChildren,
  totalChildrenCompleted,
  onToggleExpansion,
}: TaskExpanderCellProps) => {
  return (
    <td className="task-table-expander-cell">
      {hasChildren && (
        <IconButton
          ariaLabel={isExpanded ? `Collapse child list items for ${itemName}` : `Expand child list items for ${itemName}`}
          className="task-table-expand-button"
          disabled={isLoadingChildren}
          icon={isExpanded ? faMinus : faPlus}
          size="small"
          title={`${totalChildrenCompleted} of ${totalChildren} child items completed`}
          onClick={onToggleExpansion}
        />
      )}
      {isChild && (
        <span className="task-table-child-connector" aria-hidden="true" />
      )}
    </td>
  );
};

export default TaskExpanderCell;
