import Modal from '../shared/Modal';
import './CreateModal.css';

type FormField = {
  id: string;
  label: string;
  type: 'text' | 'date';
  required?: boolean;
  autoFocus?: boolean;
  value: string;
  onChange: (value: string) => void;
  renderInput?: (id: string, value: string, onChange: (value: string) => void) => React.ReactNode;
};

type CreateModalProps = {
  title: string;
  fields: FormField[];
  submitLabel?: string;
  onClose: () => void;
  onSubmit: () => void | Promise<void>;
  isValid: boolean;
  className?: string;
};

const CreateModal = ({
  title,
  fields,
  submitLabel = 'Create',
  onClose,
  onSubmit,
  isValid,
  className = '',
}: CreateModalProps) => {
  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!isValid) {
      return;
    }

    await onSubmit();
    onClose();
  };

  return (
    <Modal onClose={onClose}>
      <form
        className={className}
        onClick={(event) => event.stopPropagation()}
        onSubmit={handleSubmit}
      >
        <h3>{title}</h3>
        {fields.map((field) => (
          <div key={field.id}>
            <label htmlFor={field.id}>{field.label}</label>
            {field.renderInput ? (
              field.renderInput(field.id, field.value, field.onChange)
            ) : (
              <input
                autoFocus={field.autoFocus}
                id={field.id}
                type={field.type}
                value={field.value}
                onChange={(event) => field.onChange(event.target.value)}
              />
            )}
          </div>
        ))}
        <div className="modal-actions">
          <button type="button" onClick={onClose}>
            Cancel
          </button>
          <button type="submit" disabled={!isValid}>
            {submitLabel}
          </button>
        </div>
      </form>
    </Modal>
  );
};

export default CreateModal;
export type { FormField };
