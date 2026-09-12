import { createApi, fetchBaseQuery, type BaseQueryFn, type FetchArgs, type FetchBaseQueryError } from '@reduxjs/toolkit/query/react';
import type { ApiResponse } from '@/shared/types';
import type { RootState } from '@/app/store';
import { sessionEstablished, loggedOut } from '@/features/auth/authSlice';

const rawBaseQuery = fetchBaseQuery({
  baseUrl: import.meta.env.VITE_API_BASE_URL ?? '/api/v1',
  prepareHeaders: (headers, { getState }) => {
    const token = (getState() as RootState).auth.accessToken;
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }
    return headers;
  }
});

/**
 * baseQuery سفارشی طبق docs/frontend/AGENT.md: پاکت ApiResponse<T> را باز می‌کند و در صورت
 * دریافت 401، یک‌بار تلاش برای رفرش توکن (سند 05-Security-Rules.md) انجام می‌دهد.
 */
const baseQueryWithReauth: BaseQueryFn<string | FetchArgs, unknown, FetchBaseQueryError> = async (
  args,
  api,
  extraOptions
) => {
  let result = await rawBaseQuery(args, api, extraOptions);

  if (result.error && result.error.status === 401) {
    const state = api.getState() as RootState;
    const refreshToken = state.auth.refreshToken;

    if (refreshToken) {
      const refreshResult = await rawBaseQuery(
        { url: '/auth/refresh-token', method: 'POST', body: { refreshToken } },
        api,
        extraOptions
      );

      const refreshBody = refreshResult.data as
        | ApiResponse<{ accessToken: string; refreshToken: string; user: RootState['auth']['user'] }>
        | undefined;

      if (refreshBody?.success && refreshBody.data) {
        // طبق ADR-010: پاسخ رفرش‌توکن اکنون شامل User (با نقش‌های به‌روز) نیز هست تا اگر نقش
        // کاربر از آخرین ورود تغییر کرده باشد (مثلاً پس از ثبت شرکت)، بدون خروج/ورود مجدد اعمال شود.
        api.dispatch(sessionEstablished({
          accessToken: refreshBody.data.accessToken,
          refreshToken: refreshBody.data.refreshToken,
          user: refreshBody.data.user ?? (api.getState() as RootState).auth.user!
        }));
        result = await rawBaseQuery(args, api, extraOptions);
      } else {
        api.dispatch(loggedOut());
      }
    } else {
      api.dispatch(loggedOut());
    }
  }

  return result;
};

export const baseApi = createApi({
  reducerPath: 'api',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['Company', 'CompanyDocument', 'JobAd', 'Application', 'Resume', 'SupportTicket', 'BannerAd', 'Bookmark', 'CompanyTransaction'],
  endpoints: () => ({})
});
