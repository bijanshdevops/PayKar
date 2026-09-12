import { baseApi } from '@/api/baseApi';
import type { ApiResponse, PagedResult } from '@/shared/types';

export interface Company {
  id: string;
  name: string;
  nationalId: string;
  registrationNumber: string;
  industrialZoneId: string;
  addressDetail: string;
  industryCategory: string;
  logoUrl: string | null;
  verificationStatus: string;
  // طبق ADR-011 — شماره تماس عمومی برای دکمه «تماس با کارفرما».
  contactPhoneNumber: string | null;
  // طبق فاز بازطراحی صفحه پروفایل شرکت — فیلدهای اختیاری نمایشی.
  bannerUrl: string | null;
  website: string | null;
  email: string | null;
  description: string | null;
}

export interface CompanyDocument {
  id: string;
  companyId: string;
  documentType: string;
  fileName: string;
  fileUrl: string;
  // طبق فاز بازطراحی صفحه پروفایل شرکت — مدارک آپلودشده پیش از این فاز صفر خواهند بود (نامشخص).
  fileSizeBytes: number;
  uploadedAtUtc: string;
}

export interface CreateCompanyRequest {
  name: string;
  nationalId: string;
  registrationNumber: string;
  industrialZoneId: string;
  addressDetail: string;
  industryCategory: string;
  contactPhoneNumber: string;
  website?: string;
  email?: string;
  description?: string;
}

export interface UpdateCompanyRequest {
  name: string;
  addressDetail: string;
  industryCategory: string;
  industrialZoneId: string;
  contactPhoneNumber?: string;
  website?: string;
  email?: string;
  description?: string;
}

// طبق فاز بازطراحی پیکسل‌به‌پیکسل داشبورد کارفرما — یک KPI با درصد رشد ۳۰روزه و ریزنمودار ۱۴روزه.
export interface CompanyDashboardKpiTrend {
  currentValue: number;
  growthPercent: number;
  sparkline: number[];
}

export interface CompanyDashboardViewsByJobAdSlice {
  jobAdId: string;
  jobAdTitle: string;
  viewsCount: number;
}

export interface CompanyDashboardDailyViewsPoint {
  date: string;
  viewsCount: number;
}

// طبق تسک #74 — آمار اختصاصی پنل شرکت.
export interface CompanyDashboard {
  jobAdStats: {
    totalJobAds: number;
    publishedJobAds: number;
    pendingReviewJobAds: number;
    rejectedJobAds: number;
    expiredOrClosedJobAds: number;
  };
  applicationsPerJobAd: { jobAdId: string; jobAdTitle: string; applicationCount: number }[];
  bannerAdStats: { totalBannerAds: number; activeBannerAds: number; pendingReviewBannerAds: number };
  paymentSummary: { totalPaidInRials: number; totalSuccessfulTransactions: number };
  // طبق فاز بازطراحی پیکسل‌به‌پیکسل داشبورد کارفرما — تمام مقادیر از داده واقعی بک‌اند.
  activeJobAdsKpi: CompanyDashboardKpiTrend;
  viewsKpi: CompanyDashboardKpiTrend;
  newApplicantsKpi: CompanyDashboardKpiTrend;
  viewsByJobAd: CompanyDashboardViewsByJobAdSlice[];
  viewsTrend30Days: CompanyDashboardDailyViewsPoint[];
}

export const companyApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getMyCompany: builder.query<ApiResponse<Company>, void>({
      query: () => '/companies/me',
      providesTags: ['Company']
    }),
    getMyCompanyDashboard: builder.query<ApiResponse<CompanyDashboard>, void>({
      query: () => '/companies/me/dashboard',
      providesTags: ['Company', 'JobAd', 'BannerAd']
    }),
    createCompany: builder.mutation<ApiResponse<Company>, CreateCompanyRequest>({
      query: (body) => ({ url: '/companies', method: 'POST', body }),
      invalidatesTags: ['Company']
    }),
    updateCompany: builder.mutation<ApiResponse<Company>, { id: string; body: UpdateCompanyRequest }>({
      query: ({ id, body }) => ({ url: `/companies/${id}`, method: 'PUT', body }),
      invalidatesTags: ['Company']
    }),
    requestVerification: builder.mutation<ApiResponse<Company>, string>({
      query: (id) => ({ url: `/companies/${id}/verification-request`, method: 'POST' }),
      invalidatesTags: ['Company']
    }),
    getPendingCompanies: builder.query<ApiResponse<PagedResult<Company>>, { page: number; pageSize: number }>({
      query: ({ page, pageSize }) => ({ url: '/admin/companies/pending', params: { page, pageSize } }),
      providesTags: ['Company']
    }),
    approveCompany: builder.mutation<ApiResponse<Company>, string>({
      query: (id) => ({ url: `/admin/companies/${id}/approve`, method: 'POST' }),
      invalidatesTags: ['Company']
    }),
    rejectCompany: builder.mutation<ApiResponse<Company>, string>({
      query: (id) => ({ url: `/admin/companies/${id}/reject`, method: 'POST' }),
      invalidatesTags: ['Company']
    }),

    // ---------- مدارک احراز هویت شرکت ----------
    getCompanyDocuments: builder.query<ApiResponse<CompanyDocument[]>, string>({
      query: (companyId) => `/companies/${companyId}/documents`,
      providesTags: ['CompanyDocument']
    }),
    uploadCompanyDocument: builder.mutation<ApiResponse<CompanyDocument>, { file: File; documentType: string }>({
      query: ({ file, documentType }) => {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('documentType', documentType);
        return { url: '/companies/documents', method: 'POST', body: formData };
      },
      invalidatesTags: ['CompanyDocument']
    }),
    deleteCompanyDocument: builder.mutation<ApiResponse<null>, string>({
      query: (documentId) => ({ url: `/companies/documents/${documentId}`, method: 'DELETE' }),
      invalidatesTags: ['CompanyDocument']
    }),

    // ---------- لوگو و بنر پروفایل شرکت ----------
    uploadCompanyLogo: builder.mutation<ApiResponse<Company>, File>({
      query: (file) => {
        const formData = new FormData();
        formData.append('file', file);
        return { url: '/companies/logo', method: 'POST', body: formData };
      },
      invalidatesTags: ['Company']
    }),
    uploadCompanyBanner: builder.mutation<ApiResponse<Company>, File>({
      query: (file) => {
        const formData = new FormData();
        formData.append('file', file);
        return { url: '/companies/banner', method: 'POST', body: formData };
      },
      invalidatesTags: ['Company']
    })
  })
});

export const {
  useGetMyCompanyQuery,
  useGetMyCompanyDashboardQuery,
  useCreateCompanyMutation,
  useUpdateCompanyMutation,
  useRequestVerificationMutation,
  useGetPendingCompaniesQuery,
  useApproveCompanyMutation,
  useRejectCompanyMutation,
  useGetCompanyDocumentsQuery,
  useUploadCompanyDocumentMutation,
  useDeleteCompanyDocumentMutation,
  useUploadCompanyLogoMutation,
  useUploadCompanyBannerMutation
} = companyApi;
