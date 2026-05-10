import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import InlineEditable from './InlineEditable.tsx';

describe('InlineEditable', () => {
  const mockRenderInput = (value: string, onChange: (v: string) => void, ariaLabel: string) => (
    <input
      aria-label={ariaLabel}
      type="text"
      value={value}
      onChange={(e) => onChange(e.target.value)}
    />
  );

  describe('Display Mode', () => {
    it('should render display value when not editing', () => {
      render(
        <InlineEditable
          itemId="item-1"
          editState={null}
          displayValue="Test Value"
          ariaLabel="Test field"
          onStartEdit={() => {}}
          onSave={() => {}}
          onCancel={() => {}}
          onChange={() => {}}
          renderInput={mockRenderInput}
        />
      );

      expect(screen.getByText('Test Value')).toBeInTheDocument();
    });

    it('should render ReactNode as display value', () => {
      render(
        <InlineEditable
          itemId="item-1"
          editState={null}
          displayValue={<h1>Heading Value</h1>}
          ariaLabel="Test field"
          onStartEdit={() => {}}
          onSave={() => {}}
          onCancel={() => {}}
          onChange={() => {}}
          renderInput={mockRenderInput}
        />
      );

      expect(screen.getByRole('heading', { name: 'Heading Value' })).toBeInTheDocument();
    });

    it('should show edit button in display mode', () => {
      render(
        <InlineEditable
          itemId="item-1"
          editState={null}
          displayValue="Test Value"
          ariaLabel="Test field"
          onStartEdit={() => {}}
          onSave={() => {}}
          onCancel={() => {}}
          onChange={() => {}}
          renderInput={mockRenderInput}
        />
      );

      expect(screen.getByRole('button', { name: 'Edit Test field' })).toBeInTheDocument();
    });

    it('should call onStartEdit when edit button is clicked', async () => {
      const user = userEvent.setup();
      const handleStartEdit = vi.fn();

      render(
        <InlineEditable
          itemId="item-1"
          editState={null}
          displayValue="Test Value"
          ariaLabel="Test field"
          onStartEdit={handleStartEdit}
          onSave={() => {}}
          onCancel={() => {}}
          onChange={() => {}}
          renderInput={mockRenderInput}
        />
      );

      await user.click(screen.getByRole('button', { name: 'Edit Test field' }));

      expect(handleStartEdit).toHaveBeenCalledTimes(1);
    });
  });

  describe('Edit Mode', () => {
    it('should render input when editing current item', () => {
      const editState = {
        itemId: 'item-1',
        value: 'editing',
        isSaving: false,
      };

      render(
        <InlineEditable
          itemId="item-1"
          editState={editState}
          displayValue="Test Value"
          ariaLabel="Test field"
          onStartEdit={() => {}}
          onSave={() => {}}
          onCancel={() => {}}
          onChange={() => {}}
          renderInput={mockRenderInput}
        />
      );

      const input = screen.getByRole('textbox', { name: 'Test field' });
      expect(input).toBeInTheDocument();
      expect(input).toHaveValue('editing');
    });

    it('should not render input when editing different item', () => {
      const editState = {
        itemId: 'item-2',
        value: 'editing',
        isSaving: false,
      };

      render(
        <InlineEditable
          itemId="item-1"
          editState={editState}
          displayValue="Test Value"
          ariaLabel="Test field"
          onStartEdit={() => {}}
          onSave={() => {}}
          onCancel={() => {}}
          onChange={() => {}}
          renderInput={mockRenderInput}
        />
      );

      expect(screen.queryByRole('textbox')).not.toBeInTheDocument();
      expect(screen.getByText('Test Value')).toBeInTheDocument();
    });

    it('should render save and discard buttons in edit mode', () => {
      const editState = {
        itemId: 'item-1',
        value: 'editing',
        isSaving: false,
      };

      render(
        <InlineEditable
          itemId="item-1"
          editState={editState}
          displayValue="Test Value"
          ariaLabel="Test field"
          onStartEdit={() => {}}
          onSave={() => {}}
          onCancel={() => {}}
          onChange={() => {}}
          renderInput={mockRenderInput}
        />
      );

      expect(screen.getByRole('button', { name: 'Save Test field' })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /Discard changes/ })).toBeInTheDocument();
    });

    it('should disable save button when invalid', () => {
      const editState = {
        itemId: 'item-1',
        value: '',
        isSaving: false,
      };

      render(
        <InlineEditable
          itemId="item-1"
          editState={editState}
          displayValue="Test Value"
          ariaLabel="Test field"
          isValid={false}
          onStartEdit={() => {}}
          onSave={() => {}}
          onCancel={() => {}}
          onChange={() => {}}
          renderInput={mockRenderInput}
        />
      );

      expect(screen.getByRole('button', { name: 'Save Test field' })).toBeDisabled();
    });

    it('should disable buttons when saving', () => {
      const editState = {
        itemId: 'item-1',
        value: 'editing',
        isSaving: true,
      };

      render(
        <InlineEditable
          itemId="item-1"
          editState={editState}
          displayValue="Test Value"
          ariaLabel="Test field"
          onStartEdit={() => {}}
          onSave={() => {}}
          onCancel={() => {}}
          onChange={() => {}}
          renderInput={mockRenderInput}
        />
      );

      expect(screen.getByRole('button', { name: 'Save Test field' })).toBeDisabled();
      expect(screen.getByRole('button', { name: /Discard changes/ })).toBeDisabled();
    });

    it('should call onChange when input value changes', async () => {
      const user = userEvent.setup();
      const handleChange = vi.fn();
      const editState = {
        itemId: 'item-1',
        value: 'test',
        isSaving: false,
      };

      render(
        <InlineEditable
          itemId="item-1"
          editState={editState}
          displayValue="Test Value"
          ariaLabel="Test field"
          onStartEdit={() => {}}
          onSave={() => {}}
          onCancel={() => {}}
          onChange={handleChange}
          renderInput={mockRenderInput}
        />
      );

      const input = screen.getByRole('textbox', { name: 'Test field' });
      await user.clear(input);
      await user.type(input, 'new value');

      expect(handleChange).toHaveBeenCalled();
    });

    it('should call onSave when form is submitted', async () => {
      const user = userEvent.setup();
      const handleSave = vi.fn((e) => e.preventDefault());
      const editState = {
        itemId: 'item-1',
        value: 'editing',
        isSaving: false,
      };

      render(
        <InlineEditable
          itemId="item-1"
          editState={editState}
          displayValue="Test Value"
          ariaLabel="Test field"
          onStartEdit={() => {}}
          onSave={handleSave}
          onCancel={() => {}}
          onChange={() => {}}
          renderInput={mockRenderInput}
        />
      );

      await user.click(screen.getByRole('button', { name: 'Save Test field' }));

      expect(handleSave).toHaveBeenCalledTimes(1);
    });

    it('should call onCancel when discard button is clicked', async () => {
      const user = userEvent.setup();
      const handleCancel = vi.fn();
      const editState = {
        itemId: 'item-1',
        value: 'editing',
        isSaving: false,
      };

      render(
        <InlineEditable
          itemId="item-1"
          editState={editState}
          displayValue="Test Value"
          ariaLabel="Test field"
          onStartEdit={() => {}}
          onSave={() => {}}
          onCancel={handleCancel}
          onChange={() => {}}
          renderInput={mockRenderInput}
        />
      );

      await user.click(screen.getByRole('button', { name: /Discard changes/ }));

      expect(handleCancel).toHaveBeenCalledTimes(1);
    });

    it('should apply custom className to form', () => {
      const editState = {
        itemId: 'item-1',
        value: 'editing',
        isSaving: false,
      };

      const { container } = render(
        <InlineEditable
          itemId="item-1"
          editState={editState}
          displayValue="Test Value"
          ariaLabel="Test field"
          className="custom-edit-form"
          onStartEdit={() => {}}
          onSave={() => {}}
          onCancel={() => {}}
          onChange={() => {}}
          renderInput={mockRenderInput}
        />
      );

      expect(container.querySelector('.custom-edit-form')).toBeInTheDocument();
    });
  });
});
