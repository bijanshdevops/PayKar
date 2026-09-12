import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { useGetOwnerDashboardQuery } from '@/features/dashboard/ownerDashboardApi';
import { useGetPendingReviewJobAdsQuery } from '@/features/jobAds/jobAdApi';
import { useGetPendingBannerAdsQuery } from '@/features/ads/bannerAdApi';
import { useGetPendingCompaniesQuery } from '@/features/companies/companyApi';
import RevenueBarChart from '@/features/dashboard/RevenueBarChart';
import AdminJobAdReviewPage from '@/features/jobAds/AdminJobAdReviewPage';
import AdminBannerAdReviewPage from '@/features/ads/AdminBannerAdReviewPage';
import AdminCompanyReviewPage from '@/features/companies/AdminCompanyReviewPage';
import { ChartIcon, BuildingIcon, BriefcaseIcon, MegaphoneIcon } from '@/components/icons/DashboardIcons';

function StatCard({ icon, label, value }: { icon: React.ReactNode; label: string; value: string | number }) {
  return (
    <motion.div
      className="card owner-stat-card"
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.35 }}
    >
      <span className="owner-stat-card__icon">{icon}</span>
      <div>
        <div className="owner-stat-card__value">{typeof value === 'number' ? value.toLocaleString('fa-IR') : value}</div>
        <div className="text-muted">{label}</div>
      </div>
    </motion.div>
  );
}

function formatRials(amount: number): string {
  return `${(amount / 10).toLocaleString('fa-IR')} تومان`;
}

type TabKey = 'overview' | 'jobads' | 'banners' | 'companies';

/**
 * پنل یکپارچه مدیریت Owner — طبق تسک #81.
 * داشبورد آمار، تایید آگهی‌ها، تایید بنرهای تبلیغاتی و تایید شرکت‌ها همگی در یک صفحه با تب
 * ادغام شده‌اند تا مدیر پلتفرم بدون رفت‌وآمد بین صفحات مجزا، کل صف بررسی را مدیریت کند.
 * هر تب همچنان از طریق querystring (?tab=) قابل بوکمارک/لینک مستقیم است.
 */
export default function OwnerDashboardPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const initialTab = (searchParams.get('tab') as TabKey) ?? 'overview';
  const [activeTab, setActiveTab] = useState<TabKey>(initialTab);

  const { data, isLoading } = useGetOwnerDashboardQuery({ timeSeriesDays: 30 });
  const dashboard = data?.data;

  // شمارنده‌های صف بررسی برای نشان (badge) روی هر تب — کوئری سبک با pageSize=1 صرفاً برای خواندن totalCount.
  const { data: pendingJobAdsData } = useGetPendingReviewJobAdsQuery({ page: 1, pageSize: 1 });
  const { data: pendingBannersData } = useGetPendingBannerAdsQuery({ page: 1, pageSize: 1 });
  const { data: pendingCompaniesData } = useGetPendingCompaniesQuery({ page: 1, pageSize: 1 });

  const pendingJobAdsCount = pendingJobAdsData?.data?.totalCount ?? 0;
  const pendingBannersCount = pendingBannersData?.data?.totalCount ?? 0;
  const pendingCompaniesCount = pendingCompaniesData?.data?.totalCount ?? 0;

  const switchTab = (tab: TabKey) => {
    setActiveTab(tab);
    setSearchParams(tab === 'overview' ? {} : { tab });
  };

  const tabs: { key: TabKey; label: string; icon: React.ReactNode; badge?: number }[] = [
    { key: 'overview', label: 'نمای کلی', icon: <ChartIcon /> },
    { key: 'jobads', label: 'بررسی آگهی‌ها', icon: <BriefcaseIcon />, badge: pendingJobAdsCount },
    { key: 'banners', label: 'بررسی بنرها', icon: <MegaphoneIcon />, badge: pendingBannersCount },
    { key: 'companies', label: 'بررسی شرکت‌ها', icon: <BuildingIcon />, badge: pendingCompaniesCount }
  ];

  return (
    <div className="container">
      <h1>پنل مدیریت پلتفرم</h1>
      <p className="text-muted" style={{ marginTop: '-0.5rem' }}>
        آمار کلی، درآمد و صف بررسی آگهی‌ها/بنرها/شرکت‌ها — همه در یک پنل یکپارچه.
      </p>

      <div className="owner-tabs">
        {tabs.map((tab) => (
          <button
            key={tab.key}
            type="button"
            className={`owner-tab${activeTab === tab.key ? ' is-active' : ''}`}
            onClick={() => switchTab(tab.key)}
          >
            {tab.icon} {tab.label}
            {!!tab.badge && <span className="owner-tab__badge">{tab.badge.toLocaleString('fa-IR')}</span>}
          </button>
        ))}
      </div>

      <AnimatePresence mode="wait">
        {activeTab === 'overview' && (
          <motion.div key="overview" initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }} transition={{ duration: 0.25 }}>
            {isLoading && <p>در حال بارگذاری...</p>}

            {!isLoading && dashboard && (
              <>
                <div className="section-title">
                  <h2>آمار کلی پلتفرم</h2>
                </div>
                <div className="stats-row" style={{ flexWrap: 'wrap' }}>
                  <StatCard icon={<BuildingIcon />} label="شرکت‌ها (کل)" value={dashboard.overview.totalCompanies} />
                  <StatCard icon={<BuildingIcon />} label="شرکت‌های تایید‌شده" value={dashboard.overview.verifiedCompanies} />
                  <StatCard icon={<BriefcaseIcon />} label="آگهی‌ها (کل)" value={dashboard.overview.totalJobAds} />
                  <StatCard icon={<BriefcaseIcon />} label="آگهی‌های منتشرشده" value={dashboard.overview.publishedJobAds} />
                  <StatCard icon={<BriefcaseIcon />} label="در انتظار بررسی" value={dashboard.overview.pendingReviewJobAds} />
                  <StatCard icon={<ChartIcon />} label="کارجویان" value={dashboard.overview.totalCandidates} />
                  <StatCard icon={<ChartIcon />} label="درخواست‌های همکاری" value={dashboard.overview.totalApplications} />
                  <StatCard icon={<MegaphoneIcon />} label="بنرهای فعال" value={dashboard.overview.activeBannerAds} />
                </div>

                <div className="section-title" style={{ marginTop: '2rem' }}>
                  <h2>آمار مالی / درآمد</h2>
                </div>
                <div className="stats-row" style={{ flexWrap: 'wrap' }}>
                  <StatCard icon={<ChartIcon />} label="درآمد کل" value={formatRials(dashboard.revenue.totalRevenueInRials)} />
                  <StatCard icon={<BriefcaseIcon />} label="درآمد از هزینه آگهی" value={formatRials(dashboard.revenue.jobAdFeeRevenueInRials)} />
                  <StatCard icon={<MegaphoneIcon />} label="درآمد از هزینه بنر" value={formatRials(dashboard.revenue.bannerAdFeeRevenueInRials)} />
                  <StatCard icon={<ChartIcon />} label="تراکنش موفق" value={dashboard.revenue.totalSuccessfulTransactions} />
                </div>

                <div className="section-title" style={{ marginTop: '2rem' }}>
                  <h2>روند درآمد روزانه (۳۰ روز اخیر)</h2>
                </div>
                <div className="card">
                  <RevenueBarChart data={dashboard.revenueTimeSeries} />
                </div>
              </>
            )}
          </motion.div>
        )}

        {activeTab === 'jobads' && (
          <motion.div key="jobads" initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }} transition={{ duration: 0.25 }}>
            <AdminJobAdReviewPage embedded />
          </motion.div>
        )}

        {activeTab === 'banners' && (
          <motion.div key="banners" initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }} transition={{ duration: 0.25 }}>
            <AdminBannerAdReviewPage embedded />
          </motion.div>
        )}

        {activeTab === 'companies' && (
          <motion.div key="companies" initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }} transition={{ duration: 0.25 }}>
            <AdminCompanyReviewPage embedded />
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
