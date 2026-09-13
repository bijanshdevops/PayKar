import { Link } from 'react-router-dom';
import type { JobAd } from '@/features/jobAds/jobAdApi';
import { WorkShiftLabels, ContractTypeLabels, SalaryRangeTypeLabels } from '@/shared/enums';
import { formatRelativeJalali } from '@/shared/utils/date';

function VerifiedTick() {
  return (
    <svg viewBox="0 0 24 24" fill="none" className="h-4 w-4 text-sky-500" aria-label="شرکت تایید‌شده">
      <path
        d="M9 12.5l2 2 4.5-5M12 3l2.1 1.1L16.5 4l1 2.2L20 7.2l-.4 2.4L21 12l-1.4 2.4.4 2.4-2.2 1-.1 2.2-2.4-.4L12 21l-2.3-1.4-2.4.4-.1-2.2-2.2-1 .4-2.4L4 12l1.4-2.4-.4-2.4 2.2-1 .1-2.2 2.4.4L12 3Z"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinejoin="round"
      />
      <path d="M9 12.5l2 2 4.5-5" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function LocationPin() {
  return (
    <svg viewBox="0 0 24 24" fill="none" className="h-3.5 w-3.5 shrink-0 text-slate-400">
      <path
        d="M12 21s7-6.1 7-11.5A7 7 0 0 0 5 9.5C5 14.9 12 21 12 21Z"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinejoin="round"
      />
      <circle cx="12" cy="9.5" r="2.25" stroke="currentColor" strokeWidth="2" />
    </svg>
  );
}

function FactoryLogo() {
  return (
    <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-slate-100 text-lg">🏭</div>
  );
}

function BookmarkIcon({ filled }: { filled: boolean }) {
  return (
    <svg
      viewBox="0 0 24 24"
      className="h-4 w-4"
      fill={filled ? 'currentColor' : 'none'}
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinejoin="round"
    >
      <path d="M6 4.5A1.5 1.5 0 0 1 7.5 3h9A1.5 1.5 0 0 1 18 4.5V20l-6-3.6-6 3.6V4.5Z" />
    </svg>
  );
}

/** فاصله نسبی از انتشار آگهی — فقط بر اساس publishedAtUtc واقعی (بدون شمارنده بازدید جعلی). */
function relativeDaysFa(dateIso: string): string {
  return formatRelativeJalali(dateIso);
}

export default function JobAdCard({
  ad,
  locationLabel,
  isBookmarked,
  onToggleBookmark
}: {
  ad: JobAd;
  locationLabel?: string;
  /** طبق تصمیم صریح محصولی این فاز — اختیاری؛ فقط وقتی والد صفحه وضعیت بوک‌مارک را می‌داند نمایش داده می‌شود. */
  isBookmarked?: boolean;
  onToggleBookmark?: (jobAdId: string) => void;
}) {
  return (
    <Link
      to={`/job-ads/${ad.id}`}
      className="group relative flex h-full flex-col rounded-2xl border border-slate-100 bg-white p-4 shadow-sm shadow-slate-900/5 transition-all hover:-translate-y-0.5 hover:border-emerald-200 hover:shadow-lg hover:shadow-emerald-900/10"
    >
      {ad.isFeatured && (
        <span className="absolute -top-2 left-3 rounded-full bg-sky-500 px-2.5 py-0.5 text-[11px] font-bold text-white shadow-sm">
          ویژه ★
        </span>
      )}

      {onToggleBookmark && (
        <button
          type="button"
          aria-label={isBookmarked ? 'لغو نشان آگهی' : 'نشان‌کردن آگهی'}
          onClick={(e) => {
            e.preventDefault();
            e.stopPropagation();
            onToggleBookmark(ad.id);
          }}
          className={`absolute left-3 top-3 z-10 flex h-8 w-8 items-center justify-center rounded-full shadow-sm transition-colors ${
            isBookmarked
              ? 'bg-emerald-600 text-white hover:bg-emerald-700'
              : 'bg-white/90 text-slate-400 hover:text-emerald-600'
          }`}
        >
          <BookmarkIcon filled={Boolean(isBookmarked)} />
        </button>
      )}

      <div className="mb-3 flex items-start gap-3">
        {ad.companyLogoUrl ? (
          <img src={ad.companyLogoUrl} alt={ad.companyName} className="h-11 w-11 shrink-0 rounded-xl object-cover" />
        ) : (
          <FactoryLogo />
        )}
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-1.5">
            <span className="truncate text-xs font-semibold text-slate-500">{ad.companyName}</span>
            {ad.isCompanyVerified && <VerifiedTick />}
          </div>
          <h3 className="mt-0.5 truncate text-base font-bold text-slate-800 group-hover:text-emerald-700">{ad.title}</h3>
        </div>
      </div>

      {locationLabel && (
        <div className="mb-2 flex items-center gap-1 text-xs text-slate-500">
          <LocationPin />
          <span className="truncate">{locationLabel}</span>
        </div>
      )}

      <p className="mb-3 line-clamp-2 text-xs leading-5 text-slate-500">{ad.description}</p>

      <div className="mb-3 flex flex-wrap gap-1.5">
        <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-medium text-slate-600">
          {WorkShiftLabels[ad.workShift] ?? ad.workShift}
        </span>
        <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-medium text-slate-600">
          {ContractTypeLabels[ad.contractType] ?? ad.contractType}
        </span>
        <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-medium text-slate-600">
          {SalaryRangeTypeLabels[ad.salaryRange.type] ?? ad.salaryRange.type}
        </span>
      </div>

      <div className="mt-auto flex items-center justify-between border-t border-slate-100 pt-3 text-[11px] text-slate-400">
        <span>{ad.publishedAtUtc ? relativeDaysFa(ad.publishedAtUtc) : ''}</span>
        <span className="flex items-center gap-2">
          {ad.viewsCount > 0 && <span>👁 {ad.viewsCount.toLocaleString('fa-IR')} بازدید</span>}
          {ad.headcountNeeded ? <span>نیازمندی: {ad.headcountNeeded.toLocaleString('fa-IR')} نفر</span> : null}
        </span>
      </div>

      <span className="mt-3 flex items-center justify-center rounded-xl bg-[#F59E0B] px-4 py-2.5 text-xs font-bold text-[#19263A] transition-colors group-hover:bg-[#D97706]">
        مشاهده و ارسال رزومه
      </span>
    </Link>
  );
}
