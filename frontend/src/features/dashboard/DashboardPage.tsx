import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useAppSelector } from '@/app/hooks';
import CompanyDashboardHomePage from '@/features/dashboard/CompanyDashboardHomePage';
import CandidateDashboardHomePage from '@/features/dashboard/CandidateDashboardHomePage';

const roleLabels: Record<string, string> = {
  Admin: 'مدیر پلتفرم',
  CompanyManager: 'مدیر شرکت',
  Candidate: 'کارجو'
};

/**
 * نمای کلی داشبورد — طبق تسک #97: برای نقش‌های شرکت/کارجو اکنون یک داشبورد غنی و اختصاصی
 * (تسک #95/#96) رندر می‌شود؛ برای بقیه (مثلاً فقط Admin) همان خوش‌آمدگویی و میان‌برهای ساده قبلی
 * باقی می‌ماند، چون نمای کامل پنل Owner جای دیگری (/admin/dashboard) از قبل وجود دارد.
 */
export default function DashboardPage() {
  const user = useAppSelector((state) => state.auth.user);

  if (user?.roles.includes('CompanyManager')) return <CompanyDashboardHomePage />;
  if (user?.roles.includes('Candidate')) return <CandidateDashboardHomePage />;

  const quickLinks = [
    { to: '/job-ads', label: 'مشاهده آگهی‌های شغلی' },
    ...(user?.roles.includes('CompanyManager') ? [{ to: '/company/job-ads', label: 'مدیریت آگهی‌های شرکت' }] : []),
    ...(user?.roles.includes('Candidate') ? [{ to: '/resume/view', label: 'پروفایل و رزومه من' }] : []),
    ...(user?.roles.includes('Admin') ? [{ to: '/admin/dashboard', label: 'داشبورد آمار پلتفرم' }] : []),
    { to: '/support', label: 'تیکت‌های پشتیبانی من' }
  ];

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.35 }}>
      <div className="card" style={{ marginBottom: 'var(--space-4)' }}>
        <h1 style={{ marginBottom: '0.25rem' }}>خوش آمدید</h1>
        <p className="text-muted" style={{ marginTop: 0 }}>شماره موبایل: {user?.mobileNumber}</p>

        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.4rem', marginTop: '0.75rem' }}>
          {user?.roles.map((role) => (
            <span
              key={role}
              className="badge"
              style={{ background: 'var(--color-primary-light)', color: 'var(--color-navy)' }}
            >
              {roleLabels[role] ?? role}
            </span>
          ))}
        </div>
      </div>

      <div className="card">
        <h2 style={{ marginTop: 0 }}>میان‌برهای سریع</h2>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.6rem' }}>
          {quickLinks.map((link) => (
            <Link key={link.to} to={link.to} className="btn-secondary" style={{ textDecoration: 'none' }}>
              {link.label}
            </Link>
          ))}
        </div>
      </div>
    </motion.div>
  );
}
