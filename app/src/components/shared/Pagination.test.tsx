import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Pagination from './Pagination.tsx';

describe('Pagination', () => {
  it('should not render when totalCount is less than or equal to pageSize', () => {
    const { container } = render(
      <Pagination
        itemCount={5}
        offset={0}
        pageSize={10}
        totalCount={10}
        onNextPage={() => {}}
        onPreviousPage={() => {}}
      />
    );
    
    expect(container.querySelector('.pagination')).not.toBeInTheDocument();
  });

  it('should render when totalCount exceeds pageSize', () => {
    render(
      <Pagination
        itemCount={10}
        offset={0}
        pageSize={10}
        totalCount={25}
        onNextPage={() => {}}
        onPreviousPage={() => {}}
      />
    );
    
    expect(screen.getByRole('navigation')).toBeInTheDocument();
  });

  it('should display correct page status', () => {
    render(
      <Pagination
        itemCount={10}
        offset={10}
        pageSize={10}
        totalCount={25}
        onNextPage={() => {}}
        onPreviousPage={() => {}}
      />
    );
    
    expect(screen.getByText('11-20')).toBeInTheDocument();
  });

  it('should disable previous button on first page', () => {
    render(
      <Pagination
        itemCount={10}
        offset={0}
        pageSize={10}
        totalCount={25}
        onNextPage={() => {}}
        onPreviousPage={() => {}}
      />
    );
    
    const prevButton = screen.getByRole('button', { name: 'Previous page' });
    expect(prevButton).toBeDisabled();
  });

  it('should enable previous button when not on first page', () => {
    render(
      <Pagination
        itemCount={10}
        offset={10}
        pageSize={10}
        totalCount={25}
        onNextPage={() => {}}
        onPreviousPage={() => {}}
      />
    );
    
    const prevButton = screen.getByRole('button', { name: 'Previous page' });
    expect(prevButton).not.toBeDisabled();
  });

  it('should disable next button on last page', () => {
    render(
      <Pagination
        itemCount={5}
        offset={20}
        pageSize={10}
        totalCount={25}
        onNextPage={() => {}}
        onPreviousPage={() => {}}
      />
    );
    
    const nextButton = screen.getByRole('button', { name: 'Next page' });
    expect(nextButton).toBeDisabled();
  });

  it('should enable next button when not on last page', () => {
    render(
      <Pagination
        itemCount={10}
        offset={0}
        pageSize={10}
        totalCount={25}
        onNextPage={() => {}}
        onPreviousPage={() => {}}
      />
    );
    
    const nextButton = screen.getByRole('button', { name: 'Next page' });
    expect(nextButton).not.toBeDisabled();
  });

  it('should call onPreviousPage when previous button is clicked', async () => {
    const user = userEvent.setup();
    const handlePrevious = vi.fn();
    
    render(
      <Pagination
        itemCount={10}
        offset={10}
        pageSize={10}
        totalCount={25}
        onNextPage={() => {}}
        onPreviousPage={handlePrevious}
      />
    );
    
    await user.click(screen.getByRole('button', { name: 'Previous page' }));
    
    expect(handlePrevious).toHaveBeenCalledTimes(1);
  });

  it('should call onNextPage when next button is clicked', async () => {
    const user = userEvent.setup();
    const handleNext = vi.fn();
    
    render(
      <Pagination
        itemCount={10}
        offset={0}
        pageSize={10}
        totalCount={25}
        onNextPage={handleNext}
        onPreviousPage={() => {}}
      />
    );
    
    await user.click(screen.getByRole('button', { name: 'Next page' }));
    
    expect(handleNext).toHaveBeenCalledTimes(1);
  });

  it('should use custom aria-label', () => {
    render(
      <Pagination
        ariaLabel="Custom pagination"
        itemCount={10}
        offset={0}
        pageSize={10}
        totalCount={25}
        onNextPage={() => {}}
        onPreviousPage={() => {}}
      />
    );
    
    expect(screen.getByRole('navigation', { name: 'Custom pagination' })).toBeInTheDocument();
  });

  it('should apply custom className', () => {
    const { container } = render(
      <Pagination
        className="custom-pagination"
        itemCount={10}
        offset={0}
        pageSize={10}
        totalCount={25}
        onNextPage={() => {}}
        onPreviousPage={() => {}}
      />
    );
    
    expect(container.querySelector('.custom-pagination')).toBeInTheDocument();
  });
});
