import { baseApi } from '@/api/baseApi';
import type { ApiResponse, PagedResult } from '@/shared/types';

export type BannerPlacement = 'Home' | 'JobAdList' | 'Both';
export type BannerAdStatus = 'Draft' | 'PendingReview' | 'Active' | 'Rejected' | 'Expired';

/** جایگاه تبلیغاتی از کاتالوگ ثابت — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۳). */
export interface BannerSlot {
  id: number;
  title: string;
  placement: BannerPlacement;
  dimensions: string;
  dailyPrice: number;
  isActive: boolean;
}

export interface BannerAd {
  id: string;
  companyId: string;
  imageUrl: string;
  destinationUrl: string;
  placement: BannerPlacement;
  status: BannerAdStatus;
  isFeePaid: boolean;
  rejectionReason: string | null;
  activatedAtUtc: string | null;
  expiresAtUtc: string | null;
  createdAtUtc: string;
  bannerSlotId: number | null;
  slotTitle: string | null;
  slotDimensions: string | null;
  durationDays: number;
  startDate: string | null;
  endDate: string | null;
  totalAmount: number;
  impressionsCount: number;
  clicksCount: number;
}

export interface CreateBannerAdRequest {
  imageUrl: string;
  destinationUrl: string;
  placement: BannerPlacement;
  bannerSlotId: number;
  durationDays?: number;
}

export interface SubmitBannerAdForReviewResponse {
  paymentRedirectUrl: string;
  amountInRials: number;
}

export const bannerAdApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    /** کاتالوگ جایگاه‌های فعال — طبق تسک ۳، برای نمایش کارت‌های انتخاب جایگاه در فرم رزرو. */
    getBannerSlots: builder.query<ApiResponse<BannerSlot[]>, void>({
      query: () => '/banner-ads/slots'
    }),
    uploadBannerImage: builder.mutation<ApiResponse<string>, File>({
      query: (file) => {
        const formData = new FormData();
        formData.append('file', file);
        return { url: '/banner-ads/images', method: 'POST', body: formData };
      }
    }),
    createBannerAd: builder.mutation<ApiResponse<BannerAd>, CreateBannerAdRequest>({
      query: (body) => ({ url: '/banner-ads', method: 'POST', body }),
      invalidatesTags: ['BannerAd']
    }),
    updateBannerAd: builder.mutation<ApiResponse<BannerAd>, { id: string; imageUrl: string; destinationUrl: string; placement: BannerPlacement }>({
      query: ({ id, ...body }) => ({ url: `/banner-ads/${id}`, method: 'PUT', body }),
      invalidatesTags: ['BannerAd']
    }),
    getMyCompanyBannerAds: builder.query<ApiResponse<PagedResult<BannerAd>>, { page: number; pageSize: number }>({
      query: ({ page, pageSize }) => ({ url: '/banner-ads/my-company', params: { page, pageSize } }),
      providesTags: ['BannerAd']
    }),
    submitBannerAdForReview: builder.mutation<ApiResponse<SubmitBannerAdForReviewResponse>, string>({
      query: (bannerAdId) => ({ url: `/payments/banner-ads/${bannerAdId}/submit-for-review`, method: 'POST' }),
      invalidatesTags: ['BannerAd']
    }),
    /** تمدید بنر با ۲۰٪ تخفیف از طریق کیف‌پول — طبق تسک ۵. */
    renewBannerAd: builder.mutation<ApiResponse<BannerAd>, { id: string; durationDays?: number }>({
      query: ({ id, durationDays }) => ({
        url: `/banner-ads/${id}/renew`,
        method: 'POST',
        body: durationDays ? { durationDays } : undefined
      }),
      invalidatesTags: ['BannerAd']
    }),
    getActiveBannerAds: builder.query<ApiResponse<BannerAd[]>, BannerPlacement>({
      query: (placement) => ({ url: '/banner-ads/active', params: { placement } })
    }),
    getPendingBannerAds: builder.query<ApiResponse<PagedResult<BannerAd>>, { page: number; pageSize: number }>({
      query: ({ page, pageSize }) => ({ url: '/admin/banner-ads/pending', params: { page, pageSize } }),
      providesTags: ['BannerAd']
    }),
    approveBannerAd: builder.mutation<ApiResponse<BannerAd>, string>({
      query: (id) => ({ url: `/admin/banner-ads/${id}/approve`, method: 'POST' }),
      invalidatesTags: ['BannerAd']
    }),
    rejectBannerAd: builder.mutation<ApiResponse<BannerAd>, { id: string; reason: string }>({
      query: ({ id, reason }) => ({ url: `/admin/banner-ads/${id}/reject`, method: 'POST', body: { reason } }),
      invalidatesTags: ['BannerAd']
    })
  })
});

export const {
  useGetBannerSlotsQuery,
  useUploadBannerImageMutation,
  useCreateBannerAdMutation,
  useUpdateBannerAdMutation,
  useGetMyCompanyBannerAdsQuery,
  useSubmitBannerAdForReviewMutation,
  useRenewBannerAdMutation,
  useGetActiveBannerAdsQuery,
  useGetPendingBannerAdsQuery,
  useApproveBannerAdMutation,
  useRejectBannerAdMutation
} = bannerAdApi;
