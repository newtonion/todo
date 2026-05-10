import { faChevronLeft, faChevronRight } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import './Pagination.css';

type PaginationProps = {
  itemCount: number;
  offset: number;
  pageSize: number;
  totalCount: number;
  ariaLabel?: string;
  className?: string;
  onNextPage: () => void;
  onPreviousPage: () => void;
};

const Pagination = ({
  ariaLabel = 'Pagination',
  className = '',
  itemCount,
  offset,
  pageSize,
  totalCount,
  onNextPage,
  onPreviousPage,
}: PaginationProps) => {
  if (totalCount <= pageSize) {
    return null;
  }

  const hasPreviousPage = offset > 0;
  const hasNextPage = offset + pageSize < totalCount;

  return (
    <nav
      aria-label={ariaLabel}
      className={`pagination${className ? ` ${className}` : ''}`}
    >
      <button
        aria-label="Previous page"
        className="pagination-button"
        disabled={!hasPreviousPage}
        type="button"
        onClick={onPreviousPage}
      >
        <FontAwesomeIcon icon={faChevronLeft} />
      </button>
      <span className="pagination-status">
        {offset + 1}-{Math.min(offset + itemCount, totalCount)}
      </span>
      <button
        aria-label="Next page"
        className="pagination-button"
        disabled={!hasNextPage}
        type="button"
        onClick={onNextPage}
      >
        <FontAwesomeIcon icon={faChevronRight} />
      </button>
    </nav>
  );
};

export default Pagination;
