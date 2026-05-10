import { faArrowDown, faArrowUp, faArrowsUpDown } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import type { SortDirection, TaskSortField } from './Table';

type SortableHeaderProps = {
  field: TaskSortField;
  label: string;
  currentSortField: TaskSortField;
  sortDirection: SortDirection;
  onSortChange: (field: TaskSortField) => void;
};

const getSortIcon = (field: TaskSortField, currentField: TaskSortField, direction: SortDirection) => {
  if (currentField !== field) {
    return faArrowsUpDown;
  }
  return direction === 'asc' ? faArrowUp : faArrowDown;
};

const SortableHeader = ({ field, label, currentSortField, sortDirection, onSortChange }: SortableHeaderProps) => (
  <th>
    <button
      className="task-table-header-button"
      onClick={() => onSortChange(field)}
      type="button"
    >
      {label} <FontAwesomeIcon className="task-table-sort-icon" icon={getSortIcon(field, currentSortField, sortDirection)} />
    </button>
  </th>
);

export default SortableHeader;
