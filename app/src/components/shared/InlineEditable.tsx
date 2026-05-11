import type { ReactNode } from 'react';
import { DiscardIconButton, EditIconButton, SaveIconButton } from './IconButtons';

type EditState = {
  itemId: string;
  value: string;
  isSaving: boolean;
};

type InlineEditableProps = {
  itemId: string;
  editState: EditState | null;
  displayValue: string | ReactNode;
  ariaLabel: string;
  isValid?: boolean;
  className?: string;
  displayClassName?: string;
  onStartEdit: () => void;
  onSave: (event: React.FormEvent<HTMLFormElement>) => void | Promise<void>;
  onCancel: () => void;
  onChange: (value: string) => void;
  renderInput: (value: string, onChange: (value: string) => void, ariaLabel: string) => ReactNode;
};

const InlineEditable = ({
  itemId,
  editState,
  displayValue,
  ariaLabel,
  isValid = true,
  className = '',
  displayClassName = 'task-table-cell-action',
  onStartEdit,
  onSave,
  onCancel,
  onChange,
  renderInput,
}: InlineEditableProps) => {
  const isEditing = editState?.itemId === itemId;

  if (isEditing && editState) {
    return (
      <form className={className} onSubmit={onSave}>
        {renderInput(editState.value, onChange, ariaLabel)}
        <SaveIconButton
          ariaLabel={`Save ${ariaLabel}`}
          disabled={!isValid || editState.isSaving}
          size="small"
          type="submit"
        />
        <DiscardIconButton
          ariaLabel={`Discard changes for ${ariaLabel}`}
          disabled={editState.isSaving}
          size="small"
          onClick={onCancel}
        />
      </form>
    );
  }

  return (
    <div className={displayClassName}>
      <span>{displayValue}</span>
      <EditIconButton
        ariaLabel={`Edit ${ariaLabel}`}
        size="small"
        onClick={onStartEdit}
      />
    </div>
  );
};

export default InlineEditable;
export type { EditState };
