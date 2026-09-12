import { baseApi } from '@/api/baseApi';
import type { ApiResponse, PagedResult } from '@/shared/types';

export interface JobAdListingFee {
  amountInRials: number;
}

export interface SubmitJobAdForReviewResponse {
  paymentRedirectUrl: string;
  amountInRials: number;
}

export type PaymentTransactionStatus = 'Pending' | 'Success' | 'Failed';

/**
 * طبق سیستم فعلی پرداخت — «پکیج ارتقای آگهی» و «شارژ کیف پول» به‌عنوان هدف تراکنش هنوز در بک‌اند وجود ندارند.
 * BannerAdRenewal طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۵) اضافه شد — کسر مستقیم از کیف‌پول، نه زرین‌پال.
 */
export type PaymentPurpose = 'JobAdListingFee' | 'BannerAdFee' | 'ResumeCreationFee' | 'BannerAdRenewal';

/** طبق 04_company_dashboard_spec.md — یک ردیف در «امور مالی و تراکنش‌ها»؛ فقط تراکنش‌های مرتبط با شرکت. */
export interface CompanyTransaction {
  id: string;
  purpose: PaymentPurpose;
  amountInRials: number;
  status: PaymentTransactionStatus;
  authority: string;
  refId: string | null;
  relatedTitle: string;
  createdAtUtc: string;
}

/** خلاصهٔ آماری کارت‌های بالای صفحه — از تجمیع کل تراکنش‌های شرکت (نه فقط صفحهٔ جاری جدول). */
export interface CompanyTransactionSummary {
  walletBalanceInRials: number;
  totalPaidAmountInRials: number;
  successfulTransactionsCount: number;
  totalTransactionsCount: number;
  lastTransactionAtUtc: string | null;
}

export interface CompanyTransactionsFilter {
  page: number;
  pageSize: number;
  status?: PaymentTransactionStatus;
  purpose?: PaymentPurpose;
  fromUtc?: string;
  toUtc?: string;
  search?: string;
}

export const paymentApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getJobAdListingFee: builder.query<ApiResponse<JobAdListingFee>, void>({
      query: () => '/payments/listing-fee'
    }),
    submitJobAdForReview: builder.mutation<ApiResponse<SubmitJobAdForReviewResponse>, string>({
      query: (jobAdId) => ({ url: `/payments/job-ads/${jobAdId}/submit-for-review`, method: 'POST' }),
      invalidatesTags: ['JobAd']
    }),
    getMyCompanyTransactions: builder.query<ApiResponse<PagedResult<CompanyTransaction>>, CompanyTransactionsFilter>({
      query: ({ page, pageSize, status, purpose, fromUtc, toUtc, search }) => ({
        url: '/payments/company/transactions',
        params: { page, pageSize, status, purpose, from: fromUtc, to: toUtc, search }
      }),
      providesTags: ['CompanyTransaction']
    }),
    getMyCompanyTransactionSummary: builder.query<ApiResponse<CompanyTransactionSummary>, void>({
      query: () => '/payments/company/transactions/summary',
      providesTags: ['CompanyTransaction']
    })
  })
});

export const {
  useGetJobAdListingFeeQuery,
  useSubmitJobAdForReviewMutation,
  useGetMyCompanyTransactionsQuery,
  useGetMyCompanyTransactionSummaryQuery
} = paymentApi;
