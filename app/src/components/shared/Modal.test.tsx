import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import Modal from './Modal.tsx';

describe('Modal', () => {
  it('should render children inside modal', () => {
    render(
      <Modal onClose={() => {}}>
        <div>Test Content</div>
      </Modal>
    );

    expect(screen.getByText('Test Content')).toBeInTheDocument();
  });

  it('should call onClose when backdrop is clicked', () => {
    let closeCalled = false;
    const handleClose = () => { closeCalled = true; };

    const { container } = render(
      <Modal onClose={handleClose}>
        <div>Test Content</div>
      </Modal>
    );

    const backdrop = container.querySelector<HTMLElement>('.modal-backdrop');
    backdrop?.click();

    expect(closeCalled).toBe(true);
  });

  it('should not call onClose when content is clicked', () => {
    let closeCalled = false;
    const handleClose = () => { closeCalled = true; };

    render(
      <Modal onClose={handleClose}>
        <div>Test Content</div>
      </Modal>
    );

    screen.getByText('Test Content').click();

    expect(closeCalled).toBe(false);
  });

  it('should render modal backdrop', () => {
    const { container } = render(
      <Modal onClose={() => {}}>
        <div>Test</div>
      </Modal>
    );

    expect(container.querySelector('.modal-backdrop')).toBeInTheDocument();
  });
});
