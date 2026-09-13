import { useEffect, useMemo, useState } from 'react';
import {
  CheckCircle2,
  Clock,
  Copy,
  CreditCard,
  Download,
  FileSpreadsheet,
  FileText,
  Inbox,
  Search,
  Wallet,
  X,
  XCircle
} from 'lucide-react';
import { useAppDispatch } from '@/app/hooks';
import {
  paymentApi,
  useGetMyCompanyTransactionSummaryQuery,
  useGetMyCompanyTransactionsQuery,
  type CompanyTransaction,
  type PaymentPurpose,
  type PaymentTransactionStatus
} from '@/features/payments/paymentApi';
import { PaymentPurposeLabels, PaymentTransactionStatusLabels } from '@/shared/enums';
import { useToast } from '@/features/toast/useToast';
import { formatToJalali, formatRelativeJalali } from '@/shared/utils/date';

type StatusFilter = 'All' | PaymentTransactionStatus;
type PurposeFilter = 'All' | PaymentPurpose;
type DateRangeFilter = 'All' | '30d' | '3m';

/** فقط این سه هدف واقعاً برای شرکت رخ می‌دهند (ResumeCreationFee متعلق به کارجوست، نه شرکت). */
const COMPANY_PURPOSE_OPTIONS: PaymentPurpose[] = ['JobAdListingFee', 'BannerAdFee', 'BannerAdRenewal'];
const STATUS_OPTIONS: PaymentTransactionStatus[] = ['Success', 'Failed', 'Pending'];
const PAGE_SIZE_OPTIONS = [5, 10, 20, 50];

const STATUS_STYLES: Record<PaymentTransactionStatus, { bg: string; fg: string; border: string; label: string; Icon: typeof CheckCircle2 }> = {
  Success: { bg: '#ecfdf5', fg: '#047857', border: '#a7f3d0', label: PaymentTransactionStatusLabels.Success, Icon: CheckCircle2 },
  Failed: { bg: '#fff1f2', fg: '#be123c', border: '#fecdd3', label: PaymentTransactionStatusLabels.Failed, Icon: XCircle },
  Pending: { bg: '#fffbeb', fg: '#b45309', border: '#fde68a', label: PaymentTransactionStatusLabels.Pending, Icon: Clock }
};

const toToman = (rials: number) => Math.round(rials / 10).toLocaleString('fa-IR');

const formatDateTime = (iso: string) => formatToJalali(iso, 'yyyy/MM/dd - HH:mm');

const relativeFromNow = (iso: string): string => formatRelativeJalali(iso);

function downloadCsv(filename: string, rows: string[][]) {
  const csvContent = rows.map((row) => row.map((cell) => `"${cell.replace(/"/g, '""')}"`).join(',')).join('\r\n');
  const blob = new Blob([`﻿${csvContent}`], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  link.click();
  URL.revokeObjectURL(url);
}

function KpiCard({
  icon: Icon,
  iconBg,
  iconFg,
  label,
  value,
  hint
}: {
  icon: typeof Wallet;
  iconBg: string;
  iconFg: string;
  label: string;
  value: string;
  hint?: string;
}) {
  return (
    <div className="rounded-2xl border border-slate-200/80 bg-white p-4 shadow-sm">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-xs text-slate-400">{label}</p>
          <p className="mt-1.5 text-lg font-bold text-slate-900">{value}</p>
          {hint && <p className="mt-0.5 text-[11px] text-slate-400">{hint}</p>}
        </div>
        <span className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-xl" style={{ background: iconBg, color: iconFg }}>
          <Icon className="h-5 w-5" />
        </span>
      </div>
    </div>
  );
}

/**
 * صفحهٔ «امور مالی و تراکنش‌ها» در داشبورد کارفرما — طبق موکاپ جدید. KPIها و فیلترها همگی روی داده
 * واقعی GetMyCompanyTransactionsQuery/GetMyCompanyTransactionSummaryQuery سوار هستند؛ دسته‌بندی «پکیج
 * ارتقای آگهی» و «شارژ کیف پول» چون در بک‌اند فعلی وجود ندارند (سیستم پلن‌های ارتقاء حذف شده و شارژ
 * کیف پول هنوز پیاده‌سازی نشده)، در فیلتر نوع تراکنش عمداً نیامده‌اند تا داده/گزینهٔ ساختگی نمایش داده نشود.
 */
export default function CompanyTransactionsPage() {
  const dispatch = useAppDispatch();
  const toast = useToast();

  const [searchInput, setSearchInput] = useState('');
  const [appliedSearch, setAppliedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('All');
  const [purposeFilter, setPurposeFilter] = useState<PurposeFilter>('All');
  const [dateRangeFilter, setDateRangeFilter] = useState<DateRangeFilter>('All');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [invoiceTransaction, setInvoiceTransaction] = useState<CompanyTransaction | null>(null);
  const [isExporting, setIsExporting] = useState(false);

  useEffect(() => {
    const timeout = setTimeout(() => {
      setAppliedSearch(searchInput.trim());
      setPage(1);
    }, 400);
    return () => clearTimeout(timeout);
  }, [searchInput]);

  const fromUtc = useMemo(() => {
    if (dateRangeFilter === 'All') return undefined;
    const days = dateRangeFilter === '30d' ? 30 : 90;
    const date = new Date();
    date.setDate(date.getDate() - days);
    return date.toISOString();
  }, [dateRangeFilter]);

  const queryArgs = {
    page,
    pageSize,
    status: statusFilter === 'All' ? undefined : statusFilter,
    purpose: purposeFilter === 'All' ? undefined : purposeFilter,
    fromUtc,
    search: appliedSearch || undefined
  };

  const { data, isLoading, isFetching } = useGetMyCompanyTransactionsQuery(queryArgs);
  const { data: summaryData, isLoading: isSummaryLoading } = useGetMyCompanyTransactionSummaryQuery();

  const transactions = data?.data?.items ?? [];
  const totalCount = data?.data?.totalCount ?? 0;
  const totalPages = data?.data?.totalPages ?? 1;
  const summary = summaryData?.data;

  const hasActiveFilters = statusFilter !== 'All' || purposeFilter !== 'All' || dateRangeFilter !== 'All' || appliedSearch.length > 0;

  const clearFilters = () => {
    setSearchInput('');
    setAppliedSearch('');
    setStatusFilter('All');
    setPurposeFilter('All');
    setDateRangeFilter('All');
    setPage(1);
  };

  const handleCopy = async (value: string) => {
    try {
      await navigator.clipboard.writeText(value);
      toast.success('کد رهگیری کپی شد.');
    } catch {
      // بی‌صدا نادیده گرفته می‌شود.
    }
  };

  const handleExport = async () => {
    setIsExporting(true);
    try {
      const exportPageSize = Math.min(Math.max(totalCount, 1), 5000);
      const result = await dispatch(
        paymentApi.endpoints.getMyCompanyTransactions.initiate({ ...queryArgs, page: 1, pageSize: exportPageSize })
      ).unwrap();
      const items = result.data?.items ?? [];

      if (items.length === 0) {
        toast.error('تراکنشی برای خروجی گرفتن وجود ندارد.');
        return;
      }

      const rows: string[][] = [
        ['شماره پیگیری', 'کد رهگیری', 'شرح تراکنش', 'تاریخ و زمان', 'مبلغ (تومان)', 'وضعیت'],
        ...items.map((t) => [
          t.authority,
          t.refId ?? '-',
          `${PaymentPurposeLabels[t.purpose] ?? t.purpose} - ${t.relatedTitle}`,
          formatDateTime(t.createdAtUtc),
          toToman(t.amountInRials),
          PaymentTransactionStatusLabels[t.status] ?? t.status
        ])
      ];
      downloadCsv(`transactions-${new Date().toISOString().slice(0, 10)}.csv`, rows);
      toast.success('گزارش با موفقیت دانلود شد.');
    } catch {
      toast.error('دریافت گزارش با خطا مواجه شد.');
    } finally {
      setIsExporting(false);
    }
  };

  const rangeStart = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const rangeEnd = Math.min(page * pageSize, totalCount);

  return (
    <div className="flex flex-col gap-6" dir="rtl">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">امور مالی و تراکنش‌ها</h1>
          <p className="mt-1 text-sm text-slate-500">تاریخچهٔ کامل تراکنش‌های پرداختی شرکت شما (هزینه ثبت آگهی و بنر تبلیغاتی)</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <button
            onClick={handleExport}
            disabled={isExporting || totalCount === 0}
            className="inline-flex items-center gap-2 rounded-xl bg-white px-4 py-2.5 text-sm font-semibold text-slate-600 shadow-sm ring-1 ring-slate-200 transition-colors hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <FileSpreadsheet className="h-4 w-4" />
            {isExporting ? 'در حال آماده‌سازی...' : 'دانلود گزارش اکسل'}
          </button>
          <button
            onClick={() => toast.info('شارژ آنلاین کیف پول به‌زودی فعال می‌شود.')}
            className="inline-flex items-center gap-2 rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-bold text-white transition-colors hover:bg-emerald-700"
          >
            <Wallet className="h-4 w-4" />
            افزایش اعتبار / شارژ کیف پول
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <KpiCard
          icon={Wallet}
          iconBg="#ecfdf5"
          iconFg="#059669"
          label="موجودی کیف پول"
          value={isSummaryLoading ? '...' : `${toToman(summary?.walletBalanceInRials ?? 0)} تومان`}
        />
        <KpiCard
          icon={CreditCard}
          iconBg="#f1f5f9"
          iconFg="#475569"
          label="مجموع پرداخت‌ها"
          value={isSummaryLoading ? '...' : `${toToman(summary?.totalPaidAmountInRials ?? 0)} تومان`}
        />
        <KpiCard
          icon={CheckCircle2}
          iconBg="#ecfdf5"
          iconFg="#059669"
          label="تراکنش‌های موفق"
          value={isSummaryLoading ? '...' : `${(summary?.successfulTransactionsCount ?? 0).toLocaleString('fa-IR')} تراکنش`}
          hint={
            summary && summary.totalTransactionsCount > 0
              ? `${Math.round((summary.successfulTransactionsCount / summary.totalTransactionsCount) * 100).toLocaleString('fa-IR')}٪ از کل تراکنش‌ها`
              : undefined
          }
        />
        <KpiCard
          icon={Clock}
          iconBg="#f1f5f9"
          iconFg="#475569"
          label="آخرین تراکنش"
          value={isSummaryLoading ? '...' : summary?.lastTransactionAtUtc ? relativeFromNow(summary.lastTransactionAtUtc) : '—'}
          hint={summary?.lastTransactionAtUtc ? formatDateTime(summary.lastTransactionAtUtc) : undefined}
        />
      </div>

      <div className="rounded-2xl border border-slate-200/80 bg-white p-4 shadow-sm">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <div className="relative lg:col-span-1">
            <Search className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <input
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              placeholder="جستجو با شماره پیگیری یا شرح تراکنش..."
              className="w-full rounded-lg border border-slate-200 bg-white py-2 pl-3 pr-9 text-sm text-slate-700 focus:border-emerald-500 focus:outline-none"
            />
          </div>

          <select
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value as StatusFilter);
              setPage(1);
            }}
            className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 focus:border-emerald-500 focus:outline-none"
          >
            <option value="All">همه وضعیت‌ها</option>
            {STATUS_OPTIONS.map((s) => (
              <option key={s} value={s}>{PaymentTransactionStatusLabels[s]}</option>
            ))}
          </select>

          <select
            value={purposeFilter}
            onChange={(e) => {
              setPurposeFilter(e.target.value as PurposeFilter);
              setPage(1);
            }}
            className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 focus:border-emerald-500 focus:outline-none"
          >
            <option value="All">همه انواع</option>
            {COMPANY_PURPOSE_OPTIONS.map((p) => (
              <option key={p} value={p}>{PaymentPurposeLabels[p]}</option>
            ))}
          </select>

          <select
            value={dateRangeFilter}
            onChange={(e) => {
              setDateRangeFilter(e.target.value as DateRangeFilter);
              setPage(1);
            }}
            className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 focus:border-emerald-500 focus:outline-none"
          >
            <option value="All">همه تاریخ‌ها</option>
            <option value="30d">۳۰ روز اخیر</option>
            <option value="3m">۳ ماه اخیر</option>
          </select>
        </div>
      </div>

      <div className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-sm">
        {(isLoading || isFetching) && (
          <div className="flex flex-col gap-2 p-4">
            {Array.from({ length: 5 }, (_, i) => (
              <div key={i} className="h-12 animate-pulse rounded-xl bg-slate-100" />
            ))}
          </div>
        )}

        {!isLoading && !isFetching && transactions.length === 0 && (
          <div className="flex flex-col items-center justify-center gap-3 py-16 text-center">
            <Inbox className="h-10 w-10 text-slate-300" />
            <p className="font-semibold text-slate-600">تراکنشی یافت نشد</p>
            {hasActiveFilters && (
              <button
                onClick={clearFilters}
                className="inline-flex items-center gap-1.5 rounded-xl bg-slate-100 px-3.5 py-2 text-xs font-semibold text-slate-600 transition-colors hover:bg-slate-200"
              >
                <X className="h-3.5 w-3.5" />
                پاک کردن فیلترها
              </button>
            )}
          </div>
        )}

        {!isLoading && !isFetching && transactions.length > 0 && (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[860px] border-collapse text-sm">
              <thead>
                <tr className="border-b border-slate-100 text-right text-xs font-semibold text-slate-400">
                  <th className="px-4 py-3">شماره پیگیری / کد رهگیری</th>
                  <th className="px-4 py-3">شرح تراکنش</th>
                  <th className="px-4 py-3">تاریخ و زمان</th>
                  <th className="px-4 py-3">درگاه / روش پرداخت</th>
                  <th className="px-4 py-3">مبلغ</th>
                  <th className="px-4 py-3">وضعیت</th>
                  <th className="px-4 py-3">عملیات</th>
                </tr>
              </thead>
              <tbody>
                {transactions.map((t) => {
                  const style = STATUS_STYLES[t.status];
                  const trackingCode = t.refId ?? t.authority;
                  return (
                    <tr key={t.id} className="border-b border-slate-50 last:border-0 hover:bg-slate-50/40">
                      <td className="px-4 py-3">
                        <button
                          onClick={() => handleCopy(trackingCode)}
                          className="inline-flex items-center gap-1.5 font-mono text-xs font-semibold text-slate-700 transition-colors hover:text-emerald-600"
                          title="کپی کد رهگیری"
                        >
                          {trackingCode}
                          <Copy className="h-3 w-3" />
                        </button>
                      </td>
                      <td className="px-4 py-3">
                        <div className="font-medium text-slate-700">{PaymentPurposeLabels[t.purpose] ?? t.purpose}</div>
                        <div className="mt-0.5 max-w-[260px] truncate text-xs text-slate-400">{t.relatedTitle}</div>
                      </td>
                      <td className="px-4 py-3 text-xs text-slate-500">{formatDateTime(t.createdAtUtc)}</td>
                      <td className="px-4 py-3">
                        <span className="inline-flex items-center gap-1.5 rounded-lg bg-slate-100 px-2.5 py-1 text-[11px] font-medium text-slate-600">
                          {t.purpose === 'BannerAdRenewal' ? <Wallet className="h-3 w-3" /> : <CreditCard className="h-3 w-3" />}
                          {t.purpose === 'BannerAdRenewal' ? 'کیف پول' : 'زرین‌پال'}
                        </span>
                      </td>
                      <td className="px-4 py-3 font-bold text-slate-800">{toToman(t.amountInRials)} تومان</td>
                      <td className="px-4 py-3">
                        <span
                          className="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-semibold"
                          style={{ background: style.bg, color: style.fg, borderColor: style.border }}
                        >
                          <style.Icon className="h-3 w-3" />
                          {style.label}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <button
                          onClick={() => setInvoiceTransaction(t)}
                          className="inline-flex items-center gap-1 rounded-lg bg-slate-100 px-2.5 py-1.5 text-xs font-semibold text-slate-600 transition-colors hover:bg-emerald-50 hover:text-emerald-700"
                        >
                          <FileText className="h-3.5 w-3.5" />
                          مشاهده فاکتور
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        {!isLoading && totalCount > 0 && (
          <div className="flex flex-wrap items-center justify-between gap-3 border-t border-slate-100 px-4 py-3">
            <p className="text-xs text-slate-400">
              نمایش {rangeStart.toLocaleString('fa-IR')} تا {rangeEnd.toLocaleString('fa-IR')} از {totalCount.toLocaleString('fa-IR')} تراکنش
            </p>
            <div className="flex items-center gap-3">
              <select
                value={pageSize}
                onChange={(e) => {
                  setPageSize(Number(e.target.value));
                  setPage(1);
                }}
                className="rounded-lg border border-slate-200 bg-white px-2 py-1.5 text-xs text-slate-600 focus:border-emerald-500 focus:outline-none"
              >
                {PAGE_SIZE_OPTIONS.map((size) => (
                  <option key={size} value={size}>نمایش {size.toLocaleString('fa-IR')}</option>
                ))}
              </select>
              <div className="flex items-center gap-1">
                <button
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page === 1}
                  className="h-8 w-8 rounded-lg text-xs font-semibold text-slate-500 transition-colors hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-40"
                >
                  ‹
                </button>
                {Array.from({ length: totalPages }, (_, i) => i + 1)
                  .filter((p) => p === 1 || p === totalPages || Math.abs(p - page) <= 1)
                  .map((p, idx, arr) => (
                    <div key={p} className="flex items-center gap-1">
                      {idx > 0 && arr[idx - 1] !== p - 1 && <span className="px-1 text-xs text-slate-300">...</span>}
                      <button
                        onClick={() => setPage(p)}
                        className={`h-8 min-w-8 rounded-lg px-2 text-xs font-semibold transition-colors ${
                          p === page ? 'bg-emerald-600 text-white' : 'bg-white text-slate-500 hover:bg-slate-100'
                        }`}
                      >
                        {p.toLocaleString('fa-IR')}
                      </button>
                    </div>
                  ))}
                <button
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  disabled={page === totalPages}
                  className="h-8 w-8 rounded-lg text-xs font-semibold text-slate-500 transition-colors hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-40"
                >
                  ›
                </button>
              </div>
            </div>
          </div>
        )}
      </div>

      {invoiceTransaction && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4" onClick={() => setInvoiceTransaction(null)}>
          <div
            className="w-full max-w-md rounded-2xl bg-white p-5 shadow-xl"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="mb-4 flex items-center justify-between">
              <h2 className="text-base font-bold text-slate-800">فاکتور تراکنش</h2>
              <button onClick={() => setInvoiceTransaction(null)} className="text-slate-400 hover:text-slate-600">
                <X className="h-5 w-5" />
              </button>
            </div>
            <div className="flex flex-col gap-2.5 text-sm">
              <div className="flex items-center justify-between">
                <span className="text-slate-400">شرح</span>
                <span className="font-semibold text-slate-700">{PaymentPurposeLabels[invoiceTransaction.purpose]}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-400">جزئیات</span>
                <span className="max-w-[220px] truncate font-semibold text-slate-700">{invoiceTransaction.relatedTitle}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-400">شماره پیگیری</span>
                <span className="font-mono text-xs font-semibold text-slate-700">{invoiceTransaction.authority}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-400">کد رهگیری</span>
                <span className="font-mono text-xs font-semibold text-slate-700">{invoiceTransaction.refId ?? '—'}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-400">درگاه پرداخت</span>
                <span className="font-semibold text-slate-700">{invoiceTransaction.purpose === 'BannerAdRenewal' ? 'کیف پول' : 'زرین‌پال'}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-400">تاریخ و زمان</span>
                <span className="font-semibold text-slate-700">{formatDateTime(invoiceTransaction.createdAtUtc)}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-400">وضعیت</span>
                <span className="font-semibold text-slate-700">{PaymentTransactionStatusLabels[invoiceTransaction.status]}</span>
              </div>
              <div className="mt-1 flex items-center justify-between border-t border-slate-100 pt-2.5">
                <span className="font-semibold text-slate-500">مبلغ</span>
                <span className="text-base font-extrabold text-slate-900">{toToman(invoiceTransaction.amountInRials)} تومان</span>
              </div>
            </div>
            <button
              onClick={() => window.print()}
              className="mt-5 flex w-full items-center justify-center gap-2 rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-bold text-white transition-colors hover:bg-emerald-700"
            >
              <Download className="h-4 w-4" />
              چاپ / ذخیره فاکتور
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
