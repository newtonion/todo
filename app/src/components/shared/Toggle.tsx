import './Toggle.css';

type ToggleProps = {
  checked: boolean;
  label: string;
  onChange: (checked: boolean) => void;
  id?: string;
};

const Toggle = ({ checked, label, onChange, id }: ToggleProps) => {
  return (
    <label className="toggle-control" htmlFor={id}>
      <span className="toggle-label">{label}</span>
      <input
        checked={checked}
        id={id}
        type="checkbox"
        onChange={(event) => onChange(event.target.checked)}
      />
      <span className="toggle-track" aria-hidden="true">
        <span className="toggle-thumb" />
      </span>
    </label>
  );
};

export default Toggle;
