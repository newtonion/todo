import { useState } from 'react';
import CategoryDropdown from '../shared/CategoryDropdown';
import CreateModal from '../shared/CreateModal';

type ListCreateModalProps = {
  onClose: () => void;
  onCreate: (name: string, categoryId?: string) => void | Promise<void>;
};

const ListCreateModal = ({ onClose, onCreate }: ListCreateModalProps) => {
  const [listName, setListName] = useState('');
  const [category, setCategory] = useState('');

  const trimmedName = listName.trim();

  return (
    <CreateModal
      title="New list"
      className="list-create-modal"
      isValid={!!trimmedName}
      onClose={onClose}
      onSubmit={() => onCreate(trimmedName, category || undefined)}
      fields={[
        {
          id: 'list-name',
          label: 'List name',
          type: 'text',
          autoFocus: true,
          value: listName,
          onChange: setListName,
        },
        {
          id: 'list-category',
          label: 'Category',
          type: 'text',
          value: category,
          onChange: setCategory,
          renderInput: (id, value, onChange) => (
            <CategoryDropdown
              id={id}
              value={value}
              onChange={onChange}
              showLabel={false}
            />
          ),
        },
      ]}
    />
  );
};

export default ListCreateModal;
