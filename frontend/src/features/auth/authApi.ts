import { baseApi } from '@/api/baseApi';
import type { ApiResponse, AuthenticatedUser } from '@/shared/types';

interface RequestOtpResponse {
  expiresInSeconds: number;
}

interface VerifyOtpResponse {
  accessToken: string;
  refreshToken: string;
  expiresInSeconds: number;
  user: AuthenticatedUser;
}

export type RegistrationAccountType = 'Candidate' | 'Company';

export const authApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    requestOtp: builder.mutation<ApiResponse<RequestOtpResponse>, { mobileNumber: string }>({
      query: (body) => ({ url: '/auth/otp/request', method: 'POST', body })
    }),
    verifyOtp: builder.mutation<ApiResponse<VerifyOtpResponse>, { mobileNumber: string; otpCode: string }>({
      query: (body) => ({ url: '/auth/otp/verify', method: 'POST', body })
    }),
    // ورود با نام‌کاربری/رمزعبور — روش دوم و اختیاری در کنار OTP (ADR-005).
    loginWithPassword: builder.mutation<ApiResponse<VerifyOtpResponse>, { username: string; password: string }>({
      query: (body) => ({ url: '/auth/login', method: 'POST', body })
    }),
    setPassword: builder.mutation<ApiResponse<{ username: string }>, { username: string; password: string }>({
      query: (body) => ({ url: '/auth/set-password', method: 'POST', body })
    }),
    // ثبت‌نام کامل (نام‌کاربری+ایمیل+موبایل+رمزعبور) — طبق ADR-006؛ در صورت موفقیت، کد OTP
    // برای تایید موبایل ارسال می‌شود و تکمیل با همان اندپوینت verifyOtp انجام می‌گیرد.
    register: builder.mutation<
      ApiResponse<RequestOtpResponse>,
      { username: string; email: string; mobileNumber: string; password: string; accountType: RegistrationAccountType }
    >({
      query: (body) => ({ url: '/auth/register', method: 'POST', body })
    }),
    // طبق ADR-010: پس از ثبت موفق شرکت، فراخوانی دستی برای دریافت توکن/نقش‌های به‌روز
    // (شامل CompanyManager تازه‌اعطاشده) بدون نیاز به خروج/ورود مجدد کاربر.
    refreshSession: builder.mutation<ApiResponse<VerifyOtpResponse>, { refreshToken: string }>({
      query: (body) => ({ url: '/auth/refresh-token', method: 'POST', body })
    }),
    // بازیابی رمز عبور فراموش‌شده (مرحله دوم) — مرحله اول از همان requestOtp استفاده می‌کند.
    resetPassword: builder.mutation<
      ApiResponse<null>,
      { mobileNumber: string; otpCode: string; newPassword: string }
    >({
      query: (body) => ({ url: '/auth/password/reset', method: 'POST', body })
    }),
    logout: builder.mutation<ApiResponse<null>, { refreshToken: string }>({
      query: (body) => ({ url: '/auth/logout', method: 'POST', body })
    })
  })
});

export const {
  useRequestOtpMutation,
  useVerifyOtpMutation,
  useLoginWithPasswordMutation,
  useSetPasswordMutation,
  useRegisterMutation,
  useResetPasswordMutation,
  useLogoutMutation,
  useRefreshSessionMutation
} = authApi;
