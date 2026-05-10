import { SignInButton, SignUpButton } from '@clerk/react';
import './LandingPage.css';
import logoFlower from '../assets/logo-flower-md.webp';

const LandingPage = () => {
  return (
    <div className="landing-page">
      <div className="landing-content">
        <div className="logo-container">
            <img src={logoFlower} alt="Toduelip Logo" className="logo" />
        </div>
        <h1 className="landing-title">Welcome to Toduelip</h1>
        <p className="landing-subtitle">
          Let's get organized.
        </p>
        <div className="auth-buttons">
          <SignInButton>
            <button className="landing-auth-button landing-signin">Sign In</button>
          </SignInButton>
          <SignUpButton>
            <button className="landing-auth-button landing-signup">Sign Up</button>
          </SignUpButton>
        </div>
      </div>
    </div>
  );
};

export default LandingPage;
