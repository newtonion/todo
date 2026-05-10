import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Toggle from './Toggle.tsx';

describe('Toggle', () => {
  it('should render with label', () => {
    render(<Toggle id="test-toggle" label="Test Toggle" checked={false} onChange={() => {}} />);
    
    expect(screen.getByText('Test Toggle')).toBeInTheDocument();
  });

  it('should be checked when checked prop is true', () => {
    render(<Toggle id="test-toggle" label="Test Toggle" checked={true} onChange={() => {}} />);
    
    const checkbox = screen.getByRole('checkbox', { name: 'Test Toggle' });
    expect(checkbox).toBeChecked();
  });

  it('should be unchecked when checked prop is false', () => {
    render(<Toggle id="test-toggle" label="Test Toggle" checked={false} onChange={() => {}} />);
    
    const checkbox = screen.getByRole('checkbox', { name: 'Test Toggle' });
    expect(checkbox).not.toBeChecked();
  });

  it('should call onChange when clicked', async () => {
    const user = userEvent.setup();
    const handleChange = vi.fn();
    
    render(<Toggle id="test-toggle" label="Test Toggle" checked={false} onChange={handleChange} />);
    
    await user.click(screen.getByRole('checkbox', { name: 'Test Toggle' }));
    
    expect(handleChange).toHaveBeenCalledTimes(1);
  });

  it('should pass onChange with new checked state', async () => {
    const user = userEvent.setup();
    const handleChange = vi.fn();
    
    render(<Toggle id="test-toggle" label="Test Toggle" checked={false} onChange={handleChange} />);
    
    await user.click(screen.getByRole('checkbox', { name: 'Test Toggle' }));
    
    expect(handleChange).toHaveBeenCalledWith(true);
  });

  it('should use provided id', () => {
    render(<Toggle id="custom-id" label="Test Toggle" checked={false} onChange={() => {}} />);
    
    const checkbox = screen.getByRole('checkbox', { name: 'Test Toggle' });
    expect(checkbox).toHaveAttribute('id', 'custom-id');
  });
});
