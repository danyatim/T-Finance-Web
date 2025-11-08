import React from 'react';
import './NavButton.css';

interface NavButtonProps {
  children: React.ReactNode; // или string, JSX.Element, и т.д.
  current: string;
  onClick: () => void;
}

const NavButton: React.FC<NavButtonProps> = ({ children, current, onClick }) => {
  // Вы сравниваете current и children, чтобы определить активность
  const isActive = current === children;

  return (
    <button
      className={`nav-tab ${isActive ? 'active' : ''}`}
      onClick={onClick}
    >
      {children}
    </button>
  );
};

export default NavButton;
