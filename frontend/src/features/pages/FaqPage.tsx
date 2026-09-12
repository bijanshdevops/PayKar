import { useState } from 'react';

const faqs = [
  {
    q: 'چطور بدون ثبت‌نام می‌توانم آگهی‌ها را ببینم؟',
    a: 'مرور و جست‌وجوی آگهی‌های شغلی برای همه آزاد است. فقط برای ثبت درخواست استخدام یا انتشار آگهی نیاز به ورود دارید.'
  },
  {
    q: 'ورود به سایت چطور انجام می‌شود؟',
    a: 'ورود صرفاً با شماره موبایل و یک کد تایید ۶ رقمی (OTP) پیامکی انجام می‌شود؛ نیازی به رمز عبور نیست.'
  },
  {
    q: 'چگونه وضعیت درخواست استخدامم را پیگیری کنم؟',
    a: 'بعد از ارسال درخواست، یک کد پیگیری دریافت می‌کنید. از صفحه «پیگیری درخواست استخدام» با وارد کردن این کد می‌توانید وضعیت را ببینید.'
  },
  {
    q: 'ارتقای آگهی یعنی چه و چه فایده‌ای دارد؟',
    a: 'ارتقای آگهی باعث افزایش مدت نمایش و اولویت بیشتر آگهی در نتایج جست‌وجو می‌شود و از طریق درگاه زرین‌پال قابل خرید است.'
  },
  {
    q: 'چطور شرکتم را ثبت و تایید کنم؟',
    a: 'پس از ورود با شماره موبایل، از پنل شرکت پروفایل و مدارک هویتی را تکمیل کنید. تیم ادمین درخواست را بررسی و تایید یا رد می‌کند.'
  }
];

export default function FaqPage() {
  const [openIndex, setOpenIndex] = useState<number | null>(0);

  return (
    <div className="container-narrow" style={{ paddingTop: 'var(--space-5)', paddingBottom: 'var(--space-5)' }}>
      <h1>سوالات متداول</h1>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}>
        {faqs.map((item, index) => {
          const isOpen = openIndex === index;
          return (
            <div key={item.q} className="card">
              <button
                onClick={() => setOpenIndex(isOpen ? null : index)}
                style={{
                  all: 'unset',
                  cursor: 'pointer',
                  display: 'flex',
                  justifyContent: 'space-between',
                  width: '100%',
                  fontWeight: 600
                }}
              >
                <span>{item.q}</span>
                <span>{isOpen ? '−' : '+'}</span>
              </button>
              {isOpen && <p className="text-muted" style={{ marginTop: 'var(--space-2)', marginBottom: 0 }}>{item.a}</p>}
            </div>
          );
        })}
      </div>
    </div>
  );
}
