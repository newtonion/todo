import type { ReactNode } from 'react';
import './Modal.css';

type ModalProps = {
  children: ReactNode;
  onClose: () => void;
};

const Modal = ({ children, onClose }: ModalProps) => {
  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div onClick={(event) => event.stopPropagation()}>
        {children}
      </div>
    </div>
  );
};

export default Modal;
