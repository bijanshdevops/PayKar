import type { ReactNode } from 'react';

interface AnimatedFieldProps {
  label: string;
  htmlFor: string;
  error?: string;
  isValid?: boolean;
  children: ReactNode;
}

/**
 * فیلد فرم با بازخورد بصری انیمیشنی صحت مقدار (تیک سبز/علامت قرمز) — استفاده در صفحات
 * ورود/ثبت‌نام طبق تصمیم محصولی «UI انیمیشنی با انیمیشن‌های صحت‌سنجی».
 */
export default function AnimatedField({ label, htmlFor, error, isValid, children }: AnimatedFieldProps) {
  return (
    <div className={`field auth-field${error ? ' auth-field--invalid' : ''}${isValid && !error ? ' auth-field--valid' : ''}`}>
      <label htmlFor={htmlFor}>{label}</label>
      <div className="auth-field__control">
        {children}
        {isValid && !error && <span className="auth-field__icon auth-field__icon--valid">✓</span>}
        {error && <span className="auth-field__icon auth-field__icon--invalid">!</span>}
      </div>
      {error && <span className="error-text">{error}</span>}
    </div>
  );
}
