import { Link } from 'react-router-dom';
import { useGetMyApplicationsQuery } from '@/features/candidates/candidateApi';
import { ApplicationStatusLabels } from '@/shared/enums';

function toPersianDigits(n: number) {
  return n.toLocaleString('fa-IR');
}

const statusBadgeColors: Record<string, { bg: string; fg: string }> = {
  Submitted: { bg: '#eef2ff', fg: '#3730a3' },
  Reviewed: { bg: '#fef9c3', fg: '#854d0e' },
  InterviewScheduled: { bg: '#e0f2fe', fg: '#075985' },
  Accepted: { bg: '#dcfce7', fg: '#166534' },
  Rejected: { bg: '#fee2e2', fg: '#991b1b' }
};

function KpiCard({ value, label }: { value: number; label: string }) {
  return (
    <div className="rounded-2xl border border-slate-100 bg-white p-4 shadow-sm">
      <div className="text-xl font-extrabold text-slate-800">{toPersianDigits(value)}</div>
      <div className="text-xs text-slate-500">{label}</div>
    </div>
  );
}

/**
 * صفحهٔ اختصاصی «درخواست‌های من» — طبق تصمیم صریح محصولی «قطع کامل وابستگی به رزومه‌ساز مرحله‌ای»:
 * جدول «پیگیری وضعیت درخواست‌های من» که پیش‌تر فقط داخل ResumeFormPage.tsx (مسیر منسوخ `/resume`)
 * قابل مشاهده بود، اکنون به یک صفحهٔ مستقل در `/applications` منتقل شده — داده و منطق از همان
 * useGetMyApplicationsQuery موجود می‌آید (بدون هیچ تغییر بک‌اند)، فقط ارائهٔ آن به یک صفحهٔ جدا با
 * طراحی Tailwind هماهنگ با بقیهٔ صفحات جدید داشبورد کارجو منتقل شده است.
 */
export default function CandidateApplicationsPage() {
  const { data, isLoading } = useGetMyApplicationsQuery();
  const applications = data?.data ?? [];

  const pendingCount = applications.filter(
    (a) => a.status === 'Submitted' || a.status === 'Reviewed' || a.status === 'InterviewScheduled'
  ).length;
  const acceptedCount = applications.filter((a) => a.status === 'Accepted').length;
  const rejectedCount = applications.filter((a) => a.status === 'Rejected').length;

  return (
    <div>
      <div className="mb-5">
        <h1 className="text-xl font-extrabold text-slate-800">درخواست‌های من</h1>
        <p className="text-sm text-slate-500">پیگیری وضعیت درخواست‌هایی که برای آگهی‌های شغلی ارسال کرده‌اید.</p>
      </div>

      {isLoading && <p className="text-sm text-slate-500">در حال بارگذاری...</p>}

      {!isLoading && (
        <>
          <div className="mb-5 grid grid-cols-2 gap-3 sm:grid-cols-4">
            <KpiCard value={applications.length} label="کل درخواست‌های ارسالی" />
            <KpiCard value={pendingCount} label="در حال بررسی" />
            <KpiCard value={acceptedCount} label="پذیرفته‌شده" />
            <KpiCard value={rejectedCount} label="ردشده" />
          </div>

          {applications.length === 0 ? (
            <div className="rounded-2xl border border-slate-100 bg-white p-8 text-center shadow-sm">
              <div className="mb-2 text-3xl">📨</div>
              <p className="text-sm font-medium text-slate-600">هنوز درخواستی ارسال نکرده‌اید.</p>
              <p className="mt-1 text-xs text-slate-400">پس از ارسال درخواست برای یک آگهی، وضعیت آن را اینجا پیگیری کنید.</p>
              <Link
                to="/job-ads"
                className="mt-4 inline-block rounded-xl bg-[#F59E0B] px-5 py-2.5 text-sm font-bold text-[#19263A] transition hover:bg-[#D97706]"
              >
                مشاهده آگهی‌های شغلی
              </Link>
            </div>
          ) : (
            <div className="overflow-x-auto rounded-2xl border border-slate-100 bg-white shadow-sm">
              <table className="w-full min-w-[560px] border-collapse text-sm">
                <thead>
                  <tr className="border-b border-slate-100 text-xs text-slate-500">
                    <th className="px-4 py-3 text-start font-semibold">عنوان آگهی</th>
                    <th className="px-4 py-3 text-start font-semibold">وضعیت</th>
                    <th className="px-4 py-3 text-start font-semibold">تاریخ ارسال</th>
                    <th className="px-4 py-3" />
                  </tr>
                </thead>
                <tbody>
                  {applications.map((app) => {
                    const colors = statusBadgeColors[app.status] ?? { bg: '#f3f4f6', fg: '#374151' };
                    return (
                      <tr key={app.id} className="border-b border-slate-50 last:border-b-0">
                        <td className="px-4 py-3 font-medium text-slate-700">{app.jobAdTitle}</td>
                        <td className="px-4 py-3">
                          <span className="rounded-full px-2.5 py-1 text-xs font-medium" style={{ background: colors.bg, color: colors.fg }}>
                            {ApplicationStatusLabels[app.status] ?? app.status}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-slate-500">{new Date(app.createdAtUtc).toLocaleDateString('fa-IR')}</td>
                        <td className="px-4 py-3 text-end">
                          <Link to={`/job-ads/${app.jobAdId}`} className="text-xs font-semibold text-emerald-600 no-underline hover:text-emerald-700">
                            مشاهده آگهی ←
                          </Link>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}
    </div>
  );
}
