import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import {
  useGetMyCompanyQuery,
  useGetMyCompanyDashboardQuery,
  type CompanyDashboard,
  type CompanyDashboardKpiTrend,
} from "@/features/companies/companyApi";
import {
  useGetMyCompanyJobAdsQuery,
  type JobAd,
} from "@/features/jobAds/jobAdApi";
import { useGetRecentApplicantsForCompanyQuery } from "@/features/applications/applicationApi";
import { useZoneLabelMap } from "@/features/geography/useZoneLabel";
import { JobAdStatusLabels } from "@/shared/enums";
import {
  BriefcaseIcon,
  UsersIcon,
  WalletIcon,
  DotsIcon,
  DownloadIcon,
} from "@/components/icons/DashboardIcons";
import { EyeIcon } from "@/components/icons/AuthIcons";
import Sparkline from "@/features/dashboard/Sparkline";
import SmoothAreaChart from "@/features/dashboard/SmoothAreaChart";
import ViewsByJobAdDonut from "@/features/dashboard/ViewsByJobAdDonut";

/** رنگ بج درصد تطابق — طبق سند 04_company_dashboard_spec.md بخش ۴.۲. */
function matchScoreColor(score: number): { bg: string; fg: string } {
  if (score >= 75) return { bg: "#dcfce7", fg: "#166534" };
  if (score >= 60) return { bg: "#fef9c3", fg: "#854d0e" };
  return { bg: "#ffedd5", fg: "#9a3412" };
}

const statusBadgeColors: Record<string, { bg: string; fg: string }> = {
  Draft: { bg: "#f3f4f6", fg: "#374151" },
  PendingReview: { bg: "#fef9c3", fg: "#854d0e" },
  Published: { bg: "#dcfce7", fg: "#166534" },
  Rejected: { bg: "#fee2e2", fg: "#991b1b" },
  Expired: { bg: "#fce7f3", fg: "#9d174d" },
  Closed: { bg: "#e5e7eb", fg: "#4b5563" },
  Archived: { bg: "#e5e7eb", fg: "#4b5563" },
};

function timeAgoFa(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime();
  const minutes = Math.floor(diffMs / 60_000);
  if (minutes < 1) return "همین الان";
  if (minutes < 60) return `${minutes.toLocaleString("fa-IR")} دقیقه پیش`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours.toLocaleString("fa-IR")} ساعت پیش`;
  const days = Math.floor(hours / 24);
  if (days < 30) return `${days.toLocaleString("fa-IR")} روز پیش`;
  return new Date(iso).toLocaleDateString("fa-IR");
}

/**
 * یک کارت KPI با درصد رشد و ریزنمودار — طبق بازطراحی پیکسل‌به‌پیکسل ردیف کارت‌های آماری داشبورد
 * کارفرما: ردیف بالا (برچسب راست + آیکون رنگی چپ)، عدد بزرگ وسط، ردیف پایین (زیرنویس راست + بج
 * روند سبز/قرمز چپ)، و اسپارک‌لاین تمام‌عرض که تا لبهٔ پایین کارت ادامه پیدا می‌کند.
 */
function KpiTrendCard({
  icon,
  iconBg,
  iconColor,
  value,
  label,
  growthPercent,
  growthSubLabel,
  sparkline,
  sparklineColor,
}: {
  icon: React.ReactNode;
  iconBg: string;
  iconColor: string;
  value: string;
  label: string;
  growthPercent: number;
  growthSubLabel: string;
  sparkline: number[];
  sparklineColor: string;
}) {
  const isPositive = growthPercent >= 0;
  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
      className="flex flex-col overflow-hidden rounded-2xl border border-slate-100 bg-white p-4 shadow-sm transition hover:shadow-md"
    >
      <div className="flex items-start justify-between gap-2">
        <span className="text-xs font-medium text-slate-500">{label}</span>
        <span
          className={`inline-flex shrink-0 items-center justify-center rounded-xl p-2.5 ${iconBg} ${iconColor}`}
        >
          {icon}
        </span>
      </div>

      <div className="text-2xl font-extrabold text-slate-800">{value}</div>

      <div className="flex items-center justify-between gap-2">
        <span className="text-[11px] text-slate-400">{growthSubLabel}</span>
        <span
          className={`inline-flex shrink-0 items-center gap-0.5 rounded-full px-2 py-0.5 text-[11px] font-bold ${
            isPositive
              ? "bg-emerald-50 text-emerald-600"
              : "bg-rose-50 text-rose-600"
          }`}
        >
          {isPositive ? "↑" : "↓"}{" "}
          {Math.abs(growthPercent).toLocaleString("fa-IR")}٪
        </span>
      </div>

      {/* اسپارک‌لاین تمام‌عرض، بریده تا لبهٔ کارت (Bleed) — با overflow-hidden کارت، زیر منحنی نرم می‌شود. */}
      <div className="-mx-4 -mb-4 ">
        <Sparkline data={sparkline} color={sparklineColor} height={40} />
      </div>
    </motion.div>
  );
}

function WalletKpiCard({ amountToman }: { amountToman: string }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
      className="flex flex-col rounded-2xl border border-slate-100 bg-white p-4 shadow-sm transition hover:shadow-md"
    >
      <div className="flex items-start justify-between gap-2">
        <span className="text-xs font-medium text-slate-500">مانده حساب</span>
        <span className="inline-flex shrink-0 items-center justify-center rounded-xl bg-amber-50 p-2.5 text-amber-600">
          <WalletIcon />
        </span>
      </div>

      <div className="mt-3 truncate text-2xl font-extrabold text-slate-800">
        {amountToman} تومان
      </div>

      <div className="mt-2">
        <Link
          to="/company/transactions"
          className="text-xs font-semibold text-amber-600 hover:text-amber-700"
          style={{ textDecoration: "none" }}
        >
          تراکنش‌ها ←
        </Link>
      </div>
    </motion.div>
  );
}

const DATE_FILTER_OPTIONS = [
  { value: "3", label: "۳ روز گذشته" },
  { value: "7", label: "۷ روز گذشته" },
  { value: "30", label: "۳۰ روز گذشته" },
  { value: "all", label: "همه آگهی‌ها" },
];

const TABLE_PAGE_SIZE = 5;

/**
 * نمای کلی داشبورد شرکت — طبق فاز بازطراحی پیکسل‌به‌پیکسل، تمام ارقام از /companies/me/dashboard
 * (که این فاز با KPIهای روند/رشد و داده‌های نمودار گسترش یافت) خوانده می‌شود. هیچ عدد جعلی نمایش
 * داده نمی‌شود؛ «مجموع پرداختی» جایگزین صادقانه‌تر مفهوم «موجودی حساب» است چون این پلتفرم مفهوم
 * کیف‌پول واقعی ندارد، و رشد/ریزنمودار بازدید تا وقتی داده تاریخی کافی از CompanyDailyViewStat
 * جمع نشده صفر نمایش داده می‌شود (بدون Backfill جعلی).
 */
export default function CompanyDashboardHomePage() {
  const { data: myCompany } = useGetMyCompanyQuery();
  const company = myCompany?.data;
  const { data: dashboardData, isLoading } = useGetMyCompanyDashboardQuery(
    undefined,
    { skip: !company },
  );
  const dashboard = dashboardData?.data;
  const { data: jobAdsData } = useGetMyCompanyJobAdsQuery(
    { page: 1, pageSize: 100 },
    { skip: !company },
  );
  const allJobAds = useMemo(() => jobAdsData?.data?.items ?? [], [jobAdsData]);
  const { data: recentApplicantsData } = useGetRecentApplicantsForCompanyQuery(
    { count: 5 },
    { skip: !company },
  );
  const recentApplicants = recentApplicantsData?.data ?? [];
  const zoneLabelById = useZoneLabelMap();

  const [dateFilter, setDateFilter] = useState<string>("30");
  const [tablePage, setTablePage] = useState(1);
  const [openMenuAdId, setOpenMenuAdId] = useState<string | null>(null);

  const applicationCountByJobAdId = useMemo(
    () =>
      new Map(
        (dashboard?.applicationsPerJobAd ?? []).map((x) => [
          x.jobAdId,
          x.applicationCount,
        ]),
      ),
    [dashboard],
  );

  const filteredJobAds = useMemo(() => {
    const sorted = [...allJobAds].sort((a, b) => {
      const dateA = a.publishedAtUtc ?? a.submittedForReviewAtUtc ?? "";
      const dateB = b.publishedAtUtc ?? b.submittedForReviewAtUtc ?? "";
      return dateB.localeCompare(dateA);
    });

    if (dateFilter === "all") return sorted;

    const cutoff = Date.now() - Number(dateFilter) * 24 * 60 * 60 * 1000;
    return sorted.filter((ad) => {
      const referenceDate = ad.publishedAtUtc ?? ad.submittedForReviewAtUtc;
      return !referenceDate || new Date(referenceDate).getTime() >= cutoff;
    });
  }, [allJobAds, dateFilter]);

  const totalPages = Math.max(
    1,
    Math.ceil(filteredJobAds.length / TABLE_PAGE_SIZE),
  );
  const pageStart = (tablePage - 1) * TABLE_PAGE_SIZE;
  const pagedJobAds = filteredJobAds.slice(
    pageStart,
    pageStart + TABLE_PAGE_SIZE,
  );

  if (!company) {
    return (
      <div className="rounded-2xl border border-slate-100 bg-white p-6 text-center">
        <p className="text-slate-600">
          برای مشاهده داشبورد ابتدا باید اطلاعات شرکت خود را ثبت کنید.
        </p>
        <Link
          to="/company"
          className="btn-primary mt-3 inline-block"
          style={{ textDecoration: "none" }}
        >
          ثبت شرکت من
        </Link>
      </div>
    );
  }

  // مقادیر پیش‌فرض امن — طبق الزام صریح: کارت‌های KPI هرگز نباید به‌خاطر لودینگ/خطا/حساب تازه ناپدید
  // شوند؛ همیشه با صفر رندر می‌شوند و فقط وقتی داده واقعی برسد جای مقادیر صفر را می‌گیرد.
  const emptyKpi: CompanyDashboardKpiTrend = {
    currentValue: 0,
    growthPercent: 0,
    sparkline: new Array(14).fill(0),
  };
  const safeDashboard: CompanyDashboard = {
    jobAdStats: dashboard?.jobAdStats ?? {
      totalJobAds: 0,
      publishedJobAds: 0,
      pendingReviewJobAds: 0,
      rejectedJobAds: 0,
      expiredOrClosedJobAds: 0,
    },
    applicationsPerJobAd: dashboard?.applicationsPerJobAd ?? [],
    bannerAdStats: dashboard?.bannerAdStats ?? {
      totalBannerAds: 0,
      activeBannerAds: 0,
      pendingReviewBannerAds: 0,
    },
    paymentSummary: dashboard?.paymentSummary ?? {
      totalPaidInRials: 0,
      totalSuccessfulTransactions: 0,
    },
    activeJobAdsKpi: dashboard?.activeJobAdsKpi ?? emptyKpi,
    viewsKpi: dashboard?.viewsKpi ?? emptyKpi,
    newApplicantsKpi: dashboard?.newApplicantsKpi ?? emptyKpi,
    viewsByJobAd: dashboard?.viewsByJobAd ?? [],
    viewsTrend30Days: dashboard?.viewsTrend30Days ?? [],
  };

  const newJobAdsIn14Days = safeDashboard.activeJobAdsKpi.sparkline.reduce(
    (sum, x) => sum + x,
    0,
  );
  const totalPaidToman = Math.round(
    safeDashboard.paymentSummary.totalPaidInRials / 10,
  ).toLocaleString("fa-IR");

  return (
    <div>
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        {/* <div>
          <h1 className="text-xl font-extrabold text-slate-800">داشبورد کارفرما</h1>
          <p className="text-sm text-slate-500">خوش آمدید! {company.name}</p>
        </div> */}
        {isLoading && (
          <span className="text-xs text-slate-400">
            در حال به‌روزرسانی آمار...
          </span>
        )}
      </div>

      {/* ردیف کارت‌های آماری KPI */}
      <div className="mb-2 grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-4">
        <KpiTrendCard
          icon={<BriefcaseIcon />}
          iconBg="bg-blue-50"
          iconColor="text-blue-600"
          value={safeDashboard.activeJobAdsKpi.currentValue.toLocaleString(
            "fa-IR",
          )}
          label="تعداد آگهی‌های فعال"
          growthPercent={safeDashboard.activeJobAdsKpi.growthPercent}
          growthSubLabel={`${newJobAdsIn14Days.toLocaleString("fa-IR")} آگهی جدید`}
          sparkline={safeDashboard.activeJobAdsKpi.sparkline}
          sparklineColor="#3b82f6"
        />
        <KpiTrendCard
          icon={<EyeIcon />}
          iconBg="bg-emerald-50"
          iconColor="text-emerald-600"
          value={safeDashboard.viewsKpi.currentValue.toLocaleString("fa-IR")}
          label="مجموع بازدیدها"
          growthPercent={safeDashboard.viewsKpi.growthPercent}
          growthSubLabel="از ۳۰ روز گذشته"
          sparkline={safeDashboard.viewsKpi.sparkline}
          sparklineColor="#10b981"
        />
        <KpiTrendCard
          icon={<UsersIcon />}
          iconBg="bg-purple-50"
          iconColor="text-purple-600"
          value={safeDashboard.newApplicantsKpi.currentValue.toLocaleString(
            "fa-IR",
          )}
          label="رزومه‌های جدید دریافتی"
          growthPercent={safeDashboard.newApplicantsKpi.growthPercent}
          growthSubLabel="از ۳۰ روز گذشته"
          sparkline={safeDashboard.newApplicantsKpi.sparkline}
          sparklineColor="#8b5cf6"
        />
        <WalletKpiCard amountToman={totalPaidToman} />
      </div>

      {/* بخش میانی: جدول آگهی‌های فعال + ستون کناری */}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-[2fr_1fr]">
        <div className="rounded-2xl border border-slate-100 bg-white p-4 shadow-sm">
          <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
            <h2 className="flex items-center gap-2 text-sm font-bold text-slate-700">
              <BriefcaseIcon size={16} /> آگهی‌های فعال
            </h2>
            <Link
              to="/company/job-ads"
              className="text-xs font-medium text-emerald-600 hover:text-emerald-700"
              style={{ textDecoration: "none" }}
            >
              مشاهده همه آگهی‌ها ←
            </Link>
          </div>

          {pagedJobAds.length === 0 && (
            <p className="text-sm text-slate-400">
              آگهی‌ای در این بازه یافت نشد.
            </p>
          )}

          {pagedJobAds.length > 0 && (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[560px] border-collapse text-right text-xs">
                <thead>
                  <tr className="border-b border-slate-100 text-slate-400">
                    <th className="py-2 font-medium">
                      عنوان شغلی و شهرک صنعتی
                    </th>
                    <th className="py-2 font-medium">وضعیت</th>
                    <th className="py-2 font-medium">تاریخ انتشار</th>
                    <th className="py-2 font-medium">بازدید</th>
                    <th className="py-2 font-medium">رزومه‌ها</th>
                    <th className="py-2 font-medium"></th>
                  </tr>
                </thead>
                <tbody>
                  {pagedJobAds.map((ad: JobAd) => (
                    <tr
                      key={ad.id}
                      className="border-b border-slate-50 align-middle text-slate-700"
                    >
                      <td className="max-w-[220px] py-2.5">
                        <div className="truncate font-semibold">{ad.title}</div>
                        <div className="truncate text-[11px] text-slate-400">
                          {zoneLabelById.get(ad.industrialZoneId) ?? "—"}
                        </div>
                      </td>
                      <td className="py-2.5">
                        <span
                          className="whitespace-nowrap rounded-full px-2.5 py-1 text-[11px] font-medium"
                          style={{
                            background: (
                              statusBadgeColors[ad.status] ??
                              statusBadgeColors.Draft
                            ).bg,
                            color: (
                              statusBadgeColors[ad.status] ??
                              statusBadgeColors.Draft
                            ).fg,
                          }}
                        >
                          {JobAdStatusLabels[ad.status] ?? ad.status}
                        </span>
                      </td>
                      <td className="whitespace-nowrap py-2.5 text-slate-500">
                        {ad.publishedAtUtc
                          ? new Date(ad.publishedAtUtc).toLocaleDateString(
                              "fa-IR",
                            )
                          : "—"}
                      </td>
                      <td className="py-2.5 text-slate-500">
                        {ad.viewsCount.toLocaleString("fa-IR")}
                      </td>
                      <td className="py-2.5 text-slate-500">
                        {(
                          applicationCountByJobAdId.get(ad.id) ?? 0
                        ).toLocaleString("fa-IR")}
                      </td>
                      <td className="relative py-2.5">
                        <button
                          type="button"
                          className="rounded-lg p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600"
                          onClick={() =>
                            setOpenMenuAdId(
                              openMenuAdId === ad.id ? null : ad.id,
                            )
                          }
                          aria-label="عملیات آگهی"
                        >
                          <DotsIcon size={16} />
                        </button>
                        {openMenuAdId === ad.id && (
                          <div
                            className="absolute left-0 top-8 z-10 w-44 rounded-xl border border-slate-100 bg-white py-1.5 text-xs shadow-lg"
                            onMouseLeave={() => setOpenMenuAdId(null)}
                          >
                            <Link
                              to={`/company/job-ads/${ad.id}/applications`}
                              className="block px-3 py-2 text-slate-600 hover:bg-slate-50"
                              style={{ textDecoration: "none" }}
                              onClick={() => setOpenMenuAdId(null)}
                            >
                              مشاهده درخواست‌ها
                            </Link>
                            <Link
                              to="/company/job-ads"
                              className="block px-3 py-2 text-slate-600 hover:bg-slate-50"
                              style={{ textDecoration: "none" }}
                              onClick={() => setOpenMenuAdId(null)}
                            >
                              ویرایش / مدیریت آگهی
                            </Link>
                          </div>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          <div className="mt-2 flex flex-wrap items-center justify-between gap-2 border-t border-slate-50 pt-3">
            <select
              value={dateFilter}
              onChange={(e) => {
                setDateFilter(e.target.value);
                setTablePage(1);
              }}
              className="rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs text-slate-600"
            >
              {DATE_FILTER_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  نمایش: {opt.label}
                </option>
              ))}
            </select>

            <div className="flex items-center gap-1 text-xs text-slate-500">
              <button
                type="button"
                disabled={tablePage <= 1}
                onClick={() => setTablePage((p) => Math.max(1, p - 1))}
                className="rounded-lg px-2 py-1 hover:bg-slate-100 disabled:opacity-30"
              >
                ‹
              </button>
              <span>
                {tablePage.toLocaleString("fa-IR")} از{" "}
                {totalPages.toLocaleString("fa-IR")}
              </span>
              <button
                type="button"
                disabled={tablePage >= totalPages}
                onClick={() => setTablePage((p) => Math.min(totalPages, p + 1))}
                className="rounded-lg px-2 py-1 hover:bg-slate-100 disabled:opacity-30"
              >
                ›
              </button>
            </div>
          </div>
        </div>

        <div className="flex flex-col gap-4">
          <Link
            to="/company/job-ads"
            className="flex items-center justify-center rounded-2xl bg-amber-500 px-4 py-3.5 text-center text-sm font-bold text-white shadow-sm transition hover:bg-amber-600"
            style={{ textDecoration: "none" }}
          >
            + ثبت آگهی استخدام جدید (۵۰۰,۰۰۰ تومان)
          </Link>

          <div className="rounded-2xl border border-slate-100 bg-white p-4 shadow-sm">
            <div className="mb-3 flex items-center justify-between">
              <h2 className="text-sm font-bold text-slate-700">
                آخرین رزومه‌های دریافتی
              </h2>
            </div>

            {recentApplicants.length === 0 && (
              <p className="text-sm text-slate-400">
                هنوز رزومه‌ای دریافت نکرده‌اید.
              </p>
            )}

            <div className="flex flex-col divide-y divide-slate-100">
              {recentApplicants.map((applicant) => (
                <div
                  key={applicant.applicationId}
                  className="flex items-center gap-3 py-3"
                >
                  {applicant.candidateAvatarUrl ? (
                    <img
                      src={applicant.candidateAvatarUrl}
                      alt={applicant.candidateFullName}
                      className="h-10 w-10 shrink-0 rounded-full object-cover"
                    />
                  ) : (
                    <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-slate-100 text-sm font-bold text-slate-500">
                      {applicant.candidateFullName.charAt(0)}
                    </span>
                  )}
                  <Link
                    to={`/company/job-ads/${applicant.jobAdId}/applications`}
                    className="min-w-0 flex-1"
                    style={{ textDecoration: "none" }}
                  >
                    <div className="truncate text-sm font-semibold text-slate-700">
                      {applicant.candidateFullName}
                    </div>
                    <div className="truncate text-[11px] text-slate-400">
                      {applicant.candidateWorkExperienceSummary ??
                        applicant.jobAdTitle}{" "}
                      — {timeAgoFa(applicant.createdAtUtc)}
                    </div>
                  </Link>
                  {applicant.matchScorePercent !== null && (
                    <span
                      className="shrink-0 rounded-full px-2 py-0.5 text-[11px] font-semibold"
                      style={{
                        background: matchScoreColor(applicant.matchScorePercent)
                          .bg,
                        color: matchScoreColor(applicant.matchScorePercent).fg,
                      }}
                    >
                      {applicant.matchScorePercent.toLocaleString("fa-IR")}٪
                    </span>
                  )}
                  {applicant.candidateResumeFileUrl && (
                    <a
                      href={applicant.candidateResumeFileUrl}
                      target="_blank"
                      rel="noreferrer"
                      className="shrink-0 rounded-lg p-1.5 text-slate-400 hover:bg-slate-100 hover:text-emerald-600"
                      aria-label="دانلود رزومه"
                    >
                      <DownloadIcon size={16} />
                    </a>
                  )}
                </div>
              ))}
            </div>

            <div className="mt-2 border-t border-slate-50 pt-2 text-left">
              <Link
                to="/company/applicants"
                className="text-xs font-medium text-emerald-600 hover:text-emerald-700"
                style={{ textDecoration: "none" }}
              >
                مشاهده همه رزومه‌ها ←
              </Link>
            </div>
          </div>
        </div>
      </div>

      {/* بخش پایینی: نمودارهای تحلیلی */}
      <div className="mt-2 grid grid-cols-1 gap-4 lg:grid-cols-[2fr_1fr]">
        <div className="rounded-2xl border border-slate-100 bg-white p-4 shadow-sm">
          <h2 className="mb-3 text-sm font-bold text-slate-700">
            گزارش بازدید آگهی‌ها
          </h2>
          <SmoothAreaChart data={safeDashboard.viewsTrend30Days} />
        </div>

        <div className="rounded-2xl border border-slate-100 bg-white p-4 shadow-sm">
          <h2 className="mb-3 text-sm font-bold text-slate-700">
            بازدیدها بر اساس آگهی
          </h2>
          <ViewsByJobAdDonut data={safeDashboard.viewsByJobAd} />
        </div>
      </div>

      {safeDashboard.bannerAdStats.totalBannerAds > 0 && (
        <div className="mt-4 rounded-2xl border border-slate-100 bg-white p-4 shadow-sm">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <h2 className="text-sm font-bold text-slate-700">تبلیغات بنری</h2>
            <Link
              to="/company/banner-ads"
              className="text-xs font-medium text-emerald-600 hover:text-emerald-700"
              style={{ textDecoration: "none" }}
            >
              مدیریت بنرها ←
            </Link>
          </div>
          <p className="mt-1 text-xs text-slate-500">
            {safeDashboard.bannerAdStats.activeBannerAds.toLocaleString(
              "fa-IR",
            )}{" "}
            بنر فعال از{" "}
            {safeDashboard.bannerAdStats.totalBannerAds.toLocaleString("fa-IR")}{" "}
            بنر ثبت‌شده
            {safeDashboard.bannerAdStats.pendingReviewBannerAds > 0 &&
              ` — ${safeDashboard.bannerAdStats.pendingReviewBannerAds.toLocaleString("fa-IR")} در انتظار بررسی`}
          </p>
        </div>
      )}
    </div>
  );
}
