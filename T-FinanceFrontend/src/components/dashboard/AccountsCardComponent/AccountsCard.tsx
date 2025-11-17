// AccountsCardComponent/AccountsCard.tsx
import React from 'react';
import './AccountsCard.css';
import { BankAccount } from '@/services/api';
import { useAccountsOperations } from './hooks/useAccountsOperations';
import { useAccountInput } from './hooks/useAccountInput';
import { formatAccountForDisplay } from './utils/formatAccount';
import AccountsItem from './AccountsItem';
import AddAccountButton from './AddAccountButton';
import InputAccountName from './InputAccountName';

interface AccountsCardProps {
  accounts: BankAccount[];
  onUpdateAccounts: () => void;
}

const AccountsCard: React.FC<AccountsCardProps> = ({ 
  accounts, 
  onUpdateAccounts 
}) => {
  const {
    isInputVisible,
    accountName,
    setAccountName,
    toggleInput,
  } = useAccountInput();

  const {
    addAccount,
    deleteAccount,
    deletingIds,
    isLoading,
    validateAccountName,
  } = useAccountsOperations({
    onUpdate: onUpdateAccounts,
    onError: (error) => alert(error), // Можно заменить на toast
  });

  const handleInputSubmit = async () => {
    if (isInputVisible && validateAccountName(accountName)) {
      const success = await addAccount(accountName);
      if (success) {
        toggleInput();
      }
    } else {
      toggleInput();
    }
  };

  const handleInputToggle = () => {
    if (isInputVisible) {
      handleInputSubmit();
    } else {
      toggleInput();
    }
  };

  const getFadeClass = (isVisible: boolean) => isVisible ? 'fade-in' : 'fade-out';

  return (
    <div className="accounts-card">
      <div className="header-accounts-card">
        <h2 className="accounts-title">Счета:</h2>
        <div className="account-input-wrapper">
          <AddAccountButton 
            onButtonClick={handleInputToggle} 
            className={getFadeClass(!isInputVisible)}
            disabled={isLoading}
          />
          <InputAccountName
            onBlur={handleInputSubmit}
            onChangeInputName={setAccountName}
            value={accountName}
            className={getFadeClass(isInputVisible)}
            disabled={isLoading}
          />
        </div>
      </div>
      
      <div className="accounts-content">
        {accounts.length > 0 ? (
          accounts.map((account) => {
            const formatted = formatAccountForDisplay(account);
            return (
              <AccountsItem 
                key={account.id}
                accountId={formatted.id}
                accountName={formatted.name}
                accountValue={formatted.formattedBalance}
                onDelete={deleteAccount}
                isDeleting={deletingIds.has(account.id)}
                disabled={isLoading}
              />
            );
          })
        ) : (
          <p className="no-accounts-title">Нет счетов</p>
        )}
      </div>
    </div>
  );
};

export default AccountsCard;