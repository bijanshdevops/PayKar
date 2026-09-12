import { useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  ArrowRight,
  Search,
  Phone,
  Mail,
  MapPin,
  GraduationCap,
  Briefcase,
  Award,
  Paperclip,
  Download,
  Calendar,
  Bell,
  BellOff,
  Loader2,
  CheckCircle2,
  Users,
  Inbox,
  ChevronRight,
  ChevronLeft
} from 'lucide-react';
import {
  useGetApplicationsForJobAdQuery,
  useUpdateApplicationStatusMutation,
  useDownloadApplicationResumeMutation
} from '@/features/applications/applicationApi';
import { useGetJobAdByIdQuery } from '@/features/jobAds/jobAdApi';
import type { EmployerApplicant } from '@/features/candidates/candidateApi';
import { ApplicationStatusLabels, JobAdStatusLabels } from '@/shared/enums';

/**
 * «مدیریت رزومه‌ها و متقاضیان» — بازطراحی Split-Panel/Master-Detail این صفحه طبق موکاپ نهایی، پس از
 * گسترش دامنه Candidate (فاز «مدیریت رزومه‌ها و متقاضیان» بخش دوم): ایمیل/شهر اکنون فیلدهای واقعی و
 * اختیاری روی Candidate هستند، و تحصیلات/سوابق شغلی می‌توانند به‌صورت ردیف‌های ساختاریافته (چندتایی)
 * نیز ثبت شده باشند — این صفحه اکنون آن‌ها را واقعاً نمایش می‌دهد (نه داده جعلی).
 *
 * انحرافات آگاهانه‌ی باقی‌مانده از موکاپ پیکسل‌به‌پیکسل:
 * - وقتی کارجو هنوز ردیف ساختاریافته تحصیلات/سوابق شغلی ثبت نکرده، این صفحه به فیلدهای آزاد قدیمی
 *   (EducationLevel / WorkExperienceSummary) برمی‌گردد — به‌جای نمایش بخش خالی یا داده ساختگی.
 * - دکمهٔ دانلود رزومه برچسب «(PDF)» ندارد چون نوع واقعی فایل متغیر است (docx/jpg/png نیز ممکن است).
 * - ۵ وضعیت واقعی JobApplicationStatus (Submitted/Reviewed/InterviewScheduled/Accepted/Rejected) در
 *   ۴ تب موکاپ گروه‌بندی شده‌اند (Submitted+Reviewed → «بررسی‌نشده»)، دقیقاً هم‌راستا با الگوی موفق
 *   گروه‌بندی وضعیت آگهی در فاز «آگهی‌های شرکت من».
 */

type SortKey = 'newest' | 'matchScore';

const SORT_OPTIONS: { key: SortKey; label: string }[] = [
  { key: 'newest', label: 'جدیدترین' },
  { key: 'matchScore', label: 'بیشترین تطابق' }
];

const PAGE_SIZE = 8;

type FilterKey = 'All' | 'Pending' | 'Interview' | 'Accepted' | 'Rejected';

const FILTER_GROUPS: { key: FilterKey; label: string; statuses: string[] }[] = [
  { key: 'All', label: 'همه', statuses: [] },
  { key: 'Pending', label: 'بررسی‌نشده', statuses: ['Submitted', 'Reviewed'] },
  { key: 'Interview', label: 'دعوت به مصاحبه', statuses: ['InterviewScheduled'] },
  { key: 'Accepted', label: 'پذیرفته‌شده', statuses: ['Accepted'] },
  { key: 'Rejected', label: 'رد شده', statuses: ['Rejected'] }
];

/** انتقال‌های مجاز — دقیقاً منطبق با JobApplication.AllowedTransitions در بک‌اند. */
const NEXT_STATUS_OPTIONS: Record<string, string[]> = {
  Submitted: ['Reviewed', 'Rejected'],
  Reviewed: ['InterviewScheduled', 'Rejected'],
  InterviewScheduled: ['Accepted', 'Rejected'],
  Accepted: [],
  Rejected: []
};

const STATUS_META: Record<string, { badgeBg: string; badgeFg: string; dot: string }> = {
  Submitted: { badgeBg: 'bg-indigo-50', badgeFg: 'text-indigo-700', dot: 'bg-indigo-500' },
  Reviewed: { badgeBg: 'bg-amber-50', badgeFg: 'text-amber-700', dot: 'bg-amber-500' },
  InterviewScheduled: { badgeBg: 'bg-purple-50', badgeFg: 'text-purple-700', dot: 'bg-purple-500' },
  Accepted: { badgeBg: 'bg-emerald-50', badgeFg: 'text-emerald-700', dot: 'bg-emerald-500' },
  Rejected: { badgeBg: 'bg-rose-50', badgeFg: 'text-rose-600', dot: 'bg-rose-500' }
};

function matchScoreColor(score: number): { bg: string; fg: string } {
  if (score >= 75) return { bg: '#dcfce7', fg: '#166534' };
  if (score >= 60) return { bg: '#fef9c3', fg: '#854d0e' };
  return { bg: '#ffedd5', fg: '#9a3412' };
}

function StatusBadge({ status }: { status: string }) {
  const meta = STATUS_META[status] ?? STATUS_META.Submitted;
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full ${meta.badgeBg} ${meta.badgeFg} px-2.5 py-1 text-[11px] font-semibold`}>
      <span className={`h-1.5 w-1.5 rounded-full ${meta.dot}`} />
      {ApplicationStatusLabels[status] ?? status}
    </span>
  );
}

/** تبدیل ISO UTC به مقدار قابل‌قبول برای input[type=datetime-local] (زمان محلی مرورگر). */
function toDateTimeLocalValue(isoUtc: string): string {
  const d = new Date(isoUtc);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function ApplicantCard({ applicant, isSelected, onSelect }: { applicant: EmployerApplicant; isSelected: boolean; onSelect: () => void }) {
  const skillTags = (applicant.candidateSkills ?? '')
    .split(/[,،]/)
    .map((s) => s.trim())
    .filter(Boolean)
    .slice(0, 2);

  return (
    <button
      type="button"
      onClick={onSelect}
      className={`w-full rounded-2xl border p-4 text-right transition-colors ${
        isSelected ? 'border-emerald-500 bg-emerald-50/60 shadow-sm' : 'border-slate-200/80 bg-white hover:bg-slate-50'
      }`}
    >
      <div className="flex items-start gap-3">
        {applicant.candidateAvatarUrl ? (
          <img src={applicant.candidateAvatarUrl} alt={applicant.candidateFullName} className="h-11 w-11 shrink-0 rounded-full object-cover" />
        ) : (
          <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-slate-100 text-base font-bold text-slate-500">
            {applicant.candidateFullName.charAt(0)}
          </span>
        )}
        <div className="min-w-0 flex-1">
          <div className="flex items-center justify-between gap-2">
            <span className="truncate font-bold text-slate-800">{applicant.candidateFullName}</span>
            {applicant.matchScorePercent !== null && (
              <span
                className="shrink-0 rounded-full px-2 py-0.5 text-[10px] font-semibold"
                style={{ background: matchScoreColor(applicant.matchScorePercent).bg, color: matchScoreColor(applicant.matchScorePercent).fg }}
              >
                {applicant.matchScorePercent.toLocaleString('fa-IR')}٪ تطابق
              </span>
            )}
          </div>
          <div className="mt-0.5 text-[11px] text-slate-400">{new Date(applicant.createdAtUtc).toLocaleDateString('fa-IR')}</div>
          <div className="mt-2 flex flex-wrap items-center gap-1.5">
            <StatusBadge status={applicant.status} />
            {skillTags.map((tag) => (
              <span key={tag} className="rounded-full bg-slate-100 px-2 py-0.5 text-[10px] font-medium text-slate-500">
                {tag}
              </span>
            ))}
          </div>
        </div>
      </div>
    </button>
  );
}

function ApplicantDetailPanel({ applicant, onUpdated }: { applicant: EmployerApplicant; onUpdated: () => void }) {
  const [updateStatus, { isLoading: isSaving }] = useUpdateApplicationStatusMutation();
  const [downloadResume, { isLoading: isDownloading }] = useDownloadApplicationResumeMutation();

  const [status, setStatus] = useState(applicant.status);
  const [notes, setNotes] = useState(applicant.companyNotes ?? '');
  const [interviewAt, setInterviewAt] = useState(applicant.interviewDateTimeUtc ? toDateTimeLocalValue(applicant.interviewDateTimeUtc) : '');
  const [notify, setNotify] = useState(true);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [justSaved, setJustSaved] = useState(false);

  const statusOptions = [applicant.status, ...(NEXT_STATUS_OPTIONS[applicant.status] ?? [])];
  const skillTags = (applicant.candidateSkills ?? '')
    .split(/[,،]/)
    .map((s) => s.trim())
    .filter(Boolean);

  const markDirty = () => setJustSaved(false);

  const handleSave = async () => {
    setSaveError(null);
    setJustSaved(false);
    try {
      await updateStatus({
        applicationId: applicant.applicationId,
        newStatus: status,
        companyNotes: notes.trim() ? notes.trim() : null,
        interviewDateTimeUtc: interviewAt ? new Date(interviewAt).toISOString() : null,
        notifyCandidate: notify
      }).unwrap();
      setJustSaved(true);
      onUpdated();
    } catch (err) {
      const message = (err as { data?: { message?: string } })?.data?.message ?? 'ذخیره تغییرات با خطا مواجه شد.';
      setSaveError(message);
    }
  };

  const handleDownload = async () => {
    try {
      const { blob, fileName } = await downloadResume(applicant.applicationId).unwrap();
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
    } catch {
      // پیام خطا به‌صورت خودکار توسط toastMiddleware سراسری نمایش داده می‌شود.
    }
  };

  return (
    <div className="flex h-full flex-col rounded-2xl border border-slate-200/80 bg-white p-6">
      <div className="flex items-start gap-4 border-b border-slate-100 pb-5">
        {applicant.candidateAvatarUrl ? (
          <img src={applicant.candidateAvatarUrl} alt={applicant.candidateFullName} className="h-16 w-16 shrink-0 rounded-full object-cover" />
        ) : (
          <span className="flex h-16 w-16 shrink-0 items-center justify-center rounded-full bg-slate-100 text-xl font-bold text-slate-500">
            {applicant.candidateFullName.charAt(0)}
          </span>
        )}
        <div className="min-w-0 flex-1">
          <h2 className="text-lg font-bold text-slate-800">{applicant.candidateFullName}</h2>
          <div className="mt-1.5 flex flex-wrap items-center gap-3 text-xs text-slate-500">
            <a href={`tel:${applicant.candidateMobileNumber}`} className="flex items-center gap-1 hover:text-emerald-600">
              <Phone className="h-3.5 w-3.5" />
              {applicant.candidateMobileNumber}
            </a>
            {applicant.candidateEmail && (
              <a href={`mailto:${applicant.candidateEmail}`} className="flex items-center gap-1 hover:text-emerald-600">
                <Mail className="h-3.5 w-3.5" />
                {applicant.candidateEmail}
              </a>
            )}
            {applicant.candidateCity && (
              <span className="flex items-center gap-1">
                <MapPin className="h-3.5 w-3.5" />
                {applicant.candidateCity}
              </span>
            )}
            <span className="flex items-center gap-1">
              <GraduationCap className="h-3.5 w-3.5" />
              {applicant.candidateEducationLevel}
            </span>
          </div>
          <div className="mt-2 flex items-center gap-2">
            <StatusBadge status={applicant.status} />
            <span className="text-[11px] text-slate-400">کد رهگیری: {applicant.trackingToken}</span>
          </div>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto py-5">
        {applicant.candidateEducations.length > 0 && (
          <div className="mb-5">
            <h3 className="mb-1.5 flex items-center gap-1.5 text-sm font-bold text-slate-700">
              <GraduationCap className="h-4 w-4 text-emerald-600" />
              تحصیلات
            </h3>
            <div className="flex flex-col gap-2">
              {applicant.candidateEducations.map((edu) => (
                <div key={edu.id} className="rounded-xl bg-slate-50 p-3 text-sm text-slate-600">
                  <div className="font-semibold text-slate-700">{edu.degreeLevel} — {edu.fieldOfStudy}</div>
                  <div className="mt-0.5 text-xs text-slate-500">
                    {edu.institutionName}
                    {edu.graduationYear ? ` · ${edu.graduationYear.toLocaleString('fa-IR')}` : ''}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {applicant.candidateWorkExperiences.length > 0 ? (
          <div className="mb-5">
            <h3 className="mb-2 flex items-center gap-1.5 text-sm font-bold text-slate-700">
              <Briefcase className="h-4 w-4 text-emerald-600" />
              سوابق شغلی
            </h3>
            <ol className="relative border-r-2 border-emerald-100 pr-4">
              {applicant.candidateWorkExperiences.map((exp) => (
                <li key={exp.id} className="relative mb-4 last:mb-0">
                  <span className="absolute -right-[21px] top-1 h-3 w-3 rounded-full border-2 border-white bg-emerald-500 ring-2 ring-emerald-100" />
                  <div className="rounded-xl bg-slate-50 p-3">
                    <div className="font-semibold text-slate-700">{exp.jobTitle}</div>
                    <div className="mt-0.5 text-xs text-slate-500">{exp.companyName}</div>
                    <div className="mt-1 text-[11px] font-medium text-emerald-700">
                      {exp.startYear.toLocaleString('fa-IR')} تا {exp.endYear ? exp.endYear.toLocaleString('fa-IR') : 'اکنون'}
                    </div>
                    {exp.description && <p className="mt-1.5 whitespace-pre-line text-xs leading-6 text-slate-600">{exp.description}</p>}
                  </div>
                </li>
              ))}
            </ol>
          </div>
        ) : (
          applicant.candidateWorkExperienceSummary && (
            <div className="mb-5">
              <h3 className="mb-1.5 flex items-center gap-1.5 text-sm font-bold text-slate-700">
                <Briefcase className="h-4 w-4 text-emerald-600" />
                سوابق کاری
              </h3>
              <p className="whitespace-pre-line rounded-xl bg-slate-50 p-3 text-sm leading-7 text-slate-600">
                {applicant.candidateWorkExperienceSummary}
              </p>
            </div>
          )
        )}

        {skillTags.length > 0 && (
          <div className="mb-5">
            <h3 className="mb-1.5 flex items-center gap-1.5 text-sm font-bold text-slate-700">
              <Award className="h-4 w-4 text-emerald-600" />
              مهارت‌ها
            </h3>
            <div className="flex flex-wrap gap-1.5">
              {skillTags.map((tag) => (
                <span key={tag} className="rounded-full bg-emerald-50 px-2.5 py-1 text-[11px] font-medium text-emerald-700">
                  {tag}
                </span>
              ))}
            </div>
          </div>
        )}

        {applicant.candidateResumeFileUrl ? (
          <button
            type="button"
            onClick={handleDownload}
            disabled={isDownloading}
            className="mb-6 inline-flex items-center gap-2 rounded-xl bg-sky-50 px-4 py-2.5 text-sm font-semibold text-sky-700 transition-colors hover:bg-sky-100 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isDownloading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Paperclip className="h-4 w-4" />}
            {isDownloading ? 'در حال دریافت فایل...' : 'دانلود فایل رزومه'}
            {!isDownloading && <Download className="h-3.5 w-3.5" />}
          </button>
        ) : (
          <p className="mb-6 text-xs text-slate-400">این متقاضی فایل رزومه‌ای بارگذاری نکرده است.</p>
        )}

        <div className="rounded-2xl border border-slate-200/80 bg-slate-50/60 p-4">
          <h3 className="mb-3 text-sm font-bold text-slate-700">تغییر وضعیت متقاضی</h3>

          <label className="mb-1.5 block text-xs font-medium text-slate-500">وضعیت</label>
          <select
            value={status}
            onChange={(e) => {
              setStatus(e.target.value);
              markDirty();
            }}
            className="mb-3 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 focus:border-emerald-500 focus:outline-none"
          >
            {statusOptions.map((s) => (
              <option key={s} value={s}>
                {ApplicationStatusLabels[s] ?? s}
              </option>
            ))}
          </select>

          <label className="mb-1.5 block text-xs font-medium text-slate-500">یادداشت داخلی (فقط برای تیم شما)</label>
          <textarea
            value={notes}
            onChange={(e) => {
              setNotes(e.target.value);
              markDirty();
            }}
            rows={3}
            placeholder="مثلاً: تجربه خوب در حوزه جوشکاری، برای مصاحبه حضوری مناسب است."
            className="mb-3 w-full resize-none rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 focus:border-emerald-500 focus:outline-none"
          />

          <label className="mb-1.5 flex items-center gap-1.5 text-xs font-medium text-slate-500">
            <Calendar className="h-3.5 w-3.5" />
            زمان مصاحبه (اختیاری)
          </label>
          <input
            type="datetime-local"
            value={interviewAt}
            onChange={(e) => {
              setInterviewAt(e.target.value);
              markDirty();
            }}
            className="mb-3 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 focus:border-emerald-500 focus:outline-none"
          />

          <label className="mb-4 flex cursor-pointer items-center gap-2 text-xs font-medium text-slate-600">
            <input
              type="checkbox"
              checked={notify}
              onChange={(e) => {
                setNotify(e.target.checked);
                markDirty();
              }}
              className="h-4 w-4 rounded border-slate-300 text-emerald-600 focus:ring-emerald-500"
            />
            {notify ? <Bell className="h-3.5 w-3.5 text-emerald-600" /> : <BellOff className="h-3.5 w-3.5 text-slate-400" />}
            ارسال خودکار پیامک اطلاع‌رسانی به کارجو (ملی‌پیامک)
          </label>

          {saveError && <p className="mb-3 text-xs font-medium text-rose-600">{saveError}</p>}

          <button
            type="button"
            onClick={handleSave}
            disabled={isSaving}
            className="flex w-full items-center justify-center gap-2 rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-bold text-white transition-colors hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSaving ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                در حال ذخیره...
              </>
            ) : justSaved ? (
              <>
                <CheckCircle2 className="h-4 w-4" />
                ذخیره شد
              </>
            ) : (
              'ذخیره تغییرات'
            )}
          </button>
        </div>
      </div>
    </div>
  );
}

export default function CompanyApplicationsPage() {
  // شناسه آگهی از پارامتر مسیر (/company/job-ads/:jobAdId/applications) خوانده می‌شود.
  const { jobAdId } = useParams<{ jobAdId: string }>();
  const { data: jobAdData } = useGetJobAdByIdQuery(jobAdId ?? '', { skip: !jobAdId });
  const { data, isLoading, refetch } = useGetApplicationsForJobAdQuery(
    { jobAdId: jobAdId ?? '', page: 1, pageSize: 100 },
    { skip: !jobAdId }
  );

  const [activeFilter, setActiveFilter] = useState<FilterKey>('All');
  const [searchTerm, setSearchTerm] = useState('');
  const [sortKey, setSortKey] = useState<SortKey>('newest');
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [page, setPage] = useState(1);

  const applicants = useMemo(() => data?.data?.items ?? [], [data]);
  const jobAd = jobAdData?.data;

  const activeGroup = FILTER_GROUPS.find((g) => g.key === activeFilter) ?? FILTER_GROUPS[0];
  const statusFiltered = activeGroup.statuses.length === 0 ? applicants : applicants.filter((a) => activeGroup.statuses.includes(a.status));

  const term = searchTerm.trim().toLowerCase();
  const searchFiltered = term
    ? statusFiltered.filter(
        (a) => a.candidateFullName.toLowerCase().includes(term) || (a.candidateSkills ?? '').toLowerCase().includes(term)
      )
    : statusFiltered;

  const sortedApplicants = useMemo(() => {
    const items = [...searchFiltered];
    if (sortKey === 'matchScore') {
      items.sort((a, b) => (b.matchScorePercent ?? -1) - (a.matchScorePercent ?? -1));
    } else {
      items.sort((a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime());
    }
    return items;
  }, [searchFiltered, sortKey]);

  const totalPages = Math.max(1, Math.ceil(sortedApplicants.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const visibleApplicants = sortedApplicants.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const effectiveSelectedId = selectedId ?? visibleApplicants[0]?.applicationId ?? sortedApplicants[0]?.applicationId ?? null;
  const selectedApplicant = applicants.find((a) => a.applicationId === effectiveSelectedId) ?? null;

  const handleFilterChange = (key: FilterKey) => {
    setActiveFilter(key);
    setPage(1);
  };

  const handleSearchChange = (value: string) => {
    setSearchTerm(value);
    setPage(1);
  };

  if (!jobAdId) {
    return (
      <div className="mx-auto flex max-w-7xl flex-col items-center justify-center rounded-2xl border border-dashed border-slate-200 bg-white py-16 text-center">
        <Inbox className="mb-3 h-9 w-9 text-slate-300" />
        <p className="font-semibold text-slate-600">آگهی مورد نظر مشخص نیست.</p>
        <Link to="/company/job-ads" className="mt-3 text-sm font-medium text-emerald-600 hover:text-emerald-700">
          بازگشت به لیست آگهی‌ها
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto flex h-full max-w-7xl flex-col">
      <div className="mb-5">
        <Link to="/company/job-ads" className="mb-3 inline-flex items-center gap-1.5 text-sm font-medium text-slate-500 hover:text-emerald-600">
          <ArrowRight className="h-4 w-4" />
          بازگشت به لیست آگهی‌ها
        </Link>

        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-xl font-extrabold text-slate-800 sm:text-2xl">{jobAd?.title ?? 'مدیریت رزومه‌ها و متقاضیان'}</h1>
          {jobAd && (
            <span className="rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700">
              {JobAdStatusLabels[jobAd.status] ?? jobAd.status}
            </span>
          )}
          <span className="flex items-center gap-1.5 rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">
            <Users className="h-3.5 w-3.5" />
            {applicants.length.toLocaleString('fa-IR')} رزومه دریافتی
          </span>
        </div>

        <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex w-full flex-col gap-2 sm:flex-row sm:items-center">
            <div className="relative w-full sm:max-w-xs">
              <Search className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input
                value={searchTerm}
                onChange={(e) => handleSearchChange(e.target.value)}
                placeholder="جست‌وجو بر اساس نام یا مهارت..."
                className="w-full rounded-xl border border-slate-200 bg-white py-2.5 pl-3 pr-9 text-sm text-slate-700 focus:border-emerald-500 focus:outline-none"
              />
            </div>

            <select
              value={sortKey}
              onChange={(e) => {
                setSortKey(e.target.value as SortKey);
                setPage(1);
              }}
              className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-700 focus:border-emerald-500 focus:outline-none sm:w-auto"
            >
              {SORT_OPTIONS.map((opt) => (
                <option key={opt.key} value={opt.key}>مرتب‌سازی: {opt.label}</option>
              ))}
            </select>
          </div>

          <div className="flex flex-wrap gap-2">
            {FILTER_GROUPS.map((filter) => {
              const count = filter.statuses.length === 0 ? applicants.length : applicants.filter((a) => filter.statuses.includes(a.status)).length;
              const isActive = activeFilter === filter.key;
              return (
                <button
                  key={filter.key}
                  type="button"
                  onClick={() => handleFilterChange(filter.key)}
                  className={`rounded-full border px-3.5 py-1.5 text-xs font-semibold transition-colors ${
                    isActive ? 'border-emerald-600 bg-emerald-600 text-white shadow-sm' : 'border-slate-200 bg-white text-slate-500 hover:bg-slate-100'
                  }`}
                >
                  {filter.label}
                  <span className={`mr-1 ${isActive ? 'text-emerald-100' : 'text-slate-400'}`}>({count.toLocaleString('fa-IR')})</span>
                </button>
              );
            })}
          </div>
        </div>
      </div>

      {isLoading && <p className="text-sm text-slate-400">در حال بارگذاری...</p>}

      {!isLoading && applicants.length === 0 && (
        <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-slate-200 bg-white py-16 text-center">
          <Inbox className="mb-3 h-9 w-9 text-slate-300" />
          <p className="font-semibold text-slate-600">هنوز رزومه‌ای برای این موقعیت شغلی ثبت نشده است.</p>
          <p className="mt-1 text-sm text-slate-400">به‌محض ارسال درخواست توسط کارجویان، در همین صفحه نمایش داده می‌شود.</p>
        </div>
      )}

      {!isLoading && applicants.length > 0 && (
        <div className="flex flex-1 flex-col gap-4 lg:flex-row" dir="rtl">
          <div className="flex w-full flex-col gap-2.5 lg:w-[360px] lg:shrink-0">
            {visibleApplicants.length === 0 && (
              <p className="rounded-2xl border border-dashed border-slate-200 bg-white p-6 text-center text-xs text-slate-400">
                متقاضی‌ای با این مشخصات یافت نشد.
              </p>
            )}
            {visibleApplicants.map((applicant) => (
              <ApplicantCard
                key={applicant.applicationId}
                applicant={applicant}
                isSelected={applicant.applicationId === effectiveSelectedId}
                onSelect={() => setSelectedId(applicant.applicationId)}
              />
            ))}

            {totalPages > 1 && (
              <div className="mt-1 flex items-center justify-between rounded-2xl border border-slate-200/80 bg-white px-3 py-2">
                <button
                  type="button"
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={currentPage <= 1}
                  className="flex items-center gap-1 rounded-lg px-2 py-1 text-xs font-semibold text-slate-500 transition-colors hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-40"
                >
                  <ChevronRight className="h-3.5 w-3.5" />
                  قبلی
                </button>
                <span className="text-[11px] font-medium text-slate-400">
                  صفحه {currentPage.toLocaleString('fa-IR')} از {totalPages.toLocaleString('fa-IR')}
                </span>
                <button
                  type="button"
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  disabled={currentPage >= totalPages}
                  className="flex items-center gap-1 rounded-lg px-2 py-1 text-xs font-semibold text-slate-500 transition-colors hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-40"
                >
                  بعدی
                  <ChevronLeft className="h-3.5 w-3.5" />
                </button>
              </div>
            )}
          </div>

          <div className="min-w-0 flex-1">
            {selectedApplicant ? (
              <ApplicantDetailPanel key={selectedApplicant.applicationId} applicant={selectedApplicant} onUpdated={refetch} />
            ) : (
              <div className="flex h-full items-center justify-center rounded-2xl border border-dashed border-slate-200 bg-white p-10 text-center text-sm text-slate-400">
                برای مشاهده جزئیات، یک متقاضی را از لیست انتخاب کنید.
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
