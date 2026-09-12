export default function ContactPage() {
  return (
    <div className="container-narrow" style={{ paddingTop: 'var(--space-5)', paddingBottom: 'var(--space-5)' }}>
      <h1>تماس با ما</h1>
      <p>
        برای طرح سوال، گزارش مشکل فنی، یا همکاری با پی کار می‌توانید از راه‌های
        زیر با تیم پشتیبانی در ارتباط باشید.
      </p>

      <div className="card" style={{ marginBottom: 'var(--space-4)' }}>
        <h3>پشتیبانی عمومی</h3>
        <p className="text-muted">برای کارجویان و پرسش‌های عمومی درباره آگهی‌ها و ثبت درخواست.</p>
        <p>ایمیل: support@example.com</p>
      </div>

      <div className="card" style={{ marginBottom: 'var(--space-4)' }}>
        <h3>واحد شرکت‌ها</h3>
        <p className="text-muted">برای ثبت‌نام شرکت، تایید هویت، و مسائل مربوط به انتشار آگهی.</p>
        <p>ایمیل: companies@example.com</p>
      </div>

      <div className="card">
        <h3>مسائل فنی و امنیتی</h3>
        <p className="text-muted">گزارش باگ، مشکل در پرداخت، یا مسائل امنیتی حساب کاربری.</p>
        <p>ایمیل: tech@example.com</p>
      </div>
    </div>
  );
}
