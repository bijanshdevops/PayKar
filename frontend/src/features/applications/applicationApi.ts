import { baseApi } from '@/api/baseApi';
import type { ApiResponse, PagedResult } from '@/shared/types';
import type { EmployerApplicant } from '@/features/candidates/candidateApi';

export interface JobApplication {
  id: string;
  jobAdId: string;
  candidateId: string;
  status: string;
  trackingToken: string;
  createdAtUtc: string;
  matchScorePercent: number | null;
}

export const applicationApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    submitApplication: builder.mutation<ApiResponse<JobApplication>, string>({
      query: (jobAdId) => ({ url: `/job-ads/${jobAdId}/applications`, method: 'POST' }),
      invalidatesTags: ['Application']
    }),
    submitDirectApplication: builder.mutation<ApiResponse<JobApplication>, { jobAdId: string; fullName: string; file: File }>({
      query: ({ jobAdId, fullName, file }) => {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('fullName', fullName);
        return { url: `/job-ads/${jobAdId}/applications/direct`, method: 'POST', body: formData };
      },
      invalidatesTags: ['Application', 'Resume']
    }),
    trackApplication: builder.query<ApiResponse<JobApplication>, string>({
      query: (trackingToken) => `/applications/track/${trackingToken}`
    }),
    // طبق تصمیم صریح محصولی این فاز — چون این متقاضیان ارادی برای همین آگهی اپلای کرده‌اند، پاسخ شامل
    // هویت کامل کارجو است (EmployerApplicant)، نه فقط شناسه/وضعیت ناشناس قبلی.
    getApplicationsForJobAd: builder.query<ApiResponse<PagedResult<EmployerApplicant>>, { jobAdId: string; page: number; pageSize: number }>({
      query: ({ jobAdId, page, pageSize }) => ({ url: `/job-ads/${jobAdId}/applications`, params: { page, pageSize } }),
      providesTags: ['Application']
    }),
    // ویجت داشبورد شرکت «آخرین رزومه‌های دریافتی» — روی همهٔ آگهی‌های شرکت جاری.
    getRecentApplicantsForCompany: builder.query<ApiResponse<EmployerApplicant[]>, { count?: number } | void>({
      query: (arg) => ({ url: '/companies/me/recent-applicants', params: { count: arg?.count ?? 5 } }),
      providesTags: ['Application']
    }),
    // طبق فاز «مدیریت رزومه‌ها و متقاضیان»: علاوه بر وضعیت، یادداشت داخلی/زمان مصاحبه و کنترل
    // ارسال پیامک اطلاع‌رسانی نیز ارسال می‌شود. پاسخ اکنون EmployerApplicant کامل (نه فقط JobApplication خام) است
    // تا صفحه بلافاصله با آخرین یادداشت/زمان مصاحبه به‌روزرسانی شود.
    updateApplicationStatus: builder.mutation<
      ApiResponse<EmployerApplicant>,
      { applicationId: string; newStatus: string; companyNotes?: string | null; interviewDateTimeUtc?: string | null; notifyCandidate?: boolean }
    >({
      query: ({ applicationId, newStatus, companyNotes, interviewDateTimeUtc, notifyCandidate }) => ({
        url: `/applications/${applicationId}/status`,
        method: 'POST',
        body: { newStatus, companyNotes, interviewDateTimeUtc, notifyCandidate }
      }),
      invalidatesTags: ['Application']
    }),
    // دانلود امن فایل رزومه — به‌صورت Mutation تعریف شده تا (۱) پیام خطا از طریق toastMiddleware
    // سراسری نمایش داده شود و (۲) با کلیک کاربر تریگر شود، نه با mount شدن کامپوننت.
    // چون احراز هویت پروژه Bearer-Token است (نه کوکی)، یک لینک <a> ساده هدر Authorization را
    // ارسال نمی‌کند؛ بنابراین فایل از طریق fetchBaseQuery (که این هدر را خودکار اضافه می‌کند) به‌صورت
    // Blob دریافت شده و سپس با یک لینک موقت در مرورگر دانلود می‌شود.
    downloadApplicationResume: builder.mutation<{ blob: Blob; fileName: string }, string>({
      query: (applicationId) => ({
        url: `/job-applications/${applicationId}/resume`,
        responseHandler: async (response: Response) => {
          if (!response.ok) {
            return response.json();
          }
          const blob = await response.blob();
          const disposition = response.headers.get('content-disposition') ?? '';
          const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
          const fileName = match ? decodeURIComponent(match[1]) : 'resume';
          return { blob, fileName };
        }
      })
    })
  })
});

export const {
  useSubmitApplicationMutation,
  useSubmitDirectApplicationMutation,
  useTrackApplicationQuery,
  useLazyTrackApplicationQuery,
  useGetApplicationsForJobAdQuery,
  useGetRecentApplicantsForCompanyQuery,
  useUpdateApplicationStatusMutation,
  useDownloadApplicationResumeMutation
} = applicationApi;
