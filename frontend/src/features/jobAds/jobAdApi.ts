import { baseApi } from '@/api/baseApi';
import type { ApiResponse, PagedResult } from '@/shared/types';

export interface SalaryRangeDto {
  type: string;
  fixedAmount: number | null;
  minAmount: number | null;
  maxAmount: number | null;
}

export interface JobAd {
  id: string;
  companyId: string;
  // طبق ADR-011 — برای نمایش نام شرکت و دکمه «تماس با کارفرما» در صفحه جزئیات آگهی.
  companyName: string;
  companyContactPhoneNumber: string | null;
  companyLogoUrl: string | null;
  isCompanyVerified: boolean;
  industrialZoneId: string;
  title: string;
  description: string;
  workShift: string;
  hasCommuteService: boolean;
  commuteServiceRoutes: string | null;
  mealPlan: string;
  insuranceTypes: string[];
  salaryRange: SalaryRangeDto;
  contractType: string;
  genderPreference: string;
  minAge: number | null;
  maxAge: number | null;
  minEducationLevel: string;
  minExperienceYears: number | null;
  militaryServiceStatus: string | null;
  headcountNeeded: number | null;
  applicationDeadlineUtc: string | null;
  requiredSkills: string | null;
  additionalBenefits: string | null;
  status: string;
  isFeePaid: boolean;
  rejectionReason: string | null;
  listingFeeAmountInRials: number;
  submittedForReviewAtUtc: string | null;
  publishedAtUtc: string | null;
  expiresAtUtc: string | null;
  // طبق تصمیم صریح محصولی فاز جدید — بازدید واقعی و فلگ ویژهٔ ادمین (بدون پلن پرداختی).
  viewsCount: number;
  isFeatured: boolean;
}

export interface JobAdFormValues {
  title: string;
  description: string;
  workShift: string;
  hasCommuteService: boolean;
  commuteServiceRoutes?: string;
  mealPlan: string;
  insuranceTypes: string[];
  salaryRangeType: string;
  fixedAmount?: number;
  minAmount?: number;
  maxAmount?: number;
  contractType: string;
  genderPreference?: string;
  minAge?: number;
  maxAge?: number;
  minEducationLevel?: string;
  minExperienceYears?: number;
  militaryServiceStatus?: string;
  headcountNeeded?: number;
  applicationDeadlineUtc?: string;
  requiredSkills?: string;
  additionalBenefits?: string;
}

export interface JobAdSearchParams {
  industrialZoneId?: string;
  cityId?: string;
  workShift?: string;
  hasCommuteService?: boolean;
  contractType?: string;
  minSalaryAmount?: number;
  keyword?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: string;
}

export const jobAdApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    searchJobAds: builder.query<ApiResponse<PagedResult<JobAd>>, JobAdSearchParams>({
      query: (params) => ({ url: '/job-ads', params }),
      providesTags: ['JobAd']
    }),
    getJobAdById: builder.query<ApiResponse<JobAd>, string>({
      query: (id) => `/job-ads/${id}`,
      providesTags: ['JobAd']
    }),
    // ثبت یک بازدید واقعی — فرانت‌اند این را یک‌بار در mount شدن صفحه جزئیات آگهی فراخوانی می‌کند.
    // عمداً بدون invalidatesTags: یک رفرش کامل لیست/کش برای صرفاً +۱ شدن شمارنده بازدید غیرضروری است.
    recordJobAdView: builder.mutation<ApiResponse<null>, string>({
      query: (id) => ({ url: `/job-ads/${id}/view`, method: 'POST' })
    }),
    getMyCompanyJobAds: builder.query<ApiResponse<PagedResult<JobAd>>, { page: number; pageSize: number }>({
      query: ({ page, pageSize }) => ({ url: '/job-ads/my-company', params: { page, pageSize } }),
      providesTags: ['JobAd']
    }),
    createJobAd: builder.mutation<ApiResponse<JobAd>, JobAdFormValues>({
      query: (body) => ({ url: '/job-ads', method: 'POST', body }),
      invalidatesTags: ['JobAd']
    }),
    updateJobAd: builder.mutation<ApiResponse<JobAd>, { id: string; body: JobAdFormValues }>({
      query: ({ id, body }) => ({ url: `/job-ads/${id}`, method: 'PUT', body }),
      invalidatesTags: ['JobAd']
    }),
    closeJobAd: builder.mutation<ApiResponse<JobAd>, string>({
      query: (id) => ({ url: `/job-ads/${id}/close`, method: 'POST' }),
      invalidatesTags: ['JobAd']
    }),
    // حذف منطقی — طبق فاز بازطراحی «آگهی‌های شرکت من». فقط برای آگهی‌های غیر از در-حال-بررسی/منتشرشده مجاز است (قانون در بک‌اند).
    deleteJobAd: builder.mutation<ApiResponse<null>, string>({
      query: (id) => ({ url: `/job-ads/${id}`, method: 'DELETE' }),
      invalidatesTags: ['JobAd']
    }),

    // ---------- پنل Owner: استعلام/تایید/رد آگهی ----------
    getPendingReviewJobAds: builder.query<ApiResponse<PagedResult<JobAd>>, { page: number; pageSize: number }>({
      query: ({ page, pageSize }) => ({ url: '/admin/job-ads/pending', params: { page, pageSize } }),
      providesTags: ['JobAd']
    }),
    approveJobAd: builder.mutation<ApiResponse<JobAd>, string>({
      query: (id) => ({ url: `/admin/job-ads/${id}/approve`, method: 'POST' }),
      invalidatesTags: ['JobAd']
    }),
    rejectJobAd: builder.mutation<ApiResponse<JobAd>, { id: string; reason: string }>({
      query: ({ id, reason }) => ({ url: `/admin/job-ads/${id}/reject`, method: 'POST', body: { reason } }),
      invalidatesTags: ['JobAd']
    }),
    // فلگ ساده «ویژه» — فقط ادمین، بدون هیچ پلن پرداختی/Boost.
    setJobAdFeatured: builder.mutation<ApiResponse<JobAd>, { id: string; isFeatured: boolean }>({
      query: ({ id, isFeatured }) => ({ url: `/admin/job-ads/${id}/featured`, method: 'PATCH', body: { isFeatured } }),
      invalidatesTags: ['JobAd']
    }),
    // نشان‌کردن/لغو نشان آگهی — Toggle: نتیجه true یعنی اکنون نشان‌شده، false یعنی لغو شد.
    // invalidatesTags روی Bookmark تا لیست شناسه‌های نشان‌شده و صفحهٔ «آگهی‌های نشان‌شده» رفرش شوند.
    toggleBookmark: builder.mutation<ApiResponse<boolean>, string>({
      query: (id) => ({ url: `/job-ads/${id}/bookmark/toggle`, method: 'POST' }),
      invalidatesTags: ['Bookmark']
    })
  })
});

export const {
  useSearchJobAdsQuery,
  useGetJobAdByIdQuery,
  useGetMyCompanyJobAdsQuery,
  useCreateJobAdMutation,
  useUpdateJobAdMutation,
  useCloseJobAdMutation,
  useDeleteJobAdMutation,
  useGetPendingReviewJobAdsQuery,
  useApproveJobAdMutation,
  useRejectJobAdMutation,
  useRecordJobAdViewMutation,
  useSetJobAdFeaturedMutation,
  useToggleBookmarkMutation
} = jobAdApi;
