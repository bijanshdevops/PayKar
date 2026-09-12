import { baseApi } from '@/api/baseApi';
import type { ApiResponse } from '@/shared/types';

export interface PlatformOverview {
  totalCompanies: number;
  verifiedCompanies: number;
  totalJobAds: number;
  publishedJobAds: number;
  pendingReviewJobAds: number;
  totalCandidates: number;
  totalApplications: number;
  activeBannerAds: number;
  openSupportTickets: number;
}

export interface RevenueSummary {
  totalRevenueInRials: number;
  jobAdFeeRevenueInRials: number;
  bannerAdFeeRevenueInRials: number;
  totalSuccessfulTransactions: number;
}

export interface DailyRevenuePoint {
  date: string;
  amountInRials: number;
}

export interface OwnerDashboard {
  overview: PlatformOverview;
  revenue: RevenueSummary;
  revenueTimeSeries: DailyRevenuePoint[];
}

export const ownerDashboardApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getOwnerDashboard: builder.query<ApiResponse<OwnerDashboard>, { timeSeriesDays?: number } | void>({
      query: (args) => ({ url: '/admin/dashboard', params: { timeSeriesDays: args?.timeSeriesDays ?? 30 } })
    })
  })
});

export const { useGetOwnerDashboardQuery } = ownerDashboardApi;
