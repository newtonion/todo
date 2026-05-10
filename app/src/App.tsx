import { useUser } from '@clerk/react';
import { useState, useEffect } from 'react';
import Sidebar from './components/sidebar/Sidebar';
import MainPage from './components/listSection/ListSection';
import LandingPage from './components/LandingPage';
import { Header } from './components/Header';
import { useUserApi } from './api/users/useUserApi';

function App() {
  const { isSignedIn, isLoaded, user } = useUser();
  const { getUser } = useUserApi();
  const [loadedUserId, setLoadedUserId] = useState<string | null>(null);
  const [userDataError, setUserDataError] = useState<string | null>(null);
  const [selectedListId, setSelectedListId] = useState<string | null>(null);

  useEffect(() => {
    if (isSignedIn && isLoaded && user && loadedUserId !== user.id) {
      getUser()
        .then(() => {
          setLoadedUserId(user.id);
          setUserDataError(null);
        })
        .catch((error) => {
          setUserDataError(error.message);
          setLoadedUserId(user.id);
        });
    }
  }, [getUser, isLoaded, isSignedIn, loadedUserId, user]);

  if (!isLoaded) {
    return <div>Loading...</div>;
  }

  if (!isSignedIn) {
    return <LandingPage />;
  }

  if (loadedUserId !== user?.id) {
    return <div>Loading user data...</div>;
  }

  if (userDataError) {
    return <div>There was an issue, please try again later</div>;
  }

  return (
    <>
      <Header />
      <div style={{ display: 'flex', minHeight: '100vh' }}>
        <Sidebar selectedListId={selectedListId} onListSelect={setSelectedListId} />
        <MainPage selectedListId={selectedListId} />
      </div>
    </>
  );
}

export default App
