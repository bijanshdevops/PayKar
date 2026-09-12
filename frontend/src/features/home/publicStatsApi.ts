import { baseApi } from '@/api/baseApi';
import type { ApiResponse } from '@/shared/types';

// آمار عمومی پلتفرم برای نوار اعتمادسازی صفحه اصلی — بدون نیاز به احراز هویت.
export interface PublicStats {
  totalVerifiedCompanies: number;
  totalIndustrialZones: number;
  totalCandidates: number;
  totalActiveJobAds: number;
}

export const publicStatsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getPublicStats: builder.query<ApiResponse<PublicStats>, void>({
      query: () => '/public/stats'
    })
  })
});

export const { useGetPublicStatsQuery } = publicStatsApi;
