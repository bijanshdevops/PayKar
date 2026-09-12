import { Link } from 'react-router-dom';

export default function NotFoundPage() {
  return (
    <div className="container-narrow" style={{ textAlign: 'center', paddingTop: 'var(--space-6)', paddingBottom: 'var(--space-6)' }}>
      <h1 style={{ fontSize: 'var(--fs-3xl)' }}>۴۰۴</h1>
      <p>صفحه‌ای که دنبالش بودید پیدا نشد.</p>
      <Link to="/" className="btn-primary" style={{ textDecoration: 'none', display: 'inline-block' }}>
        بازگشت به صفحه اصلی
      </Link>
    </div>
  );
}
