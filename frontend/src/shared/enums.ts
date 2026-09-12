// برچسب‌های فارسی Enumها — دقیقاً منطبق با docs/02-Domain-Glossary.md
// طبق docs/frontend/AGENT.md: این برچسب‌ها تنها منبع صحیح نمایش فارسی در کل فرانت‌اند هستند.

export const WorkShiftLabels: Record<string, string> = {
  MorningOnly: 'تک‌شیفت صبح',
  Rotational2Shift: 'دو شیفت چرخشی (روز/شب)',
  Rotational3Shift: 'سه شیفت ۸ ساعته چرخشی',
  NightOnly: 'فقط شب‌کاری'
};

export const MealPlanLabels: Record<string, string> = {
  None: 'بدون وعده',
  LunchOnly: 'ناهار',
  LunchAndDinner: 'ناهار و شام',
  ThreeMeals: 'صبحانه، ناهار، شام'
};

export const InsuranceTypeLabels: Record<string, string> = {
  SocialSecurity: 'بیمه تامین اجتماعی',
  Supplementary: 'بیمه تکمیلی درمان',
  AccidentInsurance: 'بیمه حوادث کارگاهی'
};

export const SalaryRangeTypeLabels: Record<string, string> = {
  MinistryOfLabor: 'طبق قانون کار',
  FixedAmount: 'مبلغ ثابت',
  Range: 'توافقی در بازه',
  Agreement: 'توافقی'
};

export const JobAdStatusLabels: Record<string, string> = {
  Draft: 'پیش‌نویس',
  PendingReview: 'در انتظار بررسی ادمین',
  Published: 'منتشرشده',
  Rejected: 'ردشده',
  Expired: 'منقضی‌شده',
  Closed: 'بسته‌شده',
  Archived: 'آرشیوشده'
};

export const VerificationStatusLabels: Record<string, string> = {
  PendingVerification: 'در انتظار بررسی',
  Verified: 'تاییدشده',
  Rejected: 'ردشده',
  Suspended: 'تعلیق‌شده'
};

export const ApplicationStatusLabels: Record<string, string> = {
  Submitted: 'ارسال‌شده',
  Reviewed: 'بررسی‌شده',
  InterviewScheduled: 'دعوت به مصاحبه',
  Accepted: 'پذیرفته‌شده',
  Rejected: 'ردشده'
};

// پیام‌های اعلان وضعیت درخواست برای نمایش به کارجو در صفحه پیگیری (طبق تصمیم محصولی).
export const ApplicationStatusMessages: Record<string, string> = {
  Submitted: 'رزومه شما دریافت شد.',
  Reviewed: 'رزومه شما در حال بررسی است.',
  InterviewScheduled: 'در انتظار تماس کارشناس باشید.',
  Accepted: 'تبریک! شما برای این موقعیت شغلی پذیرفته شدید.',
  Rejected: 'متاسفانه رزومه شما برای این موقعیت رد شد.'
};

// ---------- برچسب‌های فیلدهای تکمیلی آگهی (فاز «پایه کسب‌وکار») ----------

export const ContractTypeLabels: Record<string, string> = {
  Permanent: 'قرارداد دائم',
  FixedTerm: 'قرارداد موقت (مدت‌دار)',
  PartTime: 'پاره‌وقت',
  ProjectBased: 'پروژه‌ای',
  Internship: 'کارآموزی'
};

export const GenderPreferenceLabels: Record<string, string> = {
  Any: 'فرقی ندارد',
  MaleOnly: 'فقط آقایان',
  FemaleOnly: 'فقط خانم‌ها'
};

export const EducationLevelLabels: Record<string, string> = {
  Unspecified: 'فرقی ندارد',
  Diploma: 'دیپلم',
  AssociateDegree: 'کاردانی',
  BachelorDegree: 'کارشناسی',
  MasterDegree: 'کارشناسی ارشد',
  Doctorate: 'دکتری'
};

export const CompanyDocumentTypeLabels: Record<string, string> = {
  NationalIdCard: 'کارت ملی/شناسه ملی شرکت',
  RegistrationCertificate: 'آگهی تاسیس / روزنامه رسمی',
  IndustrialLicense: 'پروانه بهره‌برداری یا جواز تاسیس',
  Other: 'سایر مدارک'
};

export const PaymentTransactionStatusLabels: Record<string, string> = {
  Pending: 'در انتظار پرداخت',
  Success: 'موفق',
  Failed: 'ناموفق'
};

export const PaymentPurposeLabels: Record<string, string> = {
  JobAdListingFee: 'هزینه ثبت آگهی',
  BannerAdFee: 'هزینه بنر تبلیغاتی',
  ResumeCreationFee: 'هزینه رزومه‌ساز',
  BannerAdRenewal: 'تمدید بنر تبلیغاتی'
};

export const MilitaryServiceStatusLabels: Record<string, string> = {
  NotApplicable: 'فرقی ندارد',
  Completed: 'پایان خدمت',
  Exempted: 'معافیت دائم',
  InProgress: 'در حال انجام خدمت'
};

// طبق فاز «پروفایل و رزومه‌ساز کارجو» — Domain.Candidates.PreferredWorkType.
export const PreferredWorkTypeLabels: Record<string, string> = {
  OnSite: 'حضوری',
  Remote: 'دورکاری',
  Hybrid: 'ترکیبی (حضوری/دورکاری)'
};

// طبق فاز «پروفایل و رزومه‌ساز کارجو» — Domain.Candidates.LanguageProficiencyLevel، به همراه رتبه
// عددی معادل (۱ تا ۵) برای نمایش نوار/ستاره سطح تسلط در رابط کاربری.
export const LanguageProficiencyLevelLabels: Record<string, string> = {
  Basic: 'مبتدی',
  Intermediate: 'متوسط',
  UpperIntermediate: 'متوسط رو به بالا',
  Fluent: 'مسلط',
  Native: 'زبان مادری'
};

export const LanguageProficiencyLevelRank: Record<string, number> = {
  Basic: 1,
  Intermediate: 2,
  UpperIntermediate: 3,
  Fluent: 4,
  Native: 5
};
