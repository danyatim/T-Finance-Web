// AccountsCardComponent/AccountsItem.tsx
import React, { memo } from 'react';
import './AccountsCard.css';

interface AccountsItemProps {
  accountId: number;
  accountName: string;
  accountValue: string;
  onDelete: (id: number) => void;
  isDeleting?: boolean;
  disabled?: boolean;
}

const AccountsItem: React.FC<AccountsItemProps> = ({
  accountId,
  accountName,
  accountValue,
  onDelete,
  isDeleting = false,
  disabled = false,
}) => {
  const handleDelete = (e: React.MouseEvent<HTMLButtonElement>) => {
    e.stopPropagation();
    if (!disabled && !isDeleting) {
      onDelete(accountId);
    }
  };

  return (
    <div className={`account-item ${isDeleting ? 'deleting' : ''}`}>
      <div className="account-info">
        <h3 className="account-name" title={accountName}>
          {accountName}
        </h3>
        <p className="account-sum">{accountValue}</p>
      </div>
      <button 
        className="delete-account-btn"
        onClick={handleDelete}
        disabled={disabled || isDeleting}
        aria-label={`Удалить счет ${accountName}`}
        type="button"
      >
        <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
          <path 
            d="M18 6L6 18M6 6L18 18" 
            stroke="currentColor" 
            strokeWidth="2" 
            strokeLinecap="round" 
            strokeLinejoin="round"
          />
        </svg>
      </button>
    </div>
  );
};

export default memo(AccountsItem);