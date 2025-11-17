import React from 'react';
import './LoginMessage.css';

interface LoginMessageProps {
    loginMessage: string
}

const FinancialCard: React.FC<LoginMessageProps> = ({loginMessage}) =>{

    return (
        <div className='loginMessageBox'>
          {loginMessage}
        </div>
    )
}
export default FinancialCard