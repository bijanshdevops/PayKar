import { Link } from 'react-router-dom';
import { useGetRecentApplicantsForCompanyQuery, useUpdateApplicationStatusMutation } from '@/features/applications/applicationApi';
import { ApplicationStatusLabels } from '@/shared/enums';
import PersianDate from '@/shared/components/PersianDate';

const nextStatusOptions: Record<string, string[]> = {
  Submitted: ['Reviewed', 'Rejected'],
  Reviewed: ['InterviewScheduled', 'Rejected'],
  InterviewScheduled: ['Accepted', 'Rejected'],
  Accepted: [],
  Rejected: []
};

const statusBadgeColors: Record<string, { bg: string; fg: string }> = {
  Submitted: { bg: '#eef2ff', fg: '#3730a3' },
  Reviewed: { bg: '#fef9c3', fg: '#854d0e' },
  InterviewScheduled: { bg: '#f3e8ff', fg: '#6b21a8' },
  Accepted: { bg: '#dcfce7', fg: '#166534' },
  Rejected: { bg: '#fee2e2', fg: '#991b1b' }
};

function matchScoreColor(score: number): { bg: string; fg: string } {
  if (score >= 75) return { bg: '#dcfce7', fg: '#166534' };
  if (score >= 60) return { bg: '#fef9c3', fg: '#854d0e' };
  return { bg: '#ffedd5', fg: '#9a3412' };
}

/**
 * صفحهٔ «رزومه‌های دریافتی» در داشبورد کارفرما — طبق 04_company_dashboard_spec.md بخش ۱ (آیتم منو)
 * و بخش ۴.۲ (فوتر ویجت «مشاهده همه رزومه‌ها»). لیست کامل متقاضیان همهٔ آگهی‌های شرکت جاری (نه فقط
 * یک آگهی خاص)، با استفاده از همان GetRecentApplicantsForCompanyQuery موجود ولی با شمارش بیشتر.
 */
export default function CompanyApplicantsPage() {
  const { data, isLoading } = useGetRecentApplicantsForCompanyQuery({ count: 100 });
  const [updateStatus] = useUpdateApplicationStatusMutation();
  const applicants = data?.data ?? [];

  return (
    <div>
      <div className="mb-5">
        <h1 className="text-xl font-extrabold text-slate-800">رزومه‌های دریافتی</h1>
        <p className="text-sm text-slate-500">تمام متقاضیان آگهی‌های شرکت شما، جدیدترین ابتدا.</p>
      </div>

      {isLoading && <p className="text-sm text-slate-500">در حال بارگذاری...</p>}
      {!isLoading && applicants.length === 0 && (
        <p className="rounded-2xl border border-slate-100 bg-white p-6 text-center text-sm text-slate-400 shadow-sm">
          هنوز هیچ رزومه‌ای برای آگهی‌های شما ارسال نشده است.
        </p>
      )}

      <div className="flex flex-col gap-3">
        {applicants.map((app) => {
          const statusColors = statusBadgeColors[app.status] ?? { bg: '#f3f4f6', fg: '#374151' };
          return (
            <div key={app.applicationId} className="rounded-2xl border border-slate-100 bg-white p-4 shadow-sm">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div className="flex items-start gap-3">
                  {app.candidateAvatarUrl ? (
                    <img
                      src={app.candidateAvatarUrl}
                      alt={app.candidateFullName}
                      className="h-12 w-12 shrink-0 rounded-full object-cover"
                    />
                  ) : (
                    <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-slate-100 text-lg font-bold text-slate-500">
                      {app.candidateFullName.charAt(0)}
                    </span>
                  )}
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-bold text-slate-800">{app.candidateFullName}</span>
                      {app.matchScorePercent !== null && (
                        <span
                          className="rounded-full px-2 py-0.5 text-[11px] font-semibold"
                          style={{ background: matchScoreColor(app.matchScorePercent).bg, color: matchScoreColor(app.matchScorePercent).fg }}
                        >
                          {app.matchScorePercent.toLocaleString('fa-IR')}٪ تطابق
                        </span>
                      )}
                    </div>
                    <div className="mt-0.5 text-xs text-slate-500">{app.candidateEducationLevel}</div>
                    <Link to={`/job-ads/${app.jobAdId}`} className="mt-1 inline-block text-xs text-emerald-600 hover:text-emerald-700">
                      برای آگهی: {app.jobAdTitle}
                    </Link>
                    <div className="mt-1 text-[11px] text-slate-400">
                      کد رهگیری: {app.trackingToken} — <PersianDate date={app.createdAtUtc} />
                    </div>
                  </div>
                </div>

                <div className="flex shrink-0 flex-col items-end gap-2">
                  <span className="rounded-full px-2.5 py-1 text-[11px] font-medium" style={{ background: statusColors.bg, color: statusColors.fg }}>
                    {ApplicationStatusLabels[app.status] ?? app.status}
                  </span>
                  {app.candidateResumeFileUrl && (
                    <a
                      href={app.candidateResumeFileUrl}
                      target="_blank"
                      rel="noreferrer"
                      className="rounded-lg bg-sky-50 px-3 py-1.5 text-xs font-semibold text-sky-700 hover:bg-sky-100"
                      style={{ textDecoration: 'none' }}
                    >
                      دانلود رزومه
                    </a>
                  )}
                </div>
              </div>

              <div className="mt-3 flex flex-wrap gap-2 border-t border-slate-100 pt-3">
                {(nextStatusOptions[app.status] ?? []).map((next) => (
                  <button
                    key={next}
                    className="rounded-lg px-3 py-1.5 text-xs font-semibold text-white"
                    style={{ background: next === 'Rejected' ? '#f43f5e' : '#19263a' }}
                    onClick={() => updateStatus({ applicationId: app.applicationId, newStatus: next })}
                  >
                    {ApplicationStatusLabels[next] ?? next}
                  </button>
                ))}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
