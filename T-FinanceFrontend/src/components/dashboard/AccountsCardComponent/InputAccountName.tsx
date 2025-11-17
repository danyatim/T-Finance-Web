import React, { useEffect, useRef } from 'react';
import './AccountsCard.css';

interface InputAccountNameProps {
    onBlur: () => void;
    onChangeInputName: (arg0: string) => void;
    value: string;
    className?: string;
    disabled?: boolean;  // Добавить это свойство
}

const InputAccountName: React.FC<InputAccountNameProps> = ({
    onBlur, 
    onChangeInputName, 
    value, 
    className,
    disabled = false  // Добавить в деструктуризацию с дефолтным значением
}) => {
    const inputRef = useRef<HTMLInputElement>(null);
    const isVisible = className?.includes('fade-in');

    useEffect(() => {
        if (isVisible && inputRef.current && !disabled) {
            // Небольшая задержка для завершения анимации появления
            const timer = setTimeout(() => {
                inputRef.current?.focus();
            }, 100);
            return () => clearTimeout(timer);
        }
    }, [isVisible, disabled]);

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === 'Enter' && !disabled) {
            onBlur();
        }
    };

    const handleBlur = () => {
        // Вызываем onBlur только если инпут видим и не заблокирован
        if (isVisible && !disabled) {
            onBlur();
        }
    };

    return (
        <input 
            ref={inputRef}
            name='accountname'
            className={`input-account-name ${className || ''}`} 
            type='text' 
            placeholder='Введите счет'
            onChange={(e) => onChangeInputName(e.target.value)}
            onBlur={handleBlur}
            value={value}
            disabled={disabled}
            onKeyDown={handleKeyDown}
        />
    )
}

export default InputAccountName