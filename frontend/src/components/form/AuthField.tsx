import type { ReactNode } from 'react';

interface AuthFieldProps {
  label: string;
  htmlFor: string;
  icon: ReactNode;
  error?: string;
  trailing?: ReactNode;
  children: ReactNode;
}

/**
 * فیلد ورودی با آیکون پیشوندی و امکان آیکون پسوندی (مثل دکمهٔ نمایش/عدم‌نمایش رمز عبور) —
 * نسخهٔ تم شیشه‌ای تیره، برای صفحات ورود/ثبت‌نام/بازیابی رمز عبور.
 */
export default function AuthField({ label, htmlFor, icon, error, trailing, children }: AuthFieldProps) {
  return (
    <div className={`field auth-ig${error ? ' auth-ig--invalid' : ''}`}>
      <label htmlFor={htmlFor}>{label}</label>
      <div className="auth-ig__control">
        <span className="auth-ig__icon">{icon}</span>
        {children}
        {trailing && <span className="auth-ig__trailing">{trailing}</span>}
      </div>
      {error && <span className="error-text">{error}</span>}
    </div>
  );
}
