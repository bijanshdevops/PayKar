import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useGetMyResumeQuery, type Candidate } from '@/features/candidates/candidateApi';
import { useGetMyApplicationsQuery, useGetMyBookmarkIdsQuery } from '@/features/candidates/candidateApi';
import { useSearchJobAdsQuery, useToggleBookmarkMutation } from '@/features/jobAds/jobAdApi';
import { useGetMySupportTicketsQuery } from '@/features/support/supportApi';
import { useZoneLabelMap } from '@/features/geography/useZoneLabel';
import { ApplicationStatusLabels } from '@/shared/enums';
import JobAdCard from '@/features/jobAds/JobAdCard';

const statusBadgeColors: Record<string, { bg: string; fg: string }> = {
  Submitted: { bg: '#eef2ff', fg: '#3730a3' },
  Reviewed: { bg: '#fef9c3', fg: '#854d0e' },
  InterviewScheduled: { bg: '#e0f2fe', fg: '#075985' },
  Accepted: { bg: '#dcfce7', fg: '#166534' },
  Rejected: { bg: '#fee2e2', fg: '#991b1b' }
};

function KpiCard({ icon, value, label }: { icon: string; value: string; label: string }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
      className="flex items-center gap-3 rounded-2xl border border-slate-100 bg-white p-4 shadow-sm"
    >
      <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-emerald-50 text-xl">{icon}</span>
      <div className="min-w-0">
        <div className="truncate text-xl font-extrabold text-slate-800">{value}</div>
        <div className="text-xs text-slate-500">{label}</div>
      </div>
    </motion.div>
  );
}

/**
 * درصد تکمیل رزومه — از روی پر بودن واقعی فیلدهای پروفایل، بدون هیچ مقدار ساختگی.
 * طبق درخواست اصلاحی محصولی:
 * ۱. «مهارت‌ها» اکنون هم از فهرست ساختاریافتهٔ جدید (structuredSkills — قابل‌تکمیل در
 *    CandidateProfileEdit.tsx) و هم از فیلد متنی آزاد قدیمی (skills) پذیرفته می‌شود؛ هر کدام که
 *    پر باشد کافی است، تا پروفایلی که فقط از صفحات جدید ساخته شده به‌ناحق «ناقص» نشان داده نشود.
 * ۲. «پاسخ‌های آزمون روان‌شناسی» (psychologyAnswers) از مخرج این محاسبه حذف شده — طبق تصمیم صریح
 *    محصولی، آن آزمون به‌صورت یک ماژول ارزیابی مستقل به اسپرینت بعد موکول شده و دیگر بخشی از فرم
 *    اصلی نیست؛ بنابراین نباید مانع رسیدن یک پروفایل کاملاً مدرن و کامل به ۱۰۰٪ شود.
 * ۳. به همین دلیل، «علاقه‌مندی‌ها» (interests) هم از مخرج حذف شده: این فیلد هم مثل psychologyAnswers
 *    یک فیلد متنی آزاد قدیمی است که هیچ معادل ساختاریافته‌ای در CandidateProfileEdit.tsx ندارد و
 *    هرگز از صفحات جدید قابل‌ویرایش نیست؛ نگه‌داشتنش در محاسبه با هدف «۱۰۰٪ طبیعی برای پروفایل
 *    کاملاً مدرن» در تضاد بود.
 */
function calcCompletionPercent(resume: Pick<Candidate, 'fullName' | 'educationLevel' | 'workExperienceSummary' | 'skills' | 'structuredSkills'> | null | undefined): number {
  if (!resume) return 0;
  const hasText = (value: string | null | undefined) => !!value && value.trim().length > 0;
  const hasSkills = (resume.structuredSkills && resume.structuredSkills.length > 0) || hasText(resume.skills);
  const fields = [hasText(resume.fullName), hasText(resume.educationLevel), hasText(resume.workExperienceSummary), hasSkills];
  const filled = fields.filter(Boolean).length;
  return Math.round((filled / fields.length) * 100);
}

/**
 * نمای کلی داشبورد کارجو — طبق تسک #96. تمام ارقام از دادهٔ واقعی خوانده می‌شود:
 * وضعیت درخواست‌ها از getMyApplications، درصد تکمیل رزومه از فیلدهای واقعی پروفایل،
 * «پیشنهادهای شغلی» از آخرین آگهی‌های منتشرشده (بدون موتور توصیه‌گر جعلی).
 */
export default function CandidateDashboardHomePage() {
  const { data: resumeData } = useGetMyResumeQuery();
  const resume = resumeData?.data;
  const { data: applicationsData } = useGetMyApplicationsQuery();
  const applications = applicationsData?.data ?? [];
  const { data: ticketsData } = useGetMySupportTicketsQuery({ page: 1, pageSize: 50 });
  const openTicketsCount = (ticketsData?.data?.items ?? []).filter((t) => t.status !== 'Closed').length;
  const { data: recommendedData } = useSearchJobAdsQuery({ page: 1, pageSize: 3, sortBy: 'createdAtUtc', sortDir: 'desc' });
  const recommendedAds = recommendedData?.data?.items ?? [];
  const zoneLabelById = useZoneLabelMap();
  const { data: bookmarkIdsData } = useGetMyBookmarkIdsQuery();
  const bookmarkedIds = new Set(bookmarkIdsData?.data ?? []);
  const [toggleBookmark] = useToggleBookmarkMutation();

  const pendingCount = applications.filter((a) => a.status === 'Submitted' || a.status === 'Reviewed' || a.status === 'InterviewScheduled').length;
  const acceptedCount = applications.filter((a) => a.status === 'Accepted').length;
  const completionPercent = calcCompletionPercent(resume);
  const recentApplications = [...applications]
    .sort((a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime())
    .slice(0, 4);

  return (
    <div>
      <div className="mb-5">
        <h1 className="text-xl font-extrabold text-slate-800">داشبورد کارجو</h1>
        <p className="text-sm text-slate-500">{resume && resume.fullName ? `خوش آمدید، ${resume.fullName}` : 'خوش آمدید'}</p>
      </div>

      <div className="mb-5 grid grid-cols-2 gap-3 sm:grid-cols-4">
        <KpiCard icon="🎫" value={openTicketsCount.toLocaleString('fa-IR')} label="تیکت‌های فعال" />
        <KpiCard icon="📨" value={applications.length.toLocaleString('fa-IR')} label="درخواست‌های ارسالی" />
        <KpiCard icon="⏳" value={pendingCount.toLocaleString('fa-IR')} label="در حال بررسی" />
        <KpiCard icon="🎉" value={acceptedCount.toLocaleString('fa-IR')} label="پذیرفته‌شده" />
      </div>

      <div className="mb-5 rounded-2xl border border-emerald-100 bg-emerald-50/60 p-5">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-white text-2xl shadow-sm">
              {resume?.isFeePaid ? '✅' : '📝'}
            </span>
            <div>
              <div className="font-bold text-slate-800">
                {resume ? 'رزومه‌ساز حرفه‌ای آنلاین' : 'هنوز رزومه‌ای نساخته‌اید'}
              </div>
              <div className="text-xs text-slate-500">
                {resume
                  ? resume.isFeePaid
                    ? 'رزومه شما نهایی شده — ویرایش‌های بعدی رایگان است.'
                    : 'رزومه شما به‌صورت پیش‌نویس ذخیره شده — برای نهایی‌سازی هزینه ساخت را پرداخت کنید.'
                  : 'با ساخت رزومه حرفه‌ای، شانس دیده‌شدن توسط کارفرمایان را افزایش دهید.'}
              </div>
            </div>
          </div>
          <Link
            // طبق تصمیم صریح محصولی «قطع کامل وابستگی به رزومه‌ساز مرحله‌ای»: CandidateProfileEdit.tsx
            // اکنون خودش می‌تواند اولین رزومه را هم بسازد (وضعیت نظام‌وظیفه/مدرک تحصیلی هم آنجاست)،
            // پس دیگر لازم نیست کارجوی تازه به `/resume` هدایت شود.
            to={resume ? '/resume/view' : '/resume/edit'}
            className="rounded-xl bg-[#F59E0B] px-5 py-2.5 text-sm font-bold text-[#19263A] transition hover:bg-[#D97706]"
            style={{ textDecoration: 'none' }}
          >
            {resume ? 'ادامه رزومه‌ساز' : 'ساخت رزومه حرفه‌ای'}
          </Link>
        </div>

        {resume && (
          <div className="mt-4">
            <div className="mb-1 flex items-center justify-between text-xs text-slate-500">
              <span>درصد تکمیل پروفایل</span>
              <span>{completionPercent.toLocaleString('fa-IR')}٪ تکمیل شده</span>
            </div>
            <div className="h-2 w-full overflow-hidden rounded-full bg-white">
              <div className="h-full rounded-full bg-emerald-500 transition-all" style={{ width: `${completionPercent}%` }} />
            </div>
          </div>
        )}
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-[1.3fr_1fr]">
        <div className="rounded-2xl border border-slate-100 bg-white p-4 shadow-sm">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-sm font-bold text-slate-700">پیشنهادهای شغلی برای شما</h2>
            <Link to="/job-ads" className="text-xs font-medium text-emerald-600 hover:text-emerald-700">مشاهده همه ←</Link>
          </div>
          {recommendedAds.length === 0 && <p className="text-sm text-slate-400">در حال حاضر آگهی فعالی وجود ندارد.</p>}
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            {recommendedAds.map((ad) => (
              <JobAdCard
                key={ad.id}
                ad={ad}
                locationLabel={zoneLabelById.get(ad.industrialZoneId)}
                isBookmarked={bookmarkedIds.has(ad.id)}
                onToggleBookmark={(id) => toggleBookmark(id)}
              />
            ))}
          </div>
        </div>

        <div className="rounded-2xl border border-slate-100 bg-white p-4 shadow-sm">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-sm font-bold text-slate-700">آخرین درخواست‌های ارسالی</h2>
            {/* طبق تصمیم صریح محصولی «قطع کامل وابستگی به رزومه‌ساز مرحله‌ای»: جدول کامل پیگیری
                درخواست‌ها اکنون در صفحهٔ اختصاصی CandidateApplicationsPage.tsx (مسیر /applications) است. */}
            <Link to="/applications" className="text-xs font-medium text-emerald-600 hover:text-emerald-700">مشاهده همه ←</Link>
          </div>
          {recentApplications.length === 0 && <p className="text-sm text-slate-400">هنوز درخواستی ارسال نکرده‌اید.</p>}
          <div className="flex flex-col divide-y divide-slate-100">
            {recentApplications.map((app) => {
              const colors = statusBadgeColors[app.status] ?? { bg: '#f3f4f6', fg: '#374151' };
              return (
                <div key={app.id} className="flex items-center justify-between gap-2 py-3">
                  <div className="min-w-0">
                    <div className="truncate text-sm font-semibold text-slate-700">{app.jobAdTitle}</div>
                    <div className="text-[11px] text-slate-400">{new Date(app.createdAtUtc).toLocaleDateString('fa-IR')}</div>
                  </div>
                  <span className="shrink-0 rounded-full px-2.5 py-1 text-[11px] font-medium" style={{ background: colors.bg, color: colors.fg }}>
                    {ApplicationStatusLabels[app.status] ?? app.status}
                  </span>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </div>
  );
}
