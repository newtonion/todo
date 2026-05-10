import ListsSection from './ListsSection';
import './Sidebar.css';

type SidebarProps = {
  selectedListId: string | null;
  onListSelect: (listId: string) => void;
};

const Sidebar = ({ selectedListId, onListSelect }: SidebarProps) => {
  return (
    <aside className="sidebar">
      <ListsSection selectedListId={selectedListId} onListSelect={onListSelect} />
    </aside>
  );
};

export default Sidebar;
