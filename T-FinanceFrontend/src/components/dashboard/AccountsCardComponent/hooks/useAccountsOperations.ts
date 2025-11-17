import { useState, useCallback } from "react";
import { api, ApiError } from "@/services/api";
import { API_ENDPOINTS } from "@/utils/constants";
import { ACCOUNT_NAME_MIN_LENGTH, DELETE_ANIMATION_DURATION } from "../constants";

interface UseAccountsOperationsProps{
    onUpdate: () => void;
    onError?: (error: string) => void;
}

const useAccountsOperations = ({onUpdate, onError}: UseAccountsOperationsProps) => {
    const [ deletingIds, setDeletingIds ] = useState<Set<number>>(new Set());
    const [ isLoading, setIsLoading ] = useState(false);

    const validateAccountName = (name: string): boolean => {
        return name.trim().length >= ACCOUNT_NAME_MIN_LENGTH;
    }

    const addAccount = useCallback(async ( name : string ) => {
        if (!validateAccountName(name)) {
            onError?.('Название счета должно содержать минимум 7 символов')
            return false;
        }

        setIsLoading(true)
        try {
            await api.post(API_ENDPOINTS.USER.BANK_ACCOUNT, { Name: name.trim() });
            onUpdate();
            return true;
        } catch (error: unknown) {
            const errorMessage = error instanceof ApiError ? error.message : 'Не удалось добавить счет';
            if (error instanceof ApiError && error.status === 401) {
                onError?.('Сессия истекла. Пожалуйста, войдите снова.');
            } else {
                onError?.(errorMessage)
            }
            return false;
        } finally {
            setIsLoading(false);
        }
    }, [onUpdate, onError]);

    const deleteAccount = useCallback(async (id: number) => {
        setDeletingIds(prev => new Set(prev).add(id));

        setTimeout(async () => {
            try {
                await api.delete(`${API_ENDPOINTS.USER.BANK_ACCOUNT}/${id}`);
                onUpdate();
            } catch (error) {
                setDeletingIds(prev => {
                    const newSet = new Set(prev);
                    newSet.delete(id);
                    return newSet;
                });

                const errorMessage = error instanceof ApiError ? error.message : 'Не удалось удалить счет';
                onError?.(errorMessage);
            }
        }, DELETE_ANIMATION_DURATION);
    }, [onUpdate, onError]);

    return {
        addAccount,
        deleteAccount,
        deletingIds,
        isLoading,
        validateAccountName,
    };
};
export {useAccountsOperations}