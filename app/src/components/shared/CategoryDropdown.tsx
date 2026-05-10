import { useCallback, useEffect, useId, useState } from 'react';
import { useCategoryApi } from '../../api/categories/useCategoryApi';

export type CategoryOption = {
  id: string;
  name: string;
};

type CategoryDropdownProps = {
  value: string;
  onChange: (categoryId: string) => void;
  id?: string;
  label?: string;
  loadCategories?: (query: string) => Promise<CategoryOption[]>;
  selectedLabel?: string;
};

const CategoryDropdown = ({
  value,
  onChange,
  id = 'category',
  label = 'Category',
  loadCategories,
  selectedLabel = '',
}: CategoryDropdownProps) => {
  const [search, setSearch] = useState(selectedLabel);
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [categories, setCategories] = useState<CategoryOption[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isOpen, setIsOpen] = useState(false);

  const { searchCategories } = useCategoryApi();
  const listboxId = useId();

  const loadOptions = useCallback(
    (query: string) => {
      if (loadCategories) {
        return loadCategories(query);
      }

      return searchCategories({
        text: query,
        orderBy: { field: 'name', ascending: true },
      }).then((results) => results.items);
    },
    [loadCategories, searchCategories]
  );

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedSearch(search.trim());
    }, 200);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [search]);

  useEffect(() => {
    let isActive = true;

    const fetchCategories = async () => {
      setIsLoading(true);
      setError(null);

      try {
        const results = await loadOptions(debouncedSearch);

        if (isActive) {
          setCategories(results);
        }
      } catch (err) {
        if (isActive) {
          setError(err instanceof Error ? err.message : 'Failed to load categories');
          setCategories([]);
        }
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    };

    fetchCategories();

    return () => {
      isActive = false;
    };
  }, [debouncedSearch, loadOptions]);

  const handleSearchChange = (nextSearch: string) => {
    setSearch(nextSearch);
    setIsOpen(true);

    if (value) {
      onChange('');
    }
  };

  const handleSelectCategory = (category: CategoryOption) => {
    setSearch(category.name);
    setIsOpen(false);
    onChange(category.id);
  };

  const handleBlur = () => {
    window.setTimeout(() => {
      setIsOpen(false);
    }, 100);
  };

  return (
    <div className="category-dropdown">
      <label htmlFor={id}>{label}</label>
      <div className="category-combobox">
        <input
          aria-autocomplete="list"
          aria-controls={listboxId}
          aria-expanded={isOpen}
          aria-label={`Filter ${label.toLowerCase()}`}
          autoComplete="off"
          id={id}
          placeholder="Type to filter..."
          role="combobox"
          type="text"
          value={search}
          onBlur={handleBlur}
          onChange={(event) => handleSearchChange(event.target.value)}
          onFocus={() => setIsOpen(true)}
        />
        {isOpen && (
          <div className="category-combobox-menu" id={listboxId} role="listbox">
            {isLoading && <div className="category-combobox-status">Loading...</div>}
            {!isLoading && categories.length === 0 && (
              <div className="category-combobox-status">No categories found</div>
            )}
            {!isLoading &&
              categories.map((category) => (
                <button
                  aria-selected={category.id === value}
                  className="category-combobox-option"
                  key={category.id}
                  role="option"
                  type="button"
                  onClick={() => handleSelectCategory(category)}
                >
                  {category.name}
                </button>
              ))}
          </div>
        )}
      </div>
      {error && <p className="category-dropdown-error">{error}</p>}
    </div>
  );
};

export default CategoryDropdown;
