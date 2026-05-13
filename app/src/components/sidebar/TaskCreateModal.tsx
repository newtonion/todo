import { useState } from 'react';
import CreateModal from '../shared/CreateModal';

type TaskCreateModalProps = {
  title?: string;
  submitLabel?: string;
  onClose: () => void;
  onCreate: (name: string, dueDate?: string) => void | Promise<void>;
};

const TaskCreateModal = ({
  title = 'New task',
  submitLabel,
  onClose,
  onCreate,
}: TaskCreateModalProps) => {
  const [taskName, setTaskName] = useState('');
  const [dueDate, setDueDate] = useState('');

  const trimmedName = taskName.trim();

  return (
    <CreateModal
      title={title}
      className="task-create-modal"
      isValid={!!trimmedName}
      submitLabel={submitLabel}
      onClose={onClose}
      onSubmit={() => onCreate(trimmedName, dueDate || undefined)}
      fields={[
        {
          id: 'task-name',
          label: 'Task name',
          type: 'text',
          autoFocus: true,
          value: taskName,
          onChange: setTaskName,
        },
        {
          id: 'task-due-date',
          label: 'Due date',
          type: 'date',
          value: dueDate,
          onChange: setDueDate,
        },
      ]}
    />
  );
};

export default TaskCreateModal;
