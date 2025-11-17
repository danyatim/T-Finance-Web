import React from 'react';
import './AccountsCard.css';

interface AddAccountButtonProps {
    onButtonClick: () => void;
    className?: string;
    disabled?: boolean;
}

const AddAccountButton: React.FC<AddAccountButtonProps> = ({
    onButtonClick, 
    className,
    disabled = false
}) => {

    const handleClick = () => {
        if (!disabled) {
            onButtonClick();
        }
    };

    return (
        <button 
            className={`add-account-btn ${className || ''}`}
            type="button"
            aria-label="Добавить счет"
            onClick={handleClick}
            disabled={disabled}
        >
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
                <path d="M12 5V19M5 12H19" stroke="#ffffff" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
        </button>
    )
}

export default AddAccountButton