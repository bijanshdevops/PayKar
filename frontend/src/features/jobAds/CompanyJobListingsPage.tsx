import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Briefcase, Plus, Users, Hourglass, Clock3, Pencil, Trash2, Info, AlertTriangle, ArrowLeft } from 'lucide-react';
import {
  useGetMyCompanyJobAdsQuery,
  useCloseJobAdMutation,
  useDeleteJobAdMutation,
  type JobAd
} from '@/features/jobAds/jobAdApi';
import { useSubmitJobAdForReviewMutation, useGetJobAdListingFeeQuery } from '@/features/payments/paymentApi';
import { useGetMyCompanyDashboardQuery } from '@/features/companies/companyApi';
import { WorkShiftLabels, ContractTypeLabels, JobAdStatusLabels } from '@/shared/enums';
import JobAdFormModal from '@/features/jobAds/JobAdFormModal';

/**
 * «آگهی‌های شرکت من» — بازطراحی بصری تسک #92، اکنون کاملاً متصل به بک‌اند واقعی.
 * جایگزین CompanyJobAdsPage.tsx قدیمی (پوسته ساده بدون Tailwind). منطق پرداخت/ارسال برای بررسی
 * و فرم دومرحله‌ای بدون تغییر حفظ شده‌اند (فرم به JobAdFormModal.tsx منتقل شده است).
 */

type FilterKey = 'All' | 'Draft' | 'PendingReview' | 'Published' | 'Rejected' | 'Inactive';

const FILTER_GROUPS: { key: FilterKey; label: string; statuses: string[] }[] = [
  { key: 'All', label: 'همه', statuses: [] },
  { key: 'Draft', label: 'پیش‌نویس', statuses: ['Draft'] },
  { key: 'PendingReview', label: 'در انتظار تایید', statuses: ['PendingReview'] },
  { key: 'Published', label: 'منتشرشده', statuses: ['Published'] },
  { key: 'Rejected', label: 'ردشده', statuses: ['Rejected'] },
  { key: 'Inactive', label: 'غیرفعال', statuses: ['Expired', 'Closed', 'Archived'] }
];

const STATUS_META: Record<
  string,
  { badgeBg: string; badgeFg: string; dot: string; iconBg: string; iconFg: string; pulse: boolean }
> = {
  Draft: { badgeBg: 'bg-slate-100', badgeFg: 'text-slate-600', dot: 'bg-slate-400', iconBg: 'bg-slate-100', iconFg: 'text-slate-500', pulse: false },
  PendingReview: { badgeBg: 'bg-amber-50', badgeFg: 'text-amber-700', dot: 'bg-amber-500', iconBg: 'bg-amber-50', iconFg: 'text-amber-600', pulse: false },
  Published: { badgeBg: 'bg-emerald-50', badgeFg: 'text-emerald-700', dot: 'bg-emerald-500', iconBg: 'bg-emerald-50', iconFg: 'text-emerald-600', pulse: true },
  Rejected: { badgeBg: 'bg-rose-50', badgeFg: 'text-rose-600', dot: 'bg-rose-500', iconBg: 'bg-rose-50', iconFg: 'text-rose-500', pulse: false },
  Expired: { badgeBg: 'bg-rose-50', badgeFg: 'text-rose-600', dot: 'bg-rose-400', iconBg: 'bg-slate-100', iconFg: 'text-slate-500', pulse: false },
  Closed: { badgeBg: 'bg-slate-100', badgeFg: 'text-slate-500', dot: 'bg-slate-400', iconBg: 'bg-slate-100', iconFg: 'text-slate-500', pulse: false },
  Archived: { badgeBg: 'bg-slate-100', badgeFg: 'text-slate-500', dot: 'bg-slate-400', iconBg: 'bg-slate-100', iconFg: 'text-slate-500', pulse: false }
};

/** آگهی‌های حذف‌ناپذیر طبق قانون بک‌اند (JobAd.EnsureDeletable) — دکمه حذف را اصلاً نشان نمی‌دهیم. */
const NON_DELETABLE_STATUSES = new Set(['PendingReview', 'Published']);
/** آگهی‌هایی که هنوز هیچ‌گاه منتشر نشده‌اند — نمایش لینک «متقاضیان» برایشان معنا ندارد مگر واقعاً متقاضی داشته باشند. */

function daysUntil(dateIso: string): number {
  return Math.ceil((new Date(dateIso).getTime() - Date.now()) / (1000 * 60 * 60 * 24));
}

function StatusBadge({ status }: { status: string }) {
  const meta = STATUS_META[status] ?? STATUS_META.Draft;
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full ${meta.badgeBg} ${meta.badgeFg} px-3 py-1 text-xs font-semibold`}>
      <span className="relative flex h-1.5 w-1.5">
        {meta.pulse && <span className={`absolute inline-flex h-full w-full animate-ping rounded-full ${meta.dot} opacity-60`} />}
        <span className={`relative inline-flex h-1.5 w-1.5 rounded-full ${meta.dot}`} />
      </span>
      {JobAdStatusLabels[status] ?? status}
    </span>
  );
}

function StatusInfoPill({ ad }: { ad: JobAd }) {
  if (ad.status === 'Draft') {
    return (
      <span className="inline-flex w-full items-center justify-center gap-1.5 rounded-full bg-slate-100 px-3 py-2 text-xs font-semibold text-slate-500">
        {ad.isFeePaid ? 'هزینه پرداخت‌شده — آماده ارسال' : 'هنوز پرداخت و ارسال نشده'}
      </span>
    );
  }

  if (ad.status === 'PendingReview') {
    return (
      <span className="inline-flex w-full items-center justify-center gap-1.5 rounded-full bg-amber-50 px-3 py-2 text-xs font-semibold text-amber-700">
        <Clock3 className="h-3.5 w-3.5" />
        در صف بررسی کارشناسان
      </span>
    );
  }

  if (ad.status === 'Rejected') {
    return (
      <span className="inline-flex w-full items-center justify-center gap-1.5 rounded-full bg-rose-50 px-3 py-2 text-center text-xs font-semibold text-rose-600" title={ad.rejectionReason ?? undefined}>
        <AlertTriangle className="h-3.5 w-3.5 shrink-0" />
        <span className="truncate">{ad.rejectionReason ?? 'ردشده توسط ادمین'}</span>
      </span>
    );
  }

  if (ad.status === 'Published') {
    if (!ad.expiresAtUtc) {
      return (
        <span className="inline-flex w-full items-center justify-center gap-1.5 rounded-full bg-emerald-50 px-3 py-2 text-xs font-semibold text-emerald-700">
          منتشرشده
        </span>
      );
    }
    const remaining = daysUntil(ad.expiresAtUtc);
    if (remaining <= 0) {
      return (
        <span className="inline-flex w-full items-center justify-center gap-1.5 rounded-full bg-rose-50 px-3 py-2 text-xs font-semibold text-rose-600">
          در حال انقضا...
        </span>
      );
    }
    const urgent = remaining <= 5;
    return (
      <span
        className={`inline-flex w-full items-center justify-center gap-1.5 rounded-full px-3 py-2 text-xs font-semibold ${
          urgent ? 'bg-amber-50 text-amber-700' : 'bg-emerald-50 text-emerald-700'
        }`}
      >
        <Hourglass className="h-3.5 w-3.5" />
        {remaining.toLocaleString('fa-IR')} روز تا انقضا
      </span>
    );
  }

  if (ad.status === 'Closed') {
    return (
      <span className="inline-flex w-full items-center justify-center gap-1.5 rounded-full bg-slate-100 px-3 py-2 text-xs font-semibold text-slate-500">
        بسته‌شده توسط شرکت
      </span>
    );
  }

  // Expired / Archived
  return (
    <span className="inline-flex w-full items-center justify-center gap-1.5 rounded-full bg-rose-50 px-3 py-2 text-xs font-semibold text-rose-600">
      {JobAdStatusLabels[ad.status] ?? 'منقضی شده'}
    </span>
  );
}

function JobCard({
  ad,
  applicantsCount,
  feeAmountToman,
  onEdit,
  onDelete,
  onClose,
  onSubmitForReview,
  isSubmittingForReview,
  submitError
}: {
  ad: JobAd;
  applicantsCount: number;
  feeAmountToman: string;
  onEdit: (ad: JobAd) => void;
  onDelete: (id: string) => void;
  onClose: (id: string) => void;
  onSubmitForReview: (id: string) => void;
  isSubmittingForReview: boolean;
  submitError?: string;
}) {
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const meta = STATUS_META[ad.status] ?? STATUS_META.Draft;
  const tags = [WorkShiftLabels[ad.workShift] ?? ad.workShift, ContractTypeLabels[ad.contractType] ?? ad.contractType, ad.hasCommuteService ? 'دارای سرویس' : null].filter(
    (t): t is string => Boolean(t)
  );
  const canDelete = !NON_DELETABLE_STATUSES.has(ad.status);
  const canEdit = ad.status === 'Draft' || ad.status === 'Published' || ad.status === 'Rejected';
  const canSubmitForReview = ad.status === 'Draft' || ad.status === 'Rejected';

  return (
    <div className="flex h-full flex-col rounded-2xl border border-slate-200/80 bg-white p-5 shadow-sm transition-all hover:-translate-y-0.5 hover:shadow-md">
      <div className="mb-4 flex items-start justify-between">
        <StatusBadge status={ad.status} />
        <span className={`flex h-9 w-9 items-center justify-center rounded-xl ${meta.iconBg} ${meta.iconFg}`}>
          <Briefcase className="h-4 w-4" />
        </span>
      </div>

      <div className="flex flex-1 flex-col items-center text-center">
        <h3 className="mb-4 line-clamp-2 text-base font-bold text-slate-800">{ad.title}</h3>

        {tags.length > 0 && (
          <div className="mb-5 flex flex-wrap items-center justify-center gap-2">
            {tags.map((tag) => (
              <span key={tag} className="rounded-full bg-emerald-50 px-2.5 py-1 text-[11px] font-medium text-emerald-700">
                {tag}
              </span>
            ))}
          </div>
        )}

        {applicantsCount > 0 ? (
          <Link
            to={`/company/job-ads/${ad.id}/applications`}
            className="mb-5 mt-auto flex items-center gap-1.5 text-sm font-medium text-emerald-600 transition-colors hover:text-emerald-700"
          >
            <Users className="h-4 w-4" />
            {applicantsCount.toLocaleString('fa-IR')} متقاضی
            <ArrowLeft className="h-3.5 w-3.5" />
          </Link>
        ) : (
          <div className="mb-5 mt-auto flex items-center gap-1.5 text-sm text-slate-400">
            <Users className="h-4 w-4" />
            بدون متقاضی
          </div>
        )}

        <div className="mb-2 w-full">
          <StatusInfoPill ad={ad} />
        </div>
        {submitError && <p className="mt-1 text-[11px] font-medium text-rose-600">{submitError}</p>}
      </div>

      <div className="mt-3 flex flex-col gap-2">
        {canSubmitForReview && (
          <button
            type="button"
            disabled={isSubmittingForReview}
            onClick={() => onSubmitForReview(ad.id)}
            className="flex items-center justify-center gap-1.5 rounded-xl bg-emerald-600 px-3 py-2 text-sm font-semibold text-white transition-colors hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {ad.isFeePaid ? 'ارسال مجدد برای بررسی' : `پرداخت ${feeAmountToman} تومان و ارسال`}
          </button>
        )}

        {ad.status === 'Published' && (
          <button
            type="button"
            onClick={() => onClose(ad.id)}
            className="flex items-center justify-center gap-1.5 rounded-xl bg-sky-50 px-3 py-2 text-sm font-semibold text-sky-600 transition-colors hover:bg-sky-100"
          >
            بستن آگهی
          </button>
        )}

        {confirmingDelete ? (
          <div className="flex flex-col gap-2">
            <p className="text-center text-xs font-medium text-slate-500">این آگهی برای همیشه حذف می‌شود.</p>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => onDelete(ad.id)}
                className="flex flex-1 items-center justify-center gap-1.5 rounded-xl bg-rose-600 px-3 py-2 text-sm font-semibold text-white transition-colors hover:bg-rose-700"
              >
                بله، حذف شود
              </button>
              <button
                type="button"
                onClick={() => setConfirmingDelete(false)}
                className="flex flex-1 items-center justify-center gap-1.5 rounded-xl border border-slate-200 px-3 py-2 text-sm font-semibold text-slate-600 transition-colors hover:bg-slate-50"
              >
                انصراف
              </button>
            </div>
          </div>
        ) : (
          (canDelete || canEdit) && (
            <div className="flex gap-2">
              {canDelete && (
                <button
                  type="button"
                  onClick={() => setConfirmingDelete(true)}
                  className="flex flex-1 items-center justify-center gap-1.5 rounded-xl bg-rose-50 px-3 py-2 text-sm font-semibold text-rose-600 transition-colors hover:bg-rose-100"
                >
                  <Trash2 className="h-4 w-4" />
                  حذف
                </button>
              )}
              {canEdit && (
                <button
                  type="button"
                  onClick={() => onEdit(ad)}
                  className="flex flex-1 items-center justify-center gap-1.5 rounded-xl bg-emerald-50 px-3 py-2 text-sm font-semibold text-emerald-600 transition-colors hover:bg-emerald-100"
                >
                  <Pencil className="h-4 w-4" />
                  ویرایش
                </button>
              )}
            </div>
          )
        )}
      </div>
    </div>
  );
}

export default function CompanyJobListingsPage() {
  const { data, isLoading } = useGetMyCompanyJobAdsQuery({ page: 1, pageSize: 50 });
  const { data: dashboardData } = useGetMyCompanyDashboardQuery();
  const { data: feeData } = useGetJobAdListingFeeQuery();
  const [closeJobAd] = useCloseJobAdMutation();
  const [deleteJobAd] = useDeleteJobAdMutation();
  const [submitForReview, { isLoading: isSubmitting }] = useSubmitJobAdForReviewMutation();

  const [activeFilter, setActiveFilter] = useState<FilterKey>('All');
  const [modalState, setModalState] = useState<{ editingAd: JobAd | null } | null>(null);
  const [submitErrorByAdId, setSubmitErrorByAdId] = useState<Record<string, string>>({});

  const jobs = useMemo(() => data?.data?.items ?? [], [data]);
  const feeAmountToman = Math.round((feeData?.data?.amountInRials ?? 500_000) / 10).toLocaleString('fa-IR');

  const applicantsByJobAdId = useMemo(() => {
    const map = new Map<string, number>();
    for (const row of dashboardData?.data?.applicationsPerJobAd ?? []) {
      map.set(row.jobAdId, row.applicationCount);
    }
    return map;
  }, [dashboardData]);

  const activeGroup = FILTER_GROUPS.find((g) => g.key === activeFilter) ?? FILTER_GROUPS[0];
  const filteredJobs = activeGroup.statuses.length === 0 ? jobs : jobs.filter((ad) => activeGroup.statuses.includes(ad.status));

  const handleDelete = async (id: string) => {
    await deleteJobAd(id);
  };

  const handleClose = async (id: string) => {
    await closeJobAd(id);
  };

  const handleSubmitForReview = async (id: string) => {
    setSubmitErrorByAdId((prev) => ({ ...prev, [id]: '' }));
    const result = await submitForReview(id);

    if ('data' in result && result.data?.success) {
      const redirectUrl = result.data.data?.paymentRedirectUrl;
      if (redirectUrl) window.location.href = redirectUrl;
      return;
    }

    const errorResponse = 'error' in result ? (result.error as { data?: { message?: string } }) : undefined;
    setSubmitErrorByAdId((prev) => ({ ...prev, [id]: errorResponse?.data?.message ?? 'ارسال برای بررسی با خطا مواجه شد.' }));
  };

  return (
    <>
      <div className="mx-auto max-w-7xl">
        <div className="mb-5 flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
          <div>
            <h1 className="flex items-center gap-2.5 text-2xl font-extrabold text-slate-800 sm:text-3xl">
              <Briefcase className="h-7 w-7 text-emerald-600" />
              آگهی‌های شرکت من
            </h1>
            <p className="mt-1.5 text-sm text-slate-500">مدیریت و پیگیری آگهی‌های استخدام شرکت</p>
          </div>

          <button
            type="button"
            onClick={() => setModalState({ editingAd: null })}
            className="inline-flex items-center justify-center gap-2 rounded-xl bg-emerald-600 px-5 py-3 text-sm font-bold text-white shadow-sm shadow-emerald-900/10 transition-colors hover:bg-emerald-700"
          >
            <Plus className="h-4 w-4" />
            ثبت آگهی استخدام جدید
          </button>
        </div>

        <div className="mb-6 flex items-start gap-2.5 rounded-2xl border border-emerald-100 bg-emerald-50/60 px-4 py-3 text-xs leading-6 text-emerald-800 sm:text-sm">
          <Info className="mt-0.5 h-4 w-4 shrink-0 text-emerald-600" />
          <span>
            هزینه ثبت و بررسی هر آگهی: <strong className="font-bold">{feeAmountToman} تومان</strong> — پس از پرداخت، آگهی برای استعلام و
            تایید نهایی به تیم پی کار ارسال می‌شود.
          </span>
        </div>

        <div className="mb-6 flex flex-wrap gap-2">
          {FILTER_GROUPS.map((filter) => {
            const count =
              filter.statuses.length === 0 ? jobs.length : jobs.filter((ad) => filter.statuses.includes(ad.status)).length;
            const isActive = activeFilter === filter.key;
            return (
              <button
                key={filter.key}
                type="button"
                onClick={() => setActiveFilter(filter.key)}
                className={`rounded-full border px-4 py-2 text-xs font-semibold transition-colors sm:text-sm ${
                  isActive
                    ? 'border-emerald-600 bg-emerald-600 text-white shadow-sm'
                    : 'border-slate-200 bg-white text-slate-500 hover:bg-slate-100'
                }`}
              >
                {filter.label}
                <span className={`mr-1.5 ${isActive ? 'text-emerald-100' : 'text-slate-400'}`}>({count.toLocaleString('fa-IR')})</span>
              </button>
            );
          })}
        </div>

        {isLoading && <p className="text-sm text-slate-400">در حال بارگذاری...</p>}

        {!isLoading && filteredJobs.length === 0 && (
          <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-slate-200 bg-white py-16 text-center">
            <Briefcase className="mb-3 h-9 w-9 text-slate-300" />
            <p className="font-semibold text-slate-600">
              {jobs.length === 0 ? 'هنوز آگهی‌ای ثبت نکرده‌اید.' : 'آگهی‌ای در این وضعیت یافت نشد.'}
            </p>
            <p className="mt-1 text-sm text-slate-400">
              {jobs.length === 0 ? 'با دکمه «ثبت آگهی استخدام جدید» اولین آگهی خود را ثبت کنید.' : 'فیلتر دیگری را انتخاب کنید.'}
            </p>
          </div>
        )}

        {!isLoading && filteredJobs.length > 0 && (
          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
            {filteredJobs.map((ad) => (
              <JobCard
                key={ad.id}
                ad={ad}
                applicantsCount={applicantsByJobAdId.get(ad.id) ?? 0}
                feeAmountToman={feeAmountToman}
                onEdit={(jobAd) => setModalState({ editingAd: jobAd })}
                onDelete={handleDelete}
                onClose={handleClose}
                onSubmitForReview={handleSubmitForReview}
                isSubmittingForReview={isSubmitting}
                submitError={submitErrorByAdId[ad.id]}
              />
            ))}
          </div>
        )}
      </div>

      {modalState && (
        <JobAdFormModal
          editingAd={modalState.editingAd}
          onClose={() => setModalState(null)}
          onSuccess={() => setModalState(null)}
        />
      )}
    </>
  );
}
