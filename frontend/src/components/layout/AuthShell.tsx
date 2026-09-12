import type { ReactNode, RefObject } from 'react';

interface AuthShellProps {
  children: ReactNode;
  /** برای اجرای انیمیشن لرزش کارت هنگام بروز خطای سرور. */
  panelRef?: RefObject<HTMLDivElement>;
}

const brandPoints = [
  'ثبت‌نام سریع با کد پیامکی یا فرم کامل',
  'مدیریت آسان آگهی‌های استخدام برای کارفرمایان',
  'پیگیری آنلاین وضعیت درخواست‌های همکاری'
];

/**
 * قالب مشترک صفحات ورود/ثبت‌نام/بازیابی رمز عبور — تم شیشه‌ای تیره با رنگ‌های برند
 * (فیروزه‌ای/سرمه‌ای)، طبق تصمیم محصولی مبتنی بر نمونهٔ بصری ارسالی کاربر.
 */
export default function AuthShell({ children, panelRef }: AuthShellProps) {
  return (
    <div className="auth-shell">
      <span className="auth-shell__glow auth-shell__glow--teal" aria-hidden="true" />
      <span className="auth-shell__glow auth-shell__glow--navy" aria-hidden="true" />

      <aside className="auth-shell__brand">
        <div className="auth-shell__brand-logo">
          <img src="/logo.png" alt="پی کار" />
        </div>
        <h2>پی کار</h2>
        <p>مسیر مستقیم اتصال نیروی متخصص به کارفرمایان صنعتی سراسر کشور.</p>
        <ul className="auth-shell__brand-points">
          {brandPoints.map((point) => (
            <li key={point}>{point}</li>
          ))}
        </ul>
      </aside>

      <div className="auth-shell__panel" ref={panelRef}>
        {children}
      </div>
    </div>
  );
}
