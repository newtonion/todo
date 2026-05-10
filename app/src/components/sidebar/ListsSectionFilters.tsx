import { useId, useState } from 'react';
import Toggle from '../shared/Toggle';

type ListsSectionFiltersProps = {
  searchText: string;
  categoryText: string;
  includeCompleted: boolean;
  includeArchived: boolean;
  onSearchTextChange: (searchText: string) => void;
  onCategoryTextChange: (categoryText: string) => void;
  onIncludeCompletedChange: (includeCompleted: boolean) => void;
  onIncludeArchivedChange: (includeArchived: boolean) => void;
};

const ListsSectionFilters = ({
  searchText,
  categoryText,
  includeCompleted,
  includeArchived,
  onSearchTextChange,
  onCategoryTextChange,
  onIncludeCompletedChange,
  onIncludeArchivedChange,
}: ListsSectionFiltersProps) => {
  const [isExpanded, setIsExpanded] = useState(false);
  const filtersContentId = useId();

  return (
    <div className="lists-section-filters">
      <button
        aria-controls={filtersContentId}
        aria-expanded={isExpanded}
        className="lists-section-filters-toggle"
        type="button"
        onClick={() => setIsExpanded((current) => !current)}
      >
        <span>Search</span>
        <span aria-hidden="true">{isExpanded ? '-' : '+'}</span>
      </button>
      {isExpanded && (
        <div className="lists-section-filters-content" id={filtersContentId}>
          <label className="lists-section-filter-search" htmlFor="list-search-text">
            <span>Search text</span>
            <input
              id="list-search-text"
              placeholder="Filter lists..."
              type="text"
              value={searchText}
              onChange={(event) => onSearchTextChange(event.target.value)}
            />
          </label>
          <label className="lists-section-filter-search" htmlFor="list-search-category">
            <span>Category text</span>
            <input
              id="list-search-category"
              placeholder="Filter by category..."
              type="text"
              value={categoryText}
              onChange={(event) => onCategoryTextChange(event.target.value)}
            />
          </label>
          <div className="list-search-toggles">
            <Toggle
              checked={includeCompleted}
              id="include-completed-lists"
              label="Show Completed"
              onChange={onIncludeCompletedChange}
            />
            <Toggle
              checked={includeArchived}
              id="include-archived-lists"
              label="Show Archived"
              onChange={onIncludeArchivedChange}
            />
          </div>
        </div>
      )}
    </div>
  );
};

export default ListsSectionFilters;
