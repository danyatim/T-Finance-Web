import React from 'react';
import './LoginMessage.css';

interface FinancialCardProps {
    loginMessage: string
}

const FinancialCard: React.FC<FinancialCardProps> = ({loginMessage}) =>{

    return (
        <div className='loginMessageBox'>
          {loginMessage}
        </div>
    )
}
export default FinancialCard