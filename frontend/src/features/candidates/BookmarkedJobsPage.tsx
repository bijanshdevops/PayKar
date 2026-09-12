import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useGetMyBookmarksQuery } from '@/features/candidates/candidateApi';
import { useToggleBookmarkMutation } from '@/features/jobAds/jobAdApi';
import { useZoneLabelMap } from '@/features/geography/useZoneLabel';
import JobAdCard from '@/features/jobAds/JobAdCard';

/**
 * صفحهٔ «آگهی‌های نشان‌شده» در داشبورد کارجو — طبق 03_jobseeker_dashboard_spec.md بخش ۱.
 * لیست کامل آگهی‌های نشان‌شده توسط GetMyBookmarkedJobsQuery برگردانده می‌شود (جدیدترین نشان‌شده
 * ابتدا)؛ لغو نشان مستقیماً از همین صفحه ممکن است و کارت بلافاصله از لیست حذف می‌شود
 * (بدون نیاز به رفرش دستی، به لطف invalidatesTags: ['Bookmark']).
 */
export default function BookmarkedJobsPage() {
  const { data, isLoading } = useGetMyBookmarksQuery();
  const bookmarkedAds = data?.data ?? [];
  const zoneLabelById = useZoneLabelMap();
  const [toggleBookmark] = useToggleBookmarkMutation();

  return (
    <div>
      <div className="mb-5">
        <h1 className="text-xl font-extrabold text-slate-800">آگهی‌های نشان‌شده</h1>
        <p className="text-sm text-slate-500">آگهی‌هایی که برای بررسی بعدی نشان کرده‌اید.</p>
      </div>

      {isLoading && <p className="text-sm text-slate-500">در حال بارگذاری...</p>}

      {!isLoading && bookmarkedAds.length === 0 && (
        <div className="rounded-2xl border border-slate-100 bg-white p-8 text-center shadow-sm">
          <div className="mb-2 text-3xl">🔖</div>
          <p className="text-sm font-medium text-slate-600">هنوز هیچ آگهی‌ای را نشان نکرده‌اید.</p>
          <p className="mt-1 text-xs text-slate-400">
            روی آیکون بوک‌مارک هر آگهی بزنید تا برای بررسی بعدی اینجا ذخیره شود.
          </p>
          <Link
            to="/job-ads"
            className="mt-4 inline-block rounded-xl bg-[#F59E0B] px-5 py-2.5 text-sm font-bold text-[#19263A] transition hover:bg-[#D97706]"
          >
            مشاهده آگهی‌ها
          </Link>
        </div>
      )}

      {!isLoading && bookmarkedAds.length > 0 && (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {bookmarkedAds.map((ad, idx) => (
            <motion.div
              key={ad.id}
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.3, delay: Math.min(idx * 0.04, 0.3) }}
            >
              <JobAdCard
                ad={ad}
                locationLabel={zoneLabelById.get(ad.industrialZoneId)}
                isBookmarked
                onToggleBookmark={(id) => toggleBookmark(id)}
              />
            </motion.div>
          ))}
        </div>
      )}
    </div>
  );
}
