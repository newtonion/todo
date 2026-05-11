import { useCallback, useEffect, useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faRotateRight } from '@fortawesome/free-solid-svg-icons';
import ListAddButton from './ListAddButton';
import ListCreateModal from './ListCreateModal';
import ListSectionItem from './ListSectionItem';
import ListsSectionFilters from './ListsSectionFilters';
import Pagination from '../shared/Pagination';
import { useListApi } from '../../api/lists/useListApi';
import type { SearchListResult } from '../../api/lists/models';
import './ListsSection.css';

type ListsSectionProps = {
  selectedListId: string | null;
  onListSelect: (listId: string) => void;
};

const LIST_PAGE_SIZE = 10;

const ListsSection = ({ selectedListId, onListSelect }: ListsSectionProps) => {
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [lists, setLists] = useState<SearchListResult[]>([]);
  const [totalListCount, setTotalListCount] = useState(0);
  const [isLoadingLists, setIsLoadingLists] = useState(true);
  const [listError, setListError] = useState<string | null>(null);
  const [searchText, setSearchText] = useState('');
  const [categoryText, setCategoryText] = useState('');
  const [debouncedCategoryText, setDebouncedCategoryText] = useState('');
  const [includeCompleted, setIncludeCompleted] = useState(false);
  const [includeArchived, setIncludeArchived] = useState(false);
  const [includeOnlyUpcomingOrOverdue, setIncludeOnlyUpcomingOrOverdue] = useState(false);
  const [listOffset, setListOffset] = useState(0);
  const { createList, searchLists } = useListApi();

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedCategoryText(categoryText.trim());
    }, 200);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [categoryText]);

  const searchVisibleLists = useCallback(() => searchLists({
    text: searchText.trim(),
    orderBy: { field: 'createdOn', ascending: false },
    pageSize: LIST_PAGE_SIZE,
    offset: listOffset,
    categoryText: debouncedCategoryText || undefined,
    includeCompleted,
    includeArchived,
    onlyUpcomingOrOverdue: includeOnlyUpcomingOrOverdue,
  }), [debouncedCategoryText, includeArchived, includeCompleted, includeOnlyUpcomingOrOverdue, listOffset, searchLists, searchText]);

  useEffect(() => {
    let isActive = true;

    const loadInitialLists = async () => {
      try {
        const results = await searchVisibleLists();

        if (isActive) {
          setLists(results.items);
          setTotalListCount(results.totalCount);
        }
      } catch (err) {
        if (isActive) {
          setListError(err instanceof Error ? err.message : 'Failed to load lists');
          setLists([]);
          setTotalListCount(0);
        }
      } finally {
        if (isActive) {
          setIsLoadingLists(false);
        }
      }
    };

    loadInitialLists();

    return () => {
      isActive = false;
    };
  }, [searchVisibleLists]);

  const handleAddListClick = () => {
    setIsCreateModalOpen(true);
  };

  const handleCreateList = async (name: string, categoryId?: string) => {
    const createdList = await createList({ name, categoryId });

    const results = await searchVisibleLists();
    setLists(results.items);
    setTotalListCount(results.totalCount);
    onListSelect(createdList.id);
  };

  const handleRefreshClick = async () => {
    setIsLoadingLists(true);
    setListError(null);

    try {
      const results = await searchVisibleLists();
      setLists(results.items);
      setTotalListCount(results.totalCount);
    } catch (err) {
      setListError(err instanceof Error ? err.message : 'Failed to load lists');
      setLists([]);
      setTotalListCount(0);
    } finally {
      setIsLoadingLists(false);
    }
  };

  const resetPaging = () => {
    setListOffset(0);
  };

  const handleSearchTextChange = (nextSearchText: string) => {
    resetPaging();
    setSearchText(nextSearchText);
  };

  const handleCategoryTextChange = (nextCategoryText: string) => {
    resetPaging();
    setCategoryText(nextCategoryText);
  };

  const handleIncludeCompletedChange = (nextIncludeCompleted: boolean) => {
    resetPaging();
    setIncludeCompleted(nextIncludeCompleted);
  };

  const handleIncludeArchivedChange = (nextIncludeArchived: boolean) => {
    resetPaging();
    setIncludeArchived(nextIncludeArchived);
  };

  const handleOnlyUpcomingOrOverdueChange = (nextOnlyUpcomingOrOverdue: boolean) => {
    resetPaging();
    setIncludeOnlyUpcomingOrOverdue(nextOnlyUpcomingOrOverdue);
  };

  const handlePreviousPageClick = () => {
    setListOffset((currentOffset) => Math.max(0, currentOffset - LIST_PAGE_SIZE));
  };

  const handleNextPageClick = () => {
    setListOffset((currentOffset) => currentOffset + LIST_PAGE_SIZE);
  };

  return (
    <section>
      <div className="sidebar-section-header">
        <h2>
          Your Lists
          <span className="sidebar-section-count">{totalListCount}</span>
        </h2>
        <div className="sidebar-section-header-actions">
          <button
            aria-label="Refresh lists"
            className="list-refresh-button"
            disabled={isLoadingLists}
            type="button"
            onClick={handleRefreshClick}
          >
            <FontAwesomeIcon icon={faRotateRight} />
          </button>
          <ListAddButton onClick={handleAddListClick} />
        </div>
      </div>
      <ListsSectionFilters
        categoryText={categoryText}
        includeArchived={includeArchived}
        includeCompleted={includeCompleted}
        includeOnlyUpcomingOrOverdue={includeOnlyUpcomingOrOverdue}
        searchText={searchText}
        onCategoryTextChange={handleCategoryTextChange}
        onIncludeArchivedChange={handleIncludeArchivedChange}
        onIncludeCompletedChange={handleIncludeCompletedChange}
        onOnlyUpcomingOrOverdueChange={handleOnlyUpcomingOrOverdueChange}
        onSearchTextChange={handleSearchTextChange}
      />
      {isLoadingLists && <p>Loading lists...</p>}
      {listError && <p>{listError}</p>}
      {!isLoadingLists && !listError && (
        <Pagination
          ariaLabel="Lists pagination"
          className="lists-pagination"
          itemCount={lists.length}
          offset={listOffset}
          pageSize={LIST_PAGE_SIZE}
          totalCount={totalListCount}
          onNextPage={handleNextPageClick}
          onPreviousPage={handlePreviousPageClick}
        />
      )}
      {!isLoadingLists && !listError && lists.length > 0 && (
        <ul className="lists-list">
          {lists.map((list) => (
            <ListSectionItem
              key={list.id}
              isSelected={list.id === selectedListId}
              list={list}
              onClick={() => onListSelect(list.id)}
            />
          ))}
        </ul>
      )}
      {!isLoadingLists && !listError && lists.length === 0 && (
        <p>No lists found. Create your first list!</p>
      )}

      {isCreateModalOpen && (
        <ListCreateModal
          onClose={() => setIsCreateModalOpen(false)}
          onCreate={handleCreateList}
        />
      )}
    </section>
  );
};

export default ListsSection;
