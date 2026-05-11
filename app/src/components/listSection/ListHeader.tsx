import type { CountListResult, GetListResult } from '../../api/lists/models';
import CategoryDropdown from '../shared/CategoryDropdown';
import InlineEditable, { type EditState } from '../shared/InlineEditable';
import { DiscardIconButton, EditIconButton, SaveIconButton } from '../shared/IconButtons';
import Toggle from '../shared/Toggle';
import './ListHeader.css';

type ListHeaderProps = {
  list: GetListResult;
  listCounts: CountListResult | null;
  listNameEditState: EditState | null;
  listCategoryEditState: EditState | null;
  onEditListClick: () => void;
  onListNameChange: (name: string) => void;
  onDiscardListName: () => void;
  onSaveListName: (event: React.FormEvent<HTMLFormElement>) => void;
  onEditCategoryClick: () => void;
  onCategoryChange: (categoryId: string) => void;
  onDiscardCategory: () => void;
  onSaveCategory: (event: React.FormEvent<HTMLFormElement>) => void;
  onCompletionStatusChange: () => void;
  onArchiveStatusChange: () => void;
};

const UNCATEGORIZED_LABEL = 'Uncategorized';

const ListHeader = ({
  list,
  listCounts,
  listNameEditState,
  listCategoryEditState,
  onEditListClick,
  onListNameChange,
  onDiscardListName,
  onSaveListName,
  onEditCategoryClick,
  onCategoryChange,
  onDiscardCategory,
  onSaveCategory,
  onCompletionStatusChange,
  onArchiveStatusChange,
}: ListHeaderProps) => {
  return (
    <div className="main-page-list-header">
      <div className="main-page-list-title">
        <InlineEditable
          itemId={list.id}
          editState={listNameEditState}
          displayValue={<h1>{list.name}</h1>}
          ariaLabel="List name"
          isValid={!!listNameEditState?.value.trim()}
          className="main-page-list-name-edit"
          displayClassName="main-page-list-name-row"
          onStartEdit={onEditListClick}
          onSave={onSaveListName}
          onCancel={onDiscardListName}
          onChange={onListNameChange}
          renderInput={(value, onChange, ariaLabel) => (
            <input
              aria-label={ariaLabel}
              autoFocus
              type="text"
              value={value}
              onChange={(event) => onChange(event.target.value)}
            />
          )}
        />
        {listCategoryEditState?.itemId === list.id ? (
          <form className="main-page-list-category-edit" onSubmit={onSaveCategory}>
            <CategoryDropdown
              id="list-category-edit"
              selectedLabel={list.category}
              value={listCategoryEditState.value}
              onChange={onCategoryChange}
            />
            <div className="main-page-inline-edit-actions">
              <SaveIconButton
                ariaLabel="Save category"
                disabled={listCategoryEditState.isSaving}
                size="small"
                type="submit"
              />
              <DiscardIconButton
                ariaLabel="Discard category changes"
                disabled={listCategoryEditState.isSaving}
                size="small"
                onClick={onDiscardCategory}
              />
            </div>
          </form>
        ) : (
          <div className="main-page-list-category-row">
            <p>{list.category || UNCATEGORIZED_LABEL}</p>
            <EditIconButton
              ariaLabel="Edit category"
              size="small"
              onClick={onEditCategoryClick}
            />
          </div>
        )}
        <div className="main-page-list-count-row">
          <span className="main-page-list-count">
            {listCounts?.completedItems ?? 0}/{listCounts?.totalItems ?? 0}
          </span>
        </div>
      </div>
      <div className="main-page-list-statuses">
        <Toggle
          checked={list.completed}
          id="list-completion-status"
          label="Complete"
          onChange={onCompletionStatusChange}
        />
        <Toggle
          checked={list.archived}
          id="list-archive-status"
          label="Archived"
          onChange={onArchiveStatusChange}
        />
      </div>
    </div>
  );
};

export default ListHeader;
