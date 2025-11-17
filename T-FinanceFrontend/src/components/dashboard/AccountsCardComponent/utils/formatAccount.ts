import { BankAccount } from "@/services/api";
import { LOCALE, CURRENCY_SYMBOL } from "../constants"; 

const formatAccountBalance = (balance: number): string => {
    return `${balance.toLocaleString(LOCALE, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    })} ${CURRENCY_SYMBOL}`;
};

const formatAccountForDisplay = (account: BankAccount): {
    id: number;
    name: string;
    formattedBalance: string;
} => {
    return {
        id: account.id,
        name: account.name,
        formattedBalance: formatAccountBalance(account.balance ?? 0),
    };
};

export { formatAccountBalance, formatAccountForDisplay}