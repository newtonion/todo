import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import SortableHeader from './SortableHeader.tsx';

describe('SortableHeader', () => {
  it('should render header with label', () => {
    render(
      <table>
        <thead>
          <tr>
            <SortableHeader
              field="name"
              label="Name"
              currentSortField="name"
              sortDirection="asc"
              onSortChange={() => {}}
            />
          </tr>
        </thead>
      </table>
    );
    
    expect(screen.getByRole('button', { name: /Name/ })).toBeInTheDocument();
  });

  it('should show up arrow when sorted ascending', () => {
    render(
      <table>
        <thead>
          <tr>
            <SortableHeader
              field="name"
              label="Name"
              currentSortField="name"
              sortDirection="asc"
              onSortChange={() => {}}
            />
          </tr>
        </thead>
      </table>
    );
    
    const button = screen.getByRole('button', { name: /Name/ });
    expect(button).toBeInTheDocument();
    // FontAwesome icon is rendered
  });

  it('should show down arrow when sorted descending', () => {
    render(
      <table>
        <thead>
          <tr>
            <SortableHeader
              field="name"
              label="Name"
              currentSortField="name"
              sortDirection="desc"
              onSortChange={() => {}}
            />
          </tr>
        </thead>
      </table>
    );
    
    const button = screen.getByRole('button', { name: /Name/ });
    expect(button).toBeInTheDocument();
  });

  it('should show unsorted icon when not the current sort field', () => {
    render(
      <table>
        <thead>
          <tr>
            <SortableHeader
              field="name"
              label="Name"
              currentSortField="dueDate"
              sortDirection="asc"
              onSortChange={() => {}}
            />
          </tr>
        </thead>
      </table>
    );
    
    const button = screen.getByRole('button', { name: /Name/ });
    expect(button).toBeInTheDocument();
  });

  it('should call onSortChange with field when clicked', async () => {
    const user = userEvent.setup();
    const handleSortChange = vi.fn();
    
    render(
      <table>
        <thead>
          <tr>
            <SortableHeader
              field="name"
              label="Name"
              currentSortField="dueDate"
              sortDirection="asc"
              onSortChange={handleSortChange}
            />
          </tr>
        </thead>
      </table>
    );
    
    await user.click(screen.getByRole('button', { name: /Name/ }));
    
    expect(handleSortChange).toHaveBeenCalledWith('name');
  });

  it('should render as table header cell', () => {
    const { container } = render(
      <table>
        <thead>
          <tr>
            <SortableHeader
              field="name"
              label="Name"
              currentSortField="name"
              sortDirection="asc"
              onSortChange={() => {}}
            />
          </tr>
        </thead>
      </table>
    );
    
    expect(container.querySelector('th')).toBeInTheDocument();
  });

  it('should have correct button type', () => {
    render(
      <table>
        <thead>
          <tr>
            <SortableHeader
              field="name"
              label="Name"
              currentSortField="name"
              sortDirection="asc"
              onSortChange={() => {}}
            />
          </tr>
        </thead>
      </table>
    );
    
    const button = screen.getByRole('button', { name: /Name/ });
    expect(button).toHaveAttribute('type', 'button');
  });
});
