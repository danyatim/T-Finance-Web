import { useState, useCallback } from "react";

const useAccountInput = () => {
    const [isInputVisible, setisInputVisible] = useState(false);
    const [accountName, setAccountName] =useState('');

    const openInput = useCallback(() => {
        setAccountName('');
        setisInputVisible(true);
    }, []);

    const closeInput = useCallback(() => {
        setAccountName('');
        setisInputVisible(false);
    }, []);

    const toggleInput = useCallback(() => {
        if (isInputVisible){
            closeInput();
        } else {
            openInput();
        }
    }, [isInputVisible, openInput, closeInput]);
    
    return {
        isInputVisible,
        accountName,
        setAccountName,
        openInput,
        closeInput,
        toggleInput,
    };
};

export {useAccountInput}