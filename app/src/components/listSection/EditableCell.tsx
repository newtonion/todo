import type { ListItemSearchResult } from '../../api/lists/models';
import InlineEditable, { type EditState } from '../shared/InlineEditable';

type EditableCellProps = {
  item: ListItemSearchResult;
  editState: EditState | null;
  displayValue: string;
  inputType: 'text' | 'date';
  placeholder?: string;
  ariaLabel: string;
  isValid?: boolean;
  onStartEdit: () => void;
  onSave: (event: React.FormEvent<HTMLFormElement>) => void | Promise<void>;
  onCancel: () => void;
  onChange: (value: string) => void;
};

const EditableCell = ({
  item,
  editState,
  displayValue,
  inputType,
  ariaLabel,
  isValid = true,
  onStartEdit,
  onSave,
  onCancel,
  onChange,
}: EditableCellProps) => {
  return (
    <InlineEditable
      itemId={item.id}
      editState={editState}
      displayValue={displayValue}
      ariaLabel={ariaLabel}
      isValid={isValid}
      className="task-table-inline-edit"
      onStartEdit={onStartEdit}
      onSave={onSave}
      onCancel={onCancel}
      onChange={onChange}
      renderInput={(value, onChange, ariaLabel) => (
        <input
          aria-label={ariaLabel}
          autoFocus
          type={inputType}
          value={value}
          onChange={(event) => onChange(event.target.value)}
        />
      )}
    />
  );
};

export default EditableCell;
export type { EditState };
