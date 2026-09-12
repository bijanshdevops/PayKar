import { Link } from 'react-router-dom';

export default function Footer() {
  const year = new Date().getFullYear();

  return (
    <footer className="site-footer">
      <div className="site-footer__inner">
        <div>
          <h4>پی کار</h4>
          <p style={{ color: '#94a3b8', fontSize: 'var(--fs-sm)' }}>
            اتصال کارجویان و شرکت‌های مستقر در شهرک‌های صنعتی سراسر کشور.
          </p>
        </div>

        <div>
          <h4>دسترسی سریع</h4>
          <Link to="/job-ads">آگهی‌های شغلی</Link>
          <Link to="/track-application">پیگیری درخواست استخدام</Link>
          <Link to="/login">ورود شرکت‌ها</Link>
        </div>

        <div>
          <h4>درباره ما</h4>
          <Link to="/about">درباره پلتفرم</Link>
          <Link to="/faq">سوالات متداول</Link>
          <Link to="/contact">تماس با ما</Link>
        </div>

        <div>
          <h4>قوانین</h4>
          <Link to="/terms">قوانین و مقررات</Link>
          <Link to="/privacy">حریم خصوصی</Link>
        </div>
      </div>

      <div className="site-footer__bottom">
        © {year} پی کار (PAYKAR). تمامی حقوق محفوظ است.
      </div>
    </footer>
  );
}
