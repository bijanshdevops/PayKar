import { baseApi } from '@/api/baseApi';
import type { ApiResponse } from '@/shared/types';
import type { JobAd } from '@/features/jobAds/jobAdApi';

// طبق فاز «مدیریت رزومه‌ها و متقاضیان» — یک ردیف سابقه تحصیلی ساختاریافته.
export interface CandidateEducation {
  id: string;
  degreeLevel: string;
  fieldOfStudy: string;
  institutionName: string;
  graduationYear: number | null;
}

// طبق فاز «مدیریت رزومه‌ها و متقاضیان» — یک ردیف سابقه شغلی ساختاریافته (endYear=null یعنی «اکنون»).
export interface CandidateWorkExperience {
  id: string;
  jobTitle: string;
  companyName: string;
  startYear: number;
  endYear: number | null;
  description: string | null;
}

// طبق فاز «پروفایل و رزومه‌ساز کارجو» — یک ردیف مدرک/گواهینامه.
export interface CandidateCertification {
  id: string;
  title: string;
  issuingOrganization: string;
  yearObtained: number;
}

// طبق فاز «پروفایل و رزومه‌ساز کارجو» — یک ردیف مهارت ساختاریافته (مکمل فیلد آزاد Candidate.skills).
export interface CandidateSkillEntry {
  id: string;
  name: string;
  category: string;
  level: number;
}

// طبق فاز «پروفایل و رزومه‌ساز کارجو» — یک ردیف زبان خارجی + سطح تسلط.
export interface CandidateLanguageEntry {
  id: string;
  name: string;
  proficiencyLevel: string;
}

export interface Candidate {
  id: string;
  fullName: string;
  militaryServiceStatus: string;
  educationLevel: string;
  workExperienceSummary: string | null;
  skills: string | null;
  interests: string | null;
  psychologyAnswers: string | null;
  resumeFileUrl: string | null;
  // طبق ADR-013 — وضعیت پرداخت هزینه رزومه‌ساز.
  isFeePaid: boolean;
  resumeCreationFeeAmountInRials: number;
  avatarUrl: string | null;
  // طبق فاز «مدیریت رزومه‌ها و متقاضیان» — فیلدهای اختیاری تماس + سوابق ساختاریافته.
  email: string | null;
  city: string | null;
  educations: CandidateEducation[];
  workExperiences: CandidateWorkExperience[];
  // ---------- طبق فاز «پروفایل و رزومه‌ساز کارجو» ----------
  jobTitle: string | null;
  professionalSummary: string | null;
  linkedInUrl: string | null;
  gitHubUrl: string | null;
  personalWebsiteUrl: string | null;
  // مقادیر ممکن: OnSite | Remote | Hybrid (ر.ک. Domain.Candidates.PreferredWorkType) — یا null اگر هنوز تعیین نشده.
  preferredWorkType: string | null;
  minRequestedSalaryInToman: number | null;
  maxRequestedSalaryInToman: number | null;
  isActivelyLookingForJob: boolean;
  certifications: CandidateCertification[];
  structuredSkills: CandidateSkillEntry[];
  languages: CandidateLanguageEntry[];
}

// طبق فاز «پروفایل و رزومه‌ساز کارجو» — خروجی کامل GetMyCandidateProfileQuery: پروفایل +
// درصد تکمیل رزومه (قطعی/غیرهوش‌مصنوعی، ر.ک. CalculateResumeScoreService بک‌اند) + پیشنهادهای تکمیل.
export interface CandidateProfileDto {
  profile: Candidate;
  resumeCompletionPercent: number;
  suggestions: string[];
}

// طبق ADR-013 — پاسخ شروع پرداخت هزینه رزومه‌ساز.
export interface SubmitResumeFeePaymentResponse {
  paymentRedirectUrl: string;
  amountInRials: number;
}

export interface CandidateFormValues {
  fullName: string;
  militaryServiceStatus: string;
  educationLevel: string;
  workExperienceSummary?: string;
  skills?: string;
  interests?: string;
  psychologyAnswers?: string;
  email?: string;
  city?: string;
  // ---------- طبق فاز «پروفایل و رزومه‌ساز کارجو» — منطبق با فیلدهای جدید SaveCandidateProfileCommand ----------
  jobTitle?: string;
  professionalSummary?: string;
  linkedInUrl?: string;
  gitHubUrl?: string;
  personalWebsiteUrl?: string;
}

// طبق فاز «مدیریت رزومه‌ها و متقاضیان» — ورودی فرم افزودن یک ردیف سابقه تحصیلی.
export interface AddCandidateEducationValues {
  degreeLevel: string;
  fieldOfStudy: string;
  institutionName: string;
  graduationYear?: number;
}

// طبق فاز «مدیریت رزومه‌ها و متقاضیان» — ورودی فرم افزودن یک ردیف سابقه شغلی.
export interface AddCandidateWorkExperienceValues {
  jobTitle: string;
  companyName: string;
  startYear: number;
  endYear?: number;
  description?: string;
}

// طبق فاز «پروفایل و رزومه‌ساز کارجو» — ورودی افزودن/ویرایش سابقه شغلی (UpsertWorkExperienceCommand).
// id=undefined یعنی افزودن ردیف جدید (POST)؛ id مشخص یعنی ویرایش درجای همان ردیف (PUT).
export interface UpsertCandidateWorkExperienceValues {
  id?: string;
  jobTitle: string;
  companyName: string;
  startYear: number;
  endYear?: number;
  description?: string;
}

// طبق فاز «پروفایل و رزومه‌ساز کارجو» — ورودی افزودن مدرک/گواهینامه.
export interface AddCandidateCertificationValues {
  title: string;
  issuingOrganization: string;
  yearObtained: number;
}

// طبق فاز «پروفایل و رزومه‌ساز کارجو» — ورودی همگام‌سازی کامل مهارت‌های ساختاریافته (جایگزینی کل فهرست).
export interface UpdateCandidateSkillsValues {
  skills: { name: string; category: string; level: number }[];
}

// طبق فاز «پروفایل و رزومه‌ساز کارجو» — ورودی همگام‌سازی کامل زبان‌ها (جایگزینی کل فهرست).
export interface UpdateCandidateLanguagesValues {
  languages: { name: string; proficiencyLevel: string }[];
}

// طبق فاز «پروفایل و رزومه‌ساز کارجو» — ورودی ترجیحات شغلی (UpdateCandidateJobPreferencesCommand).
export interface UpdateCandidateJobPreferencesValues {
  preferredWorkType: string | null;
  minRequestedSalaryInToman: number | null;
  maxRequestedSalaryInToman: number | null;
  isActivelyLookingForJob: boolean;
}

// طبق تسک #75 — ردیف نمایش در «پیگیری وضعیت درخواست‌های من».
export interface MyApplication {
  id: string;
  jobAdId: string;
  jobAdTitle: string;
  status: string;
  createdAtUtc: string;
}

/**
 * درخواست همکاری از منظر کارفرمای صاحب آگهی — طبق تصمیم صریح محصولی: چون کارجو با ارسال درخواست
 * برای همین آگهی ارادی Apply کرده، هویت کامل او (نام/آواتار/تحصیلات/سوابق/مهارت/رزومه) در اختیار
 * همان کارفرما قرار می‌گیرد. فقط از GetApplicationsForJobAdQuery/GetRecentApplicantsForCompanyQuery
 * برمی‌گردد که هر دو به صاحب همان آگهی محدودند.
 */
export interface EmployerApplicant {
  applicationId: string;
  jobAdId: string;
  jobAdTitle: string;
  candidateId: string;
  candidateFullName: string;
  candidateAvatarUrl: string | null;
  // طبق تصمیم صریح محصولی فاز «مدیریت رزومه‌ها و متقاضیان» — چون کارجو ارادی برای همین آگهی
  // Apply کرده، شماره تماس واقعی او نیز در اختیار کارفرما قرار می‌گیرد.
  candidateMobileNumber: string;
  // ایمیل و شهر — فیلدهای واقعی و اختیاری روی Candidate؛ ممکن است null باشند.
  candidateEmail: string | null;
  candidateCity: string | null;
  candidateEducationLevel: string;
  candidateWorkExperienceSummary: string | null;
  // سوابق ساختاریافته — مکمل دو فیلد آزاد بالا، ممکن است خالی باشند.
  candidateEducations: CandidateEducation[];
  candidateWorkExperiences: CandidateWorkExperience[];
  candidateSkills: string | null;
  candidateResumeFileUrl: string | null;
  matchScorePercent: number | null;
  status: string;
  trackingToken: string;
  createdAtUtc: string;
  // طبق فاز «مدیریت رزومه‌ها و متقاضیان» — یادداشت داخلی کارفرما و زمان مصاحبهٔ زمان‌بندی‌شده (هرگز به کارجو نمایش داده نمی‌شود).
  companyNotes: string | null;
  interviewDateTimeUtc: string | null;
}

export const candidateApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getMyResume: builder.query<ApiResponse<Candidate>, void>({
      query: () => '/candidates/resumes/me',
      // بک‌اند اکنون از طریق GetMyCandidateProfileQuery خروجی CandidateProfileDto (پروفایل + درصد
      // تکمیل + پیشنهادها) برمی‌گرداند؛ برای سازگاری با مصرف‌کنندگان قبلی این هوک (مثل ResumeFormPage)
      // که فقط به خود Candidate نیاز دارند، اینجا فقط بخش profile استخراج می‌شود. برای دسترسی به
      // درصد تکمیل/پیشنهادها از useGetMyCandidateProfileQuery زیر استفاده کنید.
      transformResponse: (response: ApiResponse<CandidateProfileDto>): ApiResponse<Candidate> => ({
        ...response,
        data: response.data?.profile ?? null
      }),
      providesTags: ['Resume']
    }),
    // طبق فاز «پروفایل و رزومه‌ساز کارجو» — نمای کامل رزومه (CandidateProfileView.tsx): همان
    // اندپوینت GET /candidates/resumes/me، بدون Transform، شامل درصد تکمیل و پیشنهادهای هوشمند.
    getMyCandidateProfile: builder.query<ApiResponse<CandidateProfileDto>, void>({
      query: () => '/candidates/resumes/me',
      providesTags: ['Resume']
    }),
    saveResume: builder.mutation<ApiResponse<Candidate>, CandidateFormValues>({
      query: (body) => ({ url: '/candidates/resumes', method: 'POST', body }),
      invalidatesTags: ['Resume']
    }),
    getMyApplications: builder.query<ApiResponse<MyApplication[]>, void>({
      query: () => '/candidates/me/applications',
      providesTags: ['Application']
    }),
    // طبق ADR-013 — شروع پرداخت هزینه رزومه‌ساز.
    submitResumeFeePayment: builder.mutation<ApiResponse<SubmitResumeFeePaymentResponse>, void>({
      query: () => ({ url: '/candidates/resumes/submit-payment', method: 'POST' })
    }),
    // آپلود/جایگزینی آواتار — طبق تصمیم صریح محصولی این فاز، هنگام Apply در اختیار کارفرمای همان آگهی قرار می‌گیرد.
    uploadAvatar: builder.mutation<ApiResponse<Candidate>, File>({
      query: (file) => {
        const formData = new FormData();
        formData.append('file', file);
        return { url: '/candidates/me/avatar', method: 'POST', body: formData };
      },
      invalidatesTags: ['Resume']
    }),
    // آیکون بوک‌مارک روی کارت‌ها — فقط شناسهٔ آگهی‌های نشان‌شده، برای ساخت Set و تعیین وضعیت هر کارت.
    getMyBookmarkIds: builder.query<ApiResponse<string[]>, void>({
      query: () => '/candidates/me/bookmarks/ids',
      providesTags: ['Bookmark']
    }),
    // صفحهٔ «آگهی‌های نشان‌شده» در داشبورد کارجو — جزئیات کامل هر آگهی.
    getMyBookmarks: builder.query<ApiResponse<JobAd[]>, void>({
      query: () => '/candidates/me/bookmarks',
      providesTags: ['Bookmark']
    }),
    // طبق فاز «مدیریت رزومه‌ها و متقاضیان» — افزودن/حذف ردیف‌های ساختاریافته تحصیلات و سوابق شغلی.
    addCandidateEducation: builder.mutation<ApiResponse<Candidate>, AddCandidateEducationValues>({
      query: (body) => ({ url: '/candidates/me/educations', method: 'POST', body }),
      invalidatesTags: ['Resume']
    }),
    deleteCandidateEducation: builder.mutation<ApiResponse<null>, string>({
      query: (educationId) => ({ url: `/candidates/me/educations/${educationId}`, method: 'DELETE' }),
      invalidatesTags: ['Resume']
    }),
    addCandidateWorkExperience: builder.mutation<ApiResponse<Candidate>, AddCandidateWorkExperienceValues>({
      query: (body) => ({ url: '/candidates/me/work-experiences', method: 'POST', body }),
      invalidatesTags: ['Resume']
    }),
    deleteCandidateWorkExperience: builder.mutation<ApiResponse<null>, string>({
      query: (workExperienceId) => ({ url: `/candidates/me/work-experiences/${workExperienceId}`, method: 'DELETE' }),
      invalidatesTags: ['Resume']
    }),
    // ---------- طبق فاز «پروفایل و رزومه‌ساز کارجو» (CandidateProfileEdit.tsx) ----------
    // افزودن/ویرایش اتمیک سابقه شغلی — مکمل addCandidateWorkExperience بالا (فقط افزودن)؛ این یکی
    // با ارسال id امکان ویرایش درجا را هم می‌دهد (UpsertWorkExperienceCommand در بک‌اند).
    upsertCandidateWorkExperience: builder.mutation<ApiResponse<Candidate>, UpsertCandidateWorkExperienceValues>({
      query: ({ id, ...body }) =>
        id
          ? { url: `/candidates/me/experiences/${id}`, method: 'PUT', body }
          : { url: '/candidates/me/experiences', method: 'POST', body },
      invalidatesTags: ['Resume']
    }),
    addCandidateCertification: builder.mutation<ApiResponse<Candidate>, AddCandidateCertificationValues>({
      query: (body) => ({ url: '/candidates/resumes/me/certifications', method: 'POST', body }),
      invalidatesTags: ['Resume']
    }),
    deleteCandidateCertification: builder.mutation<ApiResponse<null>, string>({
      query: (certificationId) => ({ url: `/candidates/resumes/me/certifications/${certificationId}`, method: 'DELETE' }),
      invalidatesTags: ['Resume']
    }),
    // جایگزینی کامل مجموعه مهارت‌های ساختاریافته — طبق UpdateCandidateSkillsCommand (Full-Replace).
    updateCandidateSkills: builder.mutation<ApiResponse<Candidate>, UpdateCandidateSkillsValues>({
      query: (body) => ({ url: '/candidates/resumes/me/skills', method: 'PUT', body }),
      invalidatesTags: ['Resume']
    }),
    // همگام‌سازی Diff-Based زبان‌ها — طبق UpdateCandidateLanguagesCommand.
    updateCandidateLanguages: builder.mutation<ApiResponse<Candidate>, UpdateCandidateLanguagesValues>({
      query: (body) => ({ url: '/candidates/resumes/me/languages', method: 'PUT', body }),
      invalidatesTags: ['Resume']
    }),
    updateCandidateJobPreferences: builder.mutation<ApiResponse<Candidate>, UpdateCandidateJobPreferencesValues>({
      query: (body) => ({ url: '/candidates/me/preferences', method: 'PUT', body }),
      invalidatesTags: ['Resume']
    })
  })
});

export const {
  useGetMyResumeQuery,
  useGetMyCandidateProfileQuery,
  useSaveResumeMutation,
  useGetMyApplicationsQuery,
  useSubmitResumeFeePaymentMutation,
  useUploadAvatarMutation,
  useGetMyBookmarkIdsQuery,
  useGetMyBookmarksQuery,
  useAddCandidateEducationMutation,
  useDeleteCandidateEducationMutation,
  useAddCandidateWorkExperienceMutation,
  useDeleteCandidateWorkExperienceMutation,
  useUpsertCandidateWorkExperienceMutation,
  useAddCandidateCertificationMutation,
  useDeleteCandidateCertificationMutation,
  useUpdateCandidateSkillsMutation,
  useUpdateCandidateLanguagesMutation,
  useUpdateCandidateJobPreferencesMutation
} = candidateApi;
