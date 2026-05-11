import './ListAddButton.css';

type ListAddButtonProps = {
  onClick: () => void | Promise<void>;
};

const ListAddButton = ({ onClick }: ListAddButtonProps) => {
  return (
    <button className="list-add-button" type="button" onClick={onClick}>
      +
    </button>
  );
};

export default ListAddButton;
