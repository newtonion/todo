import type { ButtonHTMLAttributes } from 'react';
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import { faCheck, faPen, faPlus, faXmark } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

type IconButtonProps = Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'aria-label'> & {
  ariaLabel: string;
  icon: IconDefinition;
  size?: 'default' | 'small';
};

export const IconButton = ({
  ariaLabel,
  className = '',
  icon,
  size = 'default',
  type = 'button',
  ...buttonProps
}: IconButtonProps) => {
  const sizeClass = size === 'small' ? ' main-page-icon-button-small' : '';

  return (
    <button
      aria-label={ariaLabel}
      className={`main-page-icon-button${sizeClass}${className ? ` ${className}` : ''}`}
      type={type}
      {...buttonProps}
    >
      <FontAwesomeIcon icon={icon} />
    </button>
  );
};

type NamedIconButtonProps = Omit<IconButtonProps, 'icon'>;

export const AddIconButton = (props: NamedIconButtonProps) => (
  <IconButton icon={faPlus} {...props} />
);

export const DiscardIconButton = (props: NamedIconButtonProps) => (
  <IconButton icon={faXmark} {...props} />
);

export const EditIconButton = (props: NamedIconButtonProps) => (
  <IconButton icon={faPen} {...props} />
);

export const SaveIconButton = (props: NamedIconButtonProps) => (
  <IconButton icon={faCheck} {...props} />
);
