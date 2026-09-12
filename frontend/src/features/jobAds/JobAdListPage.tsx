import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useAppSelector } from '@/app/hooks';
import { useSearchJobAdsQuery, useToggleBookmarkMutation } from '@/features/jobAds/jobAdApi';
import { useGetCitiesQuery, useGetIndustrialZonesQuery, useGetProvincesQuery } from '@/features/geography/geographyApi';
import { useZoneLabelMap } from '@/features/geography/useZoneLabel';
import { useGetMyBookmarkIdsQuery } from '@/features/candidates/candidateApi';
import { WorkShiftLabels, ContractTypeLabels } from '@/shared/enums';
import BannerStrip from '@/features/ads/BannerStrip';
import JobAdCard from '@/features/jobAds/JobAdCard';

/** استایل مشترک اینپوت/سلکت‌های فشرده فیلتر — طبق تسک #84 (ارتفاع h-9، پدینگ و فوکوس یکسان برای همه فیلدها). */
const compactFieldClass =
  'h-9 w-full rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs text-slate-700 placeholder:text-slate-400 transition focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20 disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-400';
const compactLabelClass = 'mb-1 block text-xs font-medium text-slate-600';

function ResetIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" className="h-3.5 w-3.5">
      <path
        d="M3 12a9 9 0 1 1 3.2 6.9M3 12V6m0 6h6"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export default function JobAdListPage() {
  const [searchParams] = useSearchParams();
  const [provinceId, setProvinceId] = useState('');
  const [cityId, setCityId] = useState(searchParams.get('cityId') ?? '');
  const [industrialZoneId, setIndustrialZoneId] = useState(searchParams.get('industrialZoneId') ?? '');
  const [workShift, setWorkShift] = useState('');
  const [contractType, setContractType] = useState('');
  const [minSalaryAmount, setMinSalaryAmount] = useState('');
  const [hasCommuteService, setHasCommuteService] = useState(false);
  const [keyword, setKeyword] = useState(searchParams.get('keyword') ?? '');
  const [page, setPage] = useState(1);

  const { data, isLoading } = useSearchJobAdsQuery({
    industrialZoneId: industrialZoneId || undefined,
    cityId: cityId || undefined,
    workShift: workShift || undefined,
    contractType: contractType || undefined,
    minSalaryAmount: minSalaryAmount ? Number(minSalaryAmount) : undefined,
    hasCommuteService: hasCommuteService || undefined,
    keyword: keyword || undefined,
    page,
    pageSize: 20
  });

  // طبق تسک #80 — جست‌وجوی آبشاری استان → شهر → شهرک صنعتی.
  const { data: provinces } = useGetProvincesQuery();
  const { data: cities } = useGetCitiesQuery(provinceId || undefined);
  const { data: zones } = useGetIndustrialZonesQuery(cityId || undefined);

  const jobAds = data?.data?.items ?? [];
  const totalPages = data?.data?.totalPages ?? 1;
  const zoneLabelById = useZoneLabelMap();

  // آیکون بوک‌مارک — طبق تسک #116، فقط برای کارجوی واردشده.
  const user = useAppSelector((state) => state.auth.user);
  const isCandidate = Boolean(user?.roles.includes('Candidate'));
  const { data: bookmarkIdsData } = useGetMyBookmarkIdsQuery(undefined, { skip: !isCandidate });
  const bookmarkedIds = new Set(bookmarkIdsData?.data ?? []);
  const [toggleBookmark] = useToggleBookmarkMutation();

  const handleProvinceChange = (value: string) => {
    setProvinceId(value);
    setCityId('');
    setIndustrialZoneId('');
    setPage(1);
  };

  const handleCityChange = (value: string) => {
    setCityId(value);
    setIndustrialZoneId('');
    setPage(1);
  };

  const resetFilters = () => {
    setKeyword('');
    setProvinceId('');
    setCityId('');
    setIndustrialZoneId('');
    setWorkShift('');
    setContractType('');
    setMinSalaryAmount('');
    setHasCommuteService(false);
    setPage(1);
  };

  /** طبق تسک #84: بدنه فیلتر یک‌بار تعریف و در دو پوسته (سایدبار ثابت دسکتاپ / بلوک بالای نتایج در موبایل) رندر می‌شود. */
  const filterFields = (
    <div className="space-y-3">
      <div>
        <label className={compactLabelClass}>جست‌وجو</label>
        <input
          className={compactFieldClass}
          value={keyword}
          onChange={(e) => { setKeyword(e.target.value); setPage(1); }}
          placeholder="عنوان شغلی، مهارت..."
        />
      </div>

      <div className="space-y-3 border-t border-slate-100 pt-3">
        <div>
          <label className={compactLabelClass}>استان</label>
          <select className={compactFieldClass} value={provinceId} onChange={(e) => handleProvinceChange(e.target.value)}>
            <option value="">همه استان‌ها</option>
            {provinces?.data?.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
        </div>
        <div>
          <label className={compactLabelClass}>شهر</label>
          <select className={compactFieldClass} value={cityId} onChange={(e) => handleCityChange(e.target.value)} disabled={!provinceId}>
            <option value="">همه شهرها</option>
            {cities?.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </div>
        <div>
          <label className={compactLabelClass}>شهرک صنعتی</label>
          <select
            className={compactFieldClass}
            value={industrialZoneId}
            onChange={(e) => { setIndustrialZoneId(e.target.value); setPage(1); }}
            disabled={!cityId}
          >
            <option value="">همه</option>
            {zones?.data?.map((z) => <option key={z.id} value={z.id}>{z.name}</option>)}
          </select>
        </div>
      </div>

      <div className="space-y-3 border-t border-slate-100 pt-3">
        <div>
          <label className={compactLabelClass}>شیفت کاری</label>
          <select className={compactFieldClass} value={workShift} onChange={(e) => { setWorkShift(e.target.value); setPage(1); }}>
            <option value="">همه</option>
            {Object.entries(WorkShiftLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </select>
        </div>
        <div>
          <label className={compactLabelClass}>نوع قرارداد</label>
          <select className={compactFieldClass} value={contractType} onChange={(e) => { setContractType(e.target.value); setPage(1); }}>
            <option value="">همه</option>
            {Object.entries(ContractTypeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </select>
        </div>
        <div>
          <label className={compactLabelClass}>حداقل حقوق (تومان)</label>
          <input
            type="number"
            className={compactFieldClass}
            value={minSalaryAmount}
            onChange={(e) => { setMinSalaryAmount(e.target.value); setPage(1); }}
            placeholder="مثلاً ۱۵۰۰۰۰۰۰"
          />
        </div>
        <label htmlFor="hasCommuteService" className="flex cursor-pointer items-center gap-2 text-xs font-medium text-slate-600">
          <input
            type="checkbox"
            id="hasCommuteService"
            className="h-3.5 w-3.5 rounded border-slate-300 text-emerald-500 focus:ring-emerald-500/30"
            checked={hasCommuteService}
            onChange={(e) => { setHasCommuteService(e.target.checked); setPage(1); }}
          />
          دارای سرویس ایاب‌وذهاب
        </label>
      </div>
    </div>
  );

  const filterHeader = (
    <div className="mb-3 flex items-center justify-between">
      <h3 className="text-sm font-semibold text-slate-800">جست‌وجو و فیلتر پیشرفته</h3>
      <button
        type="button"
        onClick={resetFilters}
        title="پاک‌سازی فیلترها"
        className="flex h-6 w-6 items-center justify-center rounded-md text-slate-400 transition hover:bg-slate-100 hover:text-slate-600"
      >
        <ResetIcon />
      </button>
    </div>
  );

  return (
    <>
      {/* طبق تسک #84: سایدبار فیلتر در دسکتاپ کاملاً به لبه چپ صفحه (viewport) می‌چسبد، نه فقط لبه container؛
          به همین دلیل fixed است و مستقل از عرض حداکثر محتوا (۱۰۸۰px) قرار می‌گیرد. */}
      <aside className="scrollbar-thin scrollbar-thumb-slate-200 scrollbar-track-transparent fixed top-[var(--header-height)] left-0 z-10 hidden h-[calc(100vh-var(--header-height))] w-64 flex-col overflow-y-auto rounded-r-2xl border border-slate-100/80 bg-white px-4 py-4 shadow-sm lg:flex xl:w-72">
        {filterHeader}
        {filterFields}
      </aside>

      <div className="lg:ml-64 xl:ml-72">
        <div className="container">
          <h1>آگهی‌های استخدامی شهرک‌های صنعتی</h1>

          <BannerStrip placement="JobAdList" />

          {/* فیلتر در موبایل/تبلت: بلوک ساده بالای نتایج (بدون fixed) */}
          <div className="mb-4 rounded-2xl border border-slate-100/80 bg-white p-4 shadow-sm lg:hidden">
            {filterHeader}
            {filterFields}
          </div>

          {isLoading && <p>در حال بارگذاری...</p>}
          {!isLoading && jobAds.length === 0 && <p>آگهی‌ای با این فیلترها یافت نشد.</p>}

          <motion.div
            className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ duration: 0.3 }}
          >
            {jobAds.map((ad, idx) => (
              <motion.div
                key={ad.id}
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.3, delay: Math.min(idx * 0.03, 0.3) }}
              >
                <JobAdCard
                  ad={ad}
                  locationLabel={zoneLabelById.get(ad.industrialZoneId)}
                  isBookmarked={isCandidate ? bookmarkedIds.has(ad.id) : undefined}
                  onToggleBookmark={isCandidate ? (id) => toggleBookmark(id) : undefined}
                />
              </motion.div>
            ))}
          </motion.div>

          {totalPages > 1 && (
            <div style={{ display: 'flex', gap: '0.5rem', marginTop: '1.5rem', flexWrap: 'wrap' }}>
              {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
                <button key={p} className="btn-primary" style={{ opacity: p === page ? 1 : 0.5 }} onClick={() => setPage(p)}>
                  {p}
                </button>
              ))}
            </div>
          )}
        </div>
      </div>
    </>
  );
}
