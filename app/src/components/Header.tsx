import './Header.css'
import { Show, UserButton, useUser } from '@clerk/react'
import logoheader from '../assets/logo-header.webp';

function Header() {
  const { user } = useUser()
  
  return (
    <>
      <header className="header">
        <img src={logoheader} alt="Todulip Logo" className="logo-header" />
        <div className="auth-button-container">
            <Show when="signed-in">
            <UserButton />
            <span className="username">{user?.username ||'unknown'}</span>
            </Show>
        </div>
      </header>
    </>
  )
}

export { Header }