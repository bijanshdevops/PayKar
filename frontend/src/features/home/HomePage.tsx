import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useAppSelector } from '@/app/hooks';
import { useSearchJobAdsQuery, useToggleBookmarkMutation } from '@/features/jobAds/jobAdApi';
import { useGetCitiesQuery, useGetProvincesQuery } from '@/features/geography/geographyApi';
import { useZoneLabelMap } from '@/features/geography/useZoneLabel';
import { useGetPublicStatsQuery } from '@/features/home/publicStatsApi';
import { useGetMyBookmarkIdsQuery } from '@/features/candidates/candidateApi';
import { WorkShiftLabels, ContractTypeLabels } from '@/shared/enums';
import BannerStrip from '@/features/ads/BannerStrip';
import JobAdCard from '@/features/jobAds/JobAdCard';

/** طبق تسک #85: آیکون‌های مینیمال Inline SVG برای سرچ‌بار Hero (بدون افزودن کتابخانه آیکون جدید). */
function SearchIcon({ className = 'h-5 w-5' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" className={className}>
      <circle cx="11" cy="11" r="7" stroke="currentColor" strokeWidth="2" />
      <path d="m20 20-3.5-3.5" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  );
}

function LocationIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" className={className}>
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

function ChevronIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" className="pointer-events-none h-3.5 w-3.5 shrink-0 text-slate-400">
      <path d="m6 9 6 6 6-6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

const quickFilterSelectClass =
  'h-8 rounded-full border border-slate-200 bg-white px-3 text-xs text-slate-600 focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20';

export default function HomePage() {
  const user = useAppSelector((state) => state.auth.user);
  const navigate = useNavigate();
  const [keyword, setKeyword] = useState('');
  const [provinceId, setProvinceId] = useState('');
  const [cityId, setCityId] = useState('');

  // طبق تسک #94 — چیپ‌های فیلتر سریع؛ همگی روی پارامترهای واقعی پشتیبانی‌شده توسط SearchJobAdsQuery.
  const [quickWorkShift, setQuickWorkShift] = useState('');
  const [quickContractType, setQuickContractType] = useState('');
  const [quickCommuteOnly, setQuickCommuteOnly] = useState(false);

  // طبق تسک #80 — جست‌وجوی آبشاری استان → شهر، حتی در فرم جست‌وجوی فشرده صفحه اصلی.
  const { data: provinces } = useGetProvincesQuery();
  const { data: cities } = useGetCitiesQuery(provinceId || undefined);

  const { data, isLoading } = useSearchJobAdsQuery({
    page: 1,
    pageSize: 9,
    workShift: quickWorkShift || undefined,
    contractType: quickContractType || undefined,
    hasCommuteService: quickCommuteOnly || undefined,
    sortBy: 'createdAtUtc',
    sortDir: 'desc'
  });

  const { data: statsData } = useGetPublicStatsQuery();
  const stats = statsData?.data;
  const zoneLabelById = useZoneLabelMap();

  // آیکون بوک‌مارک — فقط برای کارجوی واردشده؛ سایر نقش‌ها/مهمان‌ها این کوئری را صدا نمی‌زنند.
  const isCandidate = Boolean(user?.roles.includes('Candidate'));
  const { data: bookmarkIdsData } = useGetMyBookmarkIdsQuery(undefined, { skip: !isCandidate });
  const bookmarkedIds = new Set(bookmarkIdsData?.data ?? []);
  const [toggleBookmark] = useToggleBookmarkMutation();

  const latestAds = data?.data?.items ?? [];
  const totalCount = data?.data?.totalCount ?? 0;
  const hasActiveQuickFilter = !!(quickWorkShift || quickContractType || quickCommuteOnly);

  const resetQuickFilters = () => {
    setQuickWorkShift('');
    setQuickContractType('');
    setQuickCommuteOnly(false);
  };

  const onSearchSubmit = (e: FormEvent) => {
    e.preventDefault();
    const params = new URLSearchParams();
    if (keyword) params.set('keyword', keyword);
    if (cityId) params.set('cityId', cityId);
    const query = params.toString();
    navigate(query ? `/job-ads?${query}` : '/job-ads');
  };

  return (
    <div>
      <motion.section
        className="-mt-20 px-4 pb-14 pt-24 text-center sm:pb-20 sm:pt-28"
        style={{
          // تصویر پس‌زمینهٔ Hero (طبق بازطراحی: منتقل‌شده از هدر به اینجا) با یک گرادیان عمودی
          // تیرهٔ navy روی آن ترکیب می‌شود تا هم از بالا زیر هدر شفاف دیده شود، هم به‌تدریج به
          // رنگ پایهٔ سایت (#19263A) در پایین Hero بنشیند و گذار نرمی به بقیهٔ صفحه داشته باشد.
          backgroundImage:
            "linear-gradient(180deg, rgba(25, 38, 58, 0.70) 0%, rgba(25, 38, 58, 0.88) 75%, #19263A 100%), url('/images/navbar-bg.png')",
          backgroundSize: 'cover',
          backgroundPosition: 'center top',
          backgroundColor: '#19263A'
        }}
        initial={{ opacity: 0, y: -16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
      >
        <h1 className="mx-auto max-w-3xl text-2xl font-extrabold leading-snug text-white sm:text-4xl">
          فرصت شغلی بعدی‌تان در شهرک‌های صنعتی
        </h1>
        <p className="mx-auto mt-3 max-w-xl text-sm text-white/80 sm:text-lg">
          جست‌وجوی آگهی‌های استخدام کارخانه‌ها و شرکت‌های صنعتی سراسر کشور، بر اساس شهر یا نام شهرک صنعتی.
        </p>

        {/* طبق تسک #85: نوار جست‌وجوی یکپارچه (Segmented Search Bar) */}
        <form
          onSubmit={onSearchSubmit}
          className="mx-auto mt-8 flex max-w-3xl flex-col gap-1 rounded-2xl bg-white p-2 shadow-2xl shadow-slate-900/10 sm:flex-row sm:items-center sm:gap-0"
        >
          <div className="flex flex-1 items-center gap-2 px-3 py-2.5">
            <SearchIcon className="h-5 w-5 shrink-0 text-slate-400" />
            <input
              value={keyword}
              onChange={(e) => setKeyword(e.target.value)}
              placeholder="عنوان شغلی، مهارت یا شهرک صنعتی..."
              className="w-full min-w-0 border-none bg-transparent text-sm text-slate-700 placeholder:text-slate-400 focus:outline-none"
            />
          </div>

          <div className="hidden h-8 w-px shrink-0 bg-slate-200 sm:block" />

          <div className="flex items-center gap-1.5 px-3 py-2.5 sm:w-36">
            <LocationIcon className="h-4 w-4 shrink-0 text-slate-400" />
            <select
              value={provinceId}
              onChange={(e) => { setProvinceId(e.target.value); setCityId(''); }}
              className="w-full min-w-0 appearance-none border-none bg-transparent text-sm text-slate-700 focus:outline-none"
            >
              <option value="">همه استان‌ها</option>
              {provinces?.data?.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
            </select>
            <ChevronIcon />
          </div>

          <div className="hidden h-8 w-px shrink-0 bg-slate-200 sm:block" />

          <div className="flex items-center gap-1.5 px-3 py-2.5 sm:w-36">
            <select
              value={cityId}
              onChange={(e) => setCityId(e.target.value)}
              disabled={!provinceId}
              className="w-full min-w-0 appearance-none border-none bg-transparent text-sm text-slate-700 focus:outline-none disabled:text-slate-300"
            >
              <option value="">همه شهرها</option>
              {cities?.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
            <ChevronIcon />
          </div>

          <button
            type="submit"
            className="flex items-center justify-center gap-2 rounded-xl bg-[#F59E0B] px-6 py-3 text-sm font-bold text-[#19263A] shadow-md transition-all hover:bg-[#D97706]"
          >
            <SearchIcon className="h-4 w-4" />
            جست‌وجو
          </button>
        </form>

        {/* طبق تسک #94: چیپ‌های فیلتر سریع — همگی روی پارامترهای واقعی SearchJobAdsQuery، بدون فیلتر ساختگی. */}
        <div className="mx-auto mt-4 flex max-w-3xl flex-wrap items-center justify-center gap-2">
          <select
            value={quickWorkShift}
            onChange={(e) => setQuickWorkShift(e.target.value)}
            className={quickFilterSelectClass}
          >
            <option value="">شیفت کاری</option>
            {Object.entries(WorkShiftLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </select>
          <select
            value={quickContractType}
            onChange={(e) => setQuickContractType(e.target.value)}
            className={quickFilterSelectClass}
          >
            <option value="">نوع همکاری</option>
            {Object.entries(ContractTypeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </select>
          <label className={`${quickFilterSelectClass} flex cursor-pointer items-center gap-1.5`}>
            <input type="checkbox" checked={quickCommuteOnly} onChange={(e) => setQuickCommuteOnly(e.target.checked)} className="h-3 w-3" />
            دارای سرویس
          </label>
          {hasActiveQuickFilter && (
            <button
              type="button"
              onClick={resetQuickFilters}
              className="h-8 rounded-full border border-white/30 bg-white/10 px-3 text-xs text-white transition hover:bg-white/20"
            >
              پاک کردن فیلترها ✕
            </button>
          )}
          <Link
            to="/job-ads"
            className="rounded-full border border-white/20 bg-white/10 px-4 py-1.5 text-xs font-medium text-white shadow-sm backdrop-blur-sm transition-all hover:bg-white/20"
          >
            همه فیلترها ⚙️
          </Link>
          {!user && (
            <Link
              to="/login"
              className="rounded-full border border-white/20 bg-white/10 px-4 py-1.5 text-xs font-medium text-white shadow-sm backdrop-blur-sm transition-all hover:bg-white/20"
            >
              کارفرما هستید؟ ثبت رایگان آگهی 🚀
            </Link>
          )}
        </div>
      </motion.section>

      <div className="container">
        <BannerStrip placement="Home" />

        <motion.div
          className="my-6 grid grid-cols-2 gap-3 sm:grid-cols-4"
          initial={{ opacity: 0, y: 12 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, amount: 0.3 }}
          transition={{ duration: 0.4 }}
        >
          <div className="rounded-2xl border border-slate-100 bg-white p-4 text-center shadow-sm">
            <div className="text-xl font-extrabold text-[#19263A] sm:text-2xl">
              {(stats?.totalCandidates ?? 0).toLocaleString('fa-IR')}+
            </div>
            <div className="mt-1 text-xs text-slate-500">متقاضیان فعال</div>
          </div>
          <div className="rounded-2xl border border-slate-100 bg-white p-4 text-center shadow-sm">
            <div className="text-xl font-extrabold text-[#19263A] sm:text-2xl">
              {(stats?.totalIndustrialZones ?? 0).toLocaleString('fa-IR')}+
            </div>
            <div className="mt-1 text-xs text-slate-500">شهرک صنعتی در کشور</div>
          </div>
          <div className="rounded-2xl border border-slate-100 bg-white p-4 text-center shadow-sm">
            <div className="text-xl font-extrabold text-[#19263A] sm:text-2xl">
              {(stats?.totalVerifiedCompanies ?? 0).toLocaleString('fa-IR')}+
            </div>
            <div className="mt-1 text-xs text-slate-500">کارخانه و شرکت تایید‌شده</div>
          </div>
          <div className="rounded-2xl border border-slate-100 bg-white p-4 text-center shadow-sm">
            <div className="text-xl font-extrabold text-[#19263A] sm:text-2xl">
              {(stats?.totalActiveJobAds ?? totalCount).toLocaleString('fa-IR')}+
            </div>
            <div className="mt-1 text-xs text-slate-500">فرصت شغلی فعال</div>
          </div>
        </motion.div>

        <motion.div
          initial={{ opacity: 0, y: 12 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, amount: 0.15 }}
          transition={{ duration: 0.4 }}
        >
          <div className="mb-4 flex items-center justify-between">
            <h2 className="text-lg font-bold text-slate-800">تازه‌ترین فرصت‌های شغلی</h2>
            <Link to="/job-ads" className="text-sm font-medium text-emerald-600 hover:text-emerald-700">مشاهده همه ←</Link>
          </div>

          {isLoading && <p className="text-sm text-slate-500">در حال بارگذاری...</p>}
          {!isLoading && latestAds.length === 0 && <p className="text-sm text-slate-500">آگهی‌ای با این فیلترها یافت نشد.</p>}

          {!isLoading && latestAds.length > 0 && (
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {latestAds.map((ad, idx) => (
                <motion.div
                  key={ad.id}
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: Math.min(idx * 0.04, 0.3) }}
                >
                  <JobAdCard
                    ad={ad}
                    locationLabel={zoneLabelById.get(ad.industrialZoneId)}
                    isBookmarked={isCandidate ? bookmarkedIds.has(ad.id) : undefined}
                    onToggleBookmark={isCandidate ? (id) => toggleBookmark(id) : undefined}
                  />
                </motion.div>
              ))}
            </div>
          )}
        </motion.div>
      </div>
    </div>
  );
}
