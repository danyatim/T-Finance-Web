import React from 'react';
import './HeaderLink.css';

interface HeaderLinkProps {
    children: React.ReactNode
    isDisable: boolean
    onClick: () => void
}

const HeaderLink: React.FC<HeaderLinkProps> = ({children, isDisable, onClick}) =>{

    return (
        <button className="header-link" 
                onClick={onClick}
                disabled={isDisable}
        >
            {children}
        </button>
    )
}

export default HeaderLink