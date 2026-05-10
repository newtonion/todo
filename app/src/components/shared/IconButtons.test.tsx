import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { IconButton, AddIconButton, EditIconButton, SaveIconButton, DiscardIconButton } from './IconButtons.tsx';
import { faPlus } from '@fortawesome/free-solid-svg-icons';

describe('IconButtons', () => {
  describe('IconButton', () => {
    it('should render with icon', () => {
      render(<IconButton ariaLabel="Test button" icon={faPlus} />);
      
      const button = screen.getByRole('button', { name: 'Test button' });
      expect(button).toBeInTheDocument();
    });

    it('should call onClick when clicked', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      
      render(<IconButton ariaLabel="Test button" icon={faPlus} onClick={handleClick} />);
      
      await user.click(screen.getByRole('button', { name: 'Test button' }));
      
      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('should apply custom className', () => {
      const { container } = render(
        <IconButton ariaLabel="Test button" icon={faPlus} className="custom-class" />
      );
      
      const button = container.querySelector('.custom-class');
      expect(button).toBeInTheDocument();
    });

    it('should apply small size class', () => {
      const { container } = render(
        <IconButton ariaLabel="Test button" icon={faPlus} size="small" />
      );
      
      const button = container.querySelector('.main-page-icon-button-small');
      expect(button).toBeInTheDocument();
    });

    it('should be disabled when disabled prop is true', () => {
      render(<IconButton ariaLabel="Test button" icon={faPlus} disabled />);
      
      const button = screen.getByRole('button', { name: 'Test button' });
      expect(button).toBeDisabled();
    });

    it('should have default button type', () => {
      render(<IconButton ariaLabel="Test button" icon={faPlus} />);
      
      const button = screen.getByRole('button', { name: 'Test button' });
      expect(button).toHaveAttribute('type', 'button');
    });

    it('should support submit type', () => {
      render(<IconButton ariaLabel="Test button" icon={faPlus} type="submit" />);
      
      const button = screen.getByRole('button', { name: 'Test button' });
      expect(button).toHaveAttribute('type', 'submit');
    });
  });

  describe('AddIconButton', () => {
    it('should render add button', () => {
      render(<AddIconButton ariaLabel="Add item" />);
      expect(screen.getByRole('button', { name: 'Add item' })).toBeInTheDocument();
    });
  });

  describe('EditIconButton', () => {
    it('should render edit button', () => {
      render(<EditIconButton ariaLabel="Edit item" />);
      expect(screen.getByRole('button', { name: 'Edit item' })).toBeInTheDocument();
    });
  });

  describe('SaveIconButton', () => {
    it('should render save button', () => {
      render(<SaveIconButton ariaLabel="Save changes" />);
      expect(screen.getByRole('button', { name: 'Save changes' })).toBeInTheDocument();
    });
  });

  describe('DiscardIconButton', () => {
    it('should render discard button', () => {
      render(<DiscardIconButton ariaLabel="Discard changes" />);
      expect(screen.getByRole('button', { name: 'Discard changes' })).toBeInTheDocument();
    });
  });
});
