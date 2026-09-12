import { useEffect, useRef } from 'react';
import { motion } from 'framer-motion';
import { useGetActiveBannerAdsQuery, type BannerPlacement } from '@/features/ads/bannerAdApi';
import AutoScrollCarousel from '@/components/AutoScrollCarousel';

/** طبق همان الگوی baseApi.ts — این دو اندپوینت عمومی ردیابی، از پوشش RTK Query/ApiResponse خارج‌اند
 * (۲۰۴ بدون بدنه و ۳۰۲ ریدایرکت واقعی مرورگر)، پس مستقیماً به baseUrl واقعی API متصل می‌شوند. */
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api/v1';

/**
 * نوار بنرهای تبلیغاتی فعال — طبق ADR-008: همیشه به‌صورت بخشی کاملاً مجزا با برچسب «تبلیغات»
 * رندر می‌شود، هرگز به‌عنوان آیتمی داخل فهرست آگهی‌ها ادغام نمی‌شود.
 * طبق تسک #79: اسکرول افقی خودکار (با توقف روی هاور) — بدون دکمه «مشاهده بیشتر»، چون این بخش
 * از قبل تمام بنرهای فعال موجود را نمایش می‌دهد و صفحه‌ی فهرست عمومی جداگانه‌ای برای بنرها وجود ندارد.
 *
 * طبق تسک ۶ (Lightweight Tracking): این کامپوننت تنها مصرف‌کننده واقعی endpointهای عمومی
 * impression/click است — بدون این اتصال، آمار ImpressionsCount/ClicksCount که در پنل شرکت
 * (CompanyBannerAdsPage) نمایش داده می‌شود همیشه صفر باقی می‌ماند.
 * - نمایش (Impression): با فراخوانی fetch (keepalive, بی‌صدا/best-effort) به‌ازای هر بنر، حداکثر
 *   یک‌بار در طول عمر این نمونه از کامپوننت (نه IntersectionObserver دقیق در سطح ویوپورت — تقریب سبک
 *   کافی طبق عنوان «Lightweight Tracking»).
 * - کلیک: لینک به‌جای destinationUrl مستقیم، به اندپوینت ریدایرکت بک‌اند اشاره می‌کند تا هم کلیک
 *   به‌صورت اتمیک ثبت شود و هم مرورگر به‌طور طبیعی ریدایرکت ۳۰۲ را دنبال کند.
 */
export default function BannerStrip({ placement }: { placement: BannerPlacement }) {
  const { data, isLoading } = useGetActiveBannerAdsQuery(placement);
  const banners = data?.data ?? [];
  const recordedImpressions = useRef<Set<string>>(new Set());

  useEffect(() => {
    banners.forEach((banner) => {
      if (recordedImpressions.current.has(banner.id)) return;
      recordedImpressions.current.add(banner.id);
      fetch(`${API_BASE_URL}/public/banners/${banner.id}/impression`, { method: 'GET', keepalive: true }).catch(() => {
        // ردیابی نمایش صرفاً best-effort است؛ شکست آن نباید تجربه کاربر را مختل کند.
      });
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [banners.map((b) => b.id).join(',')]);

  if (isLoading || banners.length === 0) return null;

  return (
    <motion.section
      className="carousel-section carousel-section--ads"
      aria-label="تبلیغات"
      initial={{ opacity: 0, y: 12 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.2 }}
      transition={{ duration: 0.4 }}
    >
      <span className="section-title__label">تبلیغات</span>

      <AutoScrollCarousel durationSeconds={26}>
        {banners.map((banner) => (
          <a
            key={banner.id}
            href={`${API_BASE_URL}/public/banners/${banner.id}/click`}
            target="_blank"
            rel="noreferrer sponsored"
            className="banner-card"
          >
            <img src={banner.imageUrl} alt="بنر تبلیغاتی" />
          </a>
        ))}
      </AutoScrollCarousel>
    </motion.section>
  );
}
