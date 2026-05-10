import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import CreateModal from './CreateModal.tsx';

describe('CreateModal', () => {
  it('should render with title', () => {
    render(
      <CreateModal
        title="Create New Item"
        fields={[]}
        isValid={true}
        onClose={() => {}}
        onSubmit={() => {}}
      />
    );

    expect(screen.getByRole('heading', { name: 'Create New Item' })).toBeInTheDocument();
  });

  it('should render all fields', () => {
    const fields = [
      { id: 'name', label: 'Name', type: 'text' as const, value: '', onChange: () => {} },
      { id: 'date', label: 'Date', type: 'date' as const, value: '', onChange: () => {} },
    ];

    render(
      <CreateModal
        title="Create"
        fields={fields}
        isValid={true}
        onClose={() => {}}
        onSubmit={() => {}}
      />
    );

    expect(screen.getByLabelText('Name')).toBeInTheDocument();
    expect(screen.getByLabelText('Date')).toBeInTheDocument();
  });

  it('should render field with autoFocus', () => {
    const fields = [
      { id: 'name', label: 'Name', type: 'text' as const, value: '', onChange: () => {}, autoFocus: true },
    ];

    render(
      <CreateModal
        title="Create"
        fields={fields}
        isValid={true}
        onClose={() => {}}
        onSubmit={() => {}}
      />
    );

    expect(screen.getByLabelText('Name')).toHaveFocus();
  });

  it('should call onChange when field value changes', async () => {
    const user = userEvent.setup();
    const handleChange = vi.fn();
    const fields = [
      { id: 'name', label: 'Name', type: 'text' as const, value: '', onChange: handleChange },
    ];

    render(
      <CreateModal
        title="Create"
        fields={fields}
        isValid={true}
        onClose={() => {}}
        onSubmit={() => {}}
      />
    );

    await user.type(screen.getByLabelText('Name'), 'test');

    expect(handleChange).toHaveBeenCalled();
  });

  it('should render custom input using renderInput', () => {
    const fields = [
      {
        id: 'custom',
        label: 'Custom Field',
        type: 'text' as const,
        value: 'value',
        onChange: () => {},
        renderInput: (id: string, value: string) => (
          <div data-testid="custom-input">{value}</div>
        ),
      },
    ];

    render(
      <CreateModal
        title="Create"
        fields={fields}
        isValid={true}
        onClose={() => {}}
        onSubmit={() => {}}
      />
    );

    expect(screen.getByTestId('custom-input')).toHaveTextContent('value');
  });

  it('should render Cancel and Create buttons', () => {
    render(
      <CreateModal
        title="Create"
        fields={[]}
        isValid={true}
        onClose={() => {}}
        onSubmit={() => {}}
      />
    );

    expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Create' })).toBeInTheDocument();
  });

  it('should use custom submit label', () => {
    render(
      <CreateModal
        title="Create"
        fields={[]}
        submitLabel="Add Item"
        isValid={true}
        onClose={() => {}}
        onSubmit={() => {}}
      />
    );

    expect(screen.getByRole('button', { name: 'Add Item' })).toBeInTheDocument();
  });

  it('should disable submit button when not valid', () => {
    render(
      <CreateModal
        title="Create"
        fields={[]}
        isValid={false}
        onClose={() => {}}
        onSubmit={() => {}}
      />
    );

    expect(screen.getByRole('button', { name: 'Create' })).toBeDisabled();
  });

  it('should call onClose when Cancel button is clicked', async () => {
    const user = userEvent.setup();
    const handleClose = vi.fn();

    render(
      <CreateModal
        title="Create"
        fields={[]}
        isValid={true}
        onClose={handleClose}
        onSubmit={() => {}}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Cancel' }));

    expect(handleClose).toHaveBeenCalledTimes(1);
  });

  it('should call onSubmit and onClose when form is submitted with valid data', async () => {
    const user = userEvent.setup();
    const handleSubmit = vi.fn();
    const handleClose = vi.fn();

    render(
      <CreateModal
        title="Create"
        fields={[]}
        isValid={true}
        onClose={handleClose}
        onSubmit={handleSubmit}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Create' }));

    expect(handleSubmit).toHaveBeenCalledTimes(1);
    expect(handleClose).toHaveBeenCalledTimes(1);
  });

  it('should not submit when invalid', async () => {
    const user = userEvent.setup();
    const handleSubmit = vi.fn();

    render(
      <CreateModal
        title="Create"
        fields={[]}
        isValid={false}
        onClose={() => {}}
        onSubmit={handleSubmit}
      />
    );

    // Button is disabled, but try to click anyway
    const submitButton = screen.getByRole('button', { name: 'Create' });
    expect(submitButton).toBeDisabled();

    expect(handleSubmit).not.toHaveBeenCalled();
  });

  it('should apply custom className', () => {
    const { container } = render(
      <CreateModal
        title="Create"
        fields={[]}
        className="custom-modal"
        isValid={true}
        onClose={() => {}}
        onSubmit={() => {}}
      />
    );

    expect(container.querySelector('.custom-modal')).toBeInTheDocument();
  });

  it('should handle async onSubmit', async () => {
    const user = userEvent.setup();
    const handleSubmit = vi.fn(async () => {
      await new Promise(resolve => setTimeout(resolve, 10));
    });
    const handleClose = vi.fn();

    render(
      <CreateModal
        title="Create"
        fields={[]}
        isValid={true}
        onClose={handleClose}
        onSubmit={handleSubmit}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Create' }));

    expect(handleSubmit).toHaveBeenCalledTimes(1);
    // Wait for async operation
    await vi.waitFor(() => {
      expect(handleClose).toHaveBeenCalledTimes(1);
    });
  });
});
