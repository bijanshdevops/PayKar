import { baseApi } from '@/api/baseApi';
import type { ApiResponse } from '@/shared/types';

export interface Province { id: string; name: string; }
export interface City { id: string; name: string; provinceId: string; }
export interface IndustrialZone { id: string; name: string; cityId: string; zoneType: string; }

export const geographyApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getProvinces: builder.query<ApiResponse<Province[]>, void>({
      query: () => '/geography/provinces'
    }),
    getCities: builder.query<ApiResponse<City[]>, string | undefined>({
      query: (provinceId) => ({ url: '/geography/cities', params: provinceId ? { provinceId } : undefined })
    }),
    getIndustrialZones: builder.query<ApiResponse<IndustrialZone[]>, string | undefined>({
      query: (cityId) => ({ url: '/geography/industrial-zones', params: cityId ? { cityId } : undefined })
    })
  })
});

export const { useGetProvincesQuery, useGetCitiesQuery, useGetIndustrialZonesQuery } = geographyApi;
