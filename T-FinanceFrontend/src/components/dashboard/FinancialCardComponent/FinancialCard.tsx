import React from 'react';
import './FinancialCard.css';

interface FinancialCardProps {
    CardTitle: string
    CardValue: number
}

const FinancialCard: React.FC<FinancialCardProps> = ({CardTitle, CardValue}) =>{

    return (
        <div className="financial-card">
            <div className="card-title">{CardTitle}</div>
            <div className="card-value">{CardValue}₽</div>
        </div>
    )
}

export default FinancialCard