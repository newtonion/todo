import { faCheck } from '@fortawesome/free-solid-svg-icons/faCheck';
import { faArchive } from '@fortawesome/free-solid-svg-icons/faArchive';
import type { SearchListResult } from '../../api/lists/models';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

type ListSectionItemProps = {
  isSelected: boolean;
  list: SearchListResult;
  onClick: () => void;
};

const ListSectionItem = ({ isSelected, list, onClick }: ListSectionItemProps) => {
  return (
    <li className="list-section-item">
      <button
        aria-current={isSelected ? 'page' : undefined}
        className="list-section-item-button"
        type="button"
        onClick={onClick}
      >
        <span className="list-section-item-content">
          <span className="list-section-item-name">
            <span className="list-section-item-name-text">{list.name}</span>
            {list.isCompleted && (
              <span className="list-section-item-status-icon" title="Completed">
                <FontAwesomeIcon icon={faCheck} />
              </span>
            )}
            {list.archived && (
              <span className="list-section-item-status-badge" title="Archived">
                <FontAwesomeIcon icon={faArchive} />
              </span>
            )}
          </span>
        </span>
        {list.categoryName && (
          <span className="list-section-item-category">{list.categoryName}</span>
        )}
      </button>
    </li>
  );
};

export default ListSectionItem;
