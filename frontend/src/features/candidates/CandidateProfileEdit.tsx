import { useEffect, useState, type FormEvent, type InputHTMLAttributes, type SelectHTMLAttributes, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import {
  Award,
  Briefcase,
  Building2,
  Camera,
  Circle,
  Eye,
  GraduationCap,
  Laptop,
  Languages as LanguagesIcon,
  Loader2,
  Pencil,
  Phone,
  Plus,
  Save,
  Shuffle,
  Star,
  Trash2,
  User,
  Wallet,
  X
} from 'lucide-react';
import { useAppSelector } from '@/app/hooks';
import {
  useGetMyCandidateProfileQuery,
  useSaveResumeMutation,
  useUploadAvatarMutation,
  useAddCandidateEducationMutation,
  useDeleteCandidateEducationMutation,
  useUpsertCandidateWorkExperienceMutation,
  useDeleteCandidateWorkExperienceMutation,
  useAddCandidateCertificationMutation,
  useDeleteCandidateCertificationMutation,
  useUpdateCandidateSkillsMutation,
  useUpdateCandidateLanguagesMutation,
  useUpdateCandidateJobPreferencesMutation,
  type CandidateSkillEntry,
  type CandidateLanguageEntry
} from '@/features/candidates/candidateApi';
import {
  PreferredWorkTypeLabels,
  LanguageProficiencyLevelLabels,
  LanguageProficiencyLevelRank,
  MilitaryServiceStatusLabels
} from '@/shared/enums';
import ResumeFeePaymentCard from '@/features/candidates/ResumeFeePaymentCard';

// طبق فاز «پروفایل و رزومه‌ساز کارجو» (Phase 4) — نمای ویرایش رزومه، مطابق karjoo-edit-profile.png.
//
// شفاف‌سازی چند تصمیم آگاهانه نسبت به موکاپ (طبق اصل «بدون داده جعلی» و امکانات واقعی بک‌اند):
// ۱. دکمه «AI Rewrite» و نوار ابزار ویرایشگر متن غنی (Bold/Italic/Link) حذف شده‌اند — هیچ سرویس
//    هوش‌مصنوعی یا ذخیره‌سازی HTML غنی در بک‌اند این پروژه وجود ندارد؛ فیلد Description یک رشته متنی
//    ساده است، بنابراین از یک textarea معمولی استفاده شده.
// ۲. اسلایدر دو-دستگیره برای محدوده حقوق با دو ورودی عددی (حداقل/حداکثر) جایگزین شده — از نظر عملکرد
//    معادل است، بدون نیاز به کتابخانه یا کامپوننت سفارشی ریسکی.
// ۳. انتخاب‌گر تاریخ تقویمی برای بازه زمانی سوابق شغلی با ورودی «سال» جایگزین شده، چون فیلدهای واقعی
//    StartYear/EndYear در دامنه فقط سال (نه ماه/روز) را نگه می‌دارند.
// ۴. شماره تماس فقط نمایشی و غیرقابل‌ویرایش است (از پروفایل احراز هویت خوانده می‌شود) — تغییر آن از
//    مسیر ورود/OTP انجام می‌شود، نه از این فرم.
// ۵. شماره تماس/ایمیل «تایید هویت» جعلی ندارد؛ فقط وضعیت واقعی IsFeePaid/IsActivelyLookingForJob
//    در صفحه نمای رزومه (CandidateProfileView) نشان داده می‌شود، نه اینجا.

function formatToman(n: number) {
  return n.toLocaleString('fa-IR');
}

function toPersianDigits(n: number) {
  return n.toLocaleString('fa-IR');
}

function EditCard({ title, icon, action, children, className = '' }: { title: string; icon: ReactNode; action?: ReactNode; children: ReactNode; className?: string }) {
  return (
    <section className={`rounded-2xl border border-slate-200/80 bg-white p-5 shadow-sm ${className}`}>
      <div className="mb-4 flex items-center justify-between gap-2">
        <h2 className="m-0 flex items-center gap-2 text-base font-bold text-slate-800">
          <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-emerald-50 text-emerald-600">{icon}</span>
          {title}
        </h2>
        {action}
      </div>
      {children}
    </section>
  );
}

function TextField({ label, ...props }: { label: string } & InputHTMLAttributes<HTMLInputElement>) {
  return (
    <label className="block text-xs font-semibold text-slate-600">
      {label}
      <input
        {...props}
        className="mt-1 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-800 focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
      />
    </label>
  );
}

// طبق تصمیم صریح محصولی «قطع کامل وابستگی به رزومه‌ساز مرحله‌ای»: وضعیت نظام‌وظیفه اکنون مستقیماً
// در همین صفحه ویرایش می‌شود (پیش‌تر فقط در ResumeFormPage.tsx امکان‌پذیر بود).
function SelectField({ label, options, ...props }: { label: string; options: Record<string, string> } & SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <label className="block text-xs font-semibold text-slate-600">
      {label}
      <select
        {...props}
        className="mt-1 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-800 focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
      >
        {Object.entries(options).map(([value, optionLabel]) => (
          <option key={value} value={value}>{optionLabel}</option>
        ))}
      </select>
    </label>
  );
}

const WORK_TYPE_OPTIONS: { value: string; icon: ReactNode }[] = [
  { value: 'OnSite', icon: <Building2 size={14} /> },
  { value: 'Remote', icon: <Laptop size={14} /> },
  { value: 'Hybrid', icon: <Shuffle size={14} /> }
];

const PROFICIENCY_ORDER = ['Basic', 'Intermediate', 'UpperIntermediate', 'Fluent', 'Native'];

function RankPicker({ value, onChange, count = 5, icon: Icon = Star }: { value: number; onChange: (rank: number) => void; count?: number; icon?: typeof Star }) {
  return (
    <div className="flex items-center gap-0.5">
      {Array.from({ length: count }, (_, i) => i + 1).map((rank) => (
        <button key={rank} type="button" onClick={() => onChange(rank)} className="bg-transparent p-0.5" aria-label={`سطح ${rank}`}>
          <Icon size={16} className={rank <= value ? 'fill-emerald-500 text-emerald-500' : 'text-slate-300'} />
        </button>
      ))}
    </div>
  );
}

export default function CandidateProfileEdit() {
  const authUser = useAppSelector((state) => state.auth.user);
  const { data, isLoading: isLoadingProfile } = useGetMyCandidateProfileQuery();
  const resume = data?.data?.profile;
  const completionPercent = data?.data?.resumeCompletionPercent ?? 0;

  const [saveResume, { isLoading: isSavingProfile }] = useSaveResumeMutation();
  const [uploadAvatar, { isLoading: isUploadingAvatar }] = useUploadAvatarMutation();
  const [addEducation] = useAddCandidateEducationMutation();
  const [deleteEducation] = useDeleteCandidateEducationMutation();
  const [upsertWorkExperience, { isLoading: isSavingExperience }] = useUpsertCandidateWorkExperienceMutation();
  const [deleteWorkExperience] = useDeleteCandidateWorkExperienceMutation();
  const [addCertification] = useAddCandidateCertificationMutation();
  const [deleteCertification] = useDeleteCandidateCertificationMutation();
  const [updateSkills, { isLoading: isSavingSkills }] = useUpdateCandidateSkillsMutation();
  const [updateLanguages, { isLoading: isSavingLanguages }] = useUpdateCandidateLanguagesMutation();
  const [updatePreferences, { isLoading: isSavingPreferences }] = useUpdateCandidateJobPreferencesMutation();

  // ---------- مشخصات فردی ----------
  // طبق تصمیم صریح محصولی «قطع کامل وابستگی به رزومه‌ساز مرحله‌ای»: militaryServiceStatus و
  // educationLevel اکنون فیلدهای واقعی این فرم هستند (نه صرفاً عبور بدون تغییر از resume موجود)،
  // چون این صفحه باید بتواند اولین رزومهٔ یک کارجوی تازه (بدون resume قبلی) را هم بسازد.
  const [personalInfo, setPersonalInfo] = useState({
    fullName: '', jobTitle: '', professionalSummary: '', email: '', city: '',
    linkedInUrl: '', gitHubUrl: '', personalWebsiteUrl: '',
    militaryServiceStatus: 'NotApplicable', educationLevel: ''
  });
  useEffect(() => {
    if (!resume) return;
    setPersonalInfo({
      fullName: resume.fullName ?? '',
      jobTitle: resume.jobTitle ?? '',
      professionalSummary: resume.professionalSummary ?? '',
      email: resume.email ?? '',
      city: resume.city ?? '',
      linkedInUrl: resume.linkedInUrl ?? '',
      gitHubUrl: resume.gitHubUrl ?? '',
      personalWebsiteUrl: resume.personalWebsiteUrl ?? '',
      militaryServiceStatus: resume.militaryServiceStatus ?? 'NotApplicable',
      educationLevel: resume.educationLevel ?? ''
    });
  }, [resume]);
  const [personalInfoError, setPersonalInfoError] = useState<string | null>(null);
  const [personalInfoSuccess, setPersonalInfoSuccess] = useState(false);

  const handleSavePersonalInfo = async (e: FormEvent) => {
    e.preventDefault();
    setPersonalInfoError(null);
    setPersonalInfoSuccess(false);
    if (!personalInfo.fullName.trim()) {
      setPersonalInfoError('نام و نام خانوادگی الزامی است.');
      return;
    }
    if (!personalInfo.educationLevel.trim()) {
      setPersonalInfoError('آخرین مدرک تحصیلی الزامی است.');
      return;
    }
    try {
      // فیلدهای قدیمی SaveCandidateProfileCommand که هنوز در این فرم به‌طور مستقل ویرایش نمی‌شوند
      // (مهارت‌های آزاد، علایق، پاسخ‌های روان‌شناسی — طبق تصمیم صریح محصولی، آزمون روان‌شناسی به‌صورت
      // ماژول جداگانه به اسپرینت بعد موکول شده) در صورت وجود resume از مقدار فعلی بدون تغییر عبور
      // داده می‌شوند — این فرم آن‌ها را نمی‌سازد یا پاک نمی‌کند.
      // استثنا: workExperienceSummary («خلاصه رزومه» در نسخهٔ قدیمی) — تا وقتی کارجو مقدار مستقل
      // و متفاوتی از طریق رزومه‌ساز قدیمی برای آن ننوشته، از همین فیلد «درباره من / خلاصه رزومه»
      // پر می‌شود تا محاسبهٔ درصد تکمیل در داشبورد (CandidateDashboardHomePage.tsx) و نمای PDF
      // رزومه‌ساز قدیمی، برای کارجوهایی که فقط از صفحات جدید استفاده می‌کنند، خالی/ناقص نمانَد.
      // مقدار مستقل قبلی (اگر کارجو زمانی از رزومه‌ساز قدیمی استفاده کرده) هرگز بازنویسی نمی‌شود.
      await saveResume({
        fullName: personalInfo.fullName.trim(),
        militaryServiceStatus: personalInfo.militaryServiceStatus,
        educationLevel: personalInfo.educationLevel.trim(),
        workExperienceSummary: resume?.workExperienceSummary ?? (personalInfo.professionalSummary.trim() || undefined),
        skills: resume?.skills ?? undefined,
        interests: resume?.interests ?? undefined,
        psychologyAnswers: resume?.psychologyAnswers ?? undefined,
        email: personalInfo.email.trim() || undefined,
        city: personalInfo.city.trim() || undefined,
        jobTitle: personalInfo.jobTitle.trim() || undefined,
        professionalSummary: personalInfo.professionalSummary.trim() || undefined,
        linkedInUrl: personalInfo.linkedInUrl.trim() || undefined,
        gitHubUrl: personalInfo.gitHubUrl.trim() || undefined,
        personalWebsiteUrl: personalInfo.personalWebsiteUrl.trim() || undefined
      }).unwrap();
      setPersonalInfoSuccess(true);
    } catch {
      setPersonalInfoError(resume ? 'ذخیره مشخصات فردی با خطا مواجه شد.' : 'ایجاد رزومه با خطا مواجه شد.');
    }
  };

  // پیش‌نمایش محلی آواتار حین آپلود — طبق درخواست اصلاحی محصولی «بازخورد بصری آنی»: به‌جای اینکه
  // کارجو تا پایان آپلود صرفاً متن «در حال آپلود...» و تصویر قدیمی را ببیند، همان لحظه انتخاب فایل
  // از روی خود File یک Object URL محلی ساخته و نمایش داده می‌شود؛ با پایان موفق آپلود، کش «Resume»
  // نامعتبر شده و avatarUrl واقعی سرور جایگزین پیش‌نمایش محلی می‌شود (هدر بالای صفحه هم چون از همان
  // useGetMyResumeQuery در DashboardLayout استفاده می‌کند، بی‌درنگ به‌روزرسانی خواهد شد).
  const [avatarPreviewUrl, setAvatarPreviewUrl] = useState<string | null>(null);
  const [avatarError, setAvatarError] = useState<string | null>(null);
  const ALLOWED_AVATAR_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
  const MAX_AVATAR_SIZE_BYTES = 5 * 1024 * 1024; // ۵ مگابایت — دقیقاً مطابق محدودیت بک‌اند

  const handleAvatarChange = async (file: File | undefined) => {
    if (!file) return;
    setAvatarError(null);

    // اعتبارسنجی سمت کلاینت پیش از ارسال — بازخورد فوری بدون نیاز به رفت‌وبرگشت شبکه؛ بک‌اند هم
    // مستقل و به‌طور کامل همین دو قانون (فرمت/حجم) را دوباره اعتبارسنجی می‌کند.
    if (!ALLOWED_AVATAR_TYPES.includes(file.type)) {
      setAvatarError('فرمت تصویر باید jpg، jpeg، png یا webp باشد.');
      return;
    }
    if (file.size > MAX_AVATAR_SIZE_BYTES) {
      setAvatarError('حجم تصویر نباید بیشتر از ۵ مگابایت باشد.');
      return;
    }

    const localPreviewUrl = URL.createObjectURL(file);
    setAvatarPreviewUrl(localPreviewUrl);

    try {
      await uploadAvatar(file).unwrap();
    } catch {
      setAvatarError('بارگذاری تصویر با خطا مواجه شد. لطفاً دوباره تلاش کنید.');
    } finally {
      setAvatarPreviewUrl(null);
      URL.revokeObjectURL(localPreviewUrl);
    }
  };

  // ---------- سوابق شغلی ----------
  const [experienceDraft, setExperienceDraft] = useState<{
    id?: string; jobTitle: string; companyName: string; startYear: string; endYear: string; current: boolean; description: string;
  } | null>(null);

  const openNewExperience = () => setExperienceDraft({ jobTitle: '', companyName: '', startYear: '', endYear: '', current: false, description: '' });
  const openEditExperience = (exp: NonNullable<typeof resume>['workExperiences'][number]) =>
    setExperienceDraft({
      id: exp.id, jobTitle: exp.jobTitle, companyName: exp.companyName,
      startYear: String(exp.startYear), endYear: exp.endYear ? String(exp.endYear) : '',
      current: exp.endYear === null, description: exp.description ?? ''
    });

  const handleSaveExperience = async (e: FormEvent) => {
    e.preventDefault();
    if (!experienceDraft) return;
    if (!experienceDraft.jobTitle.trim() || !experienceDraft.companyName.trim() || !experienceDraft.startYear) return;
    try {
      await upsertWorkExperience({
        id: experienceDraft.id,
        jobTitle: experienceDraft.jobTitle.trim(),
        companyName: experienceDraft.companyName.trim(),
        startYear: Number(experienceDraft.startYear),
        endYear: experienceDraft.current ? undefined : experienceDraft.endYear ? Number(experienceDraft.endYear) : undefined,
        description: experienceDraft.description.trim() || undefined
      }).unwrap();
      setExperienceDraft(null);
    } catch {
      /* پیام خطا توسط Toast سراسری نمایش داده می‌شود. */
    }
  };

  // ---------- تحصیلات ----------
  const [educationDraft, setEducationDraft] = useState({ degreeLevel: '', fieldOfStudy: '', institutionName: '', graduationYear: '' });
  const [showEducationForm, setShowEducationForm] = useState(false);
  const handleAddEducation = async (e: FormEvent) => {
    e.preventDefault();
    if (!educationDraft.degreeLevel.trim() || !educationDraft.fieldOfStudy.trim() || !educationDraft.institutionName.trim()) return;
    try {
      await addEducation({
        degreeLevel: educationDraft.degreeLevel.trim(),
        fieldOfStudy: educationDraft.fieldOfStudy.trim(),
        institutionName: educationDraft.institutionName.trim(),
        graduationYear: educationDraft.graduationYear ? Number(educationDraft.graduationYear) : undefined
      }).unwrap();
      setEducationDraft({ degreeLevel: '', fieldOfStudy: '', institutionName: '', graduationYear: '' });
      setShowEducationForm(false);
    } catch {
      /* پیام خطا توسط Toast سراسری نمایش داده می‌شود. */
    }
  };

  // ---------- مدارک و گواهینامه‌ها ----------
  const [certificationDraft, setCertificationDraft] = useState({ title: '', issuingOrganization: '', yearObtained: '' });
  const [showCertificationForm, setShowCertificationForm] = useState(false);
  const handleAddCertification = async (e: FormEvent) => {
    e.preventDefault();
    if (!certificationDraft.title.trim() || !certificationDraft.issuingOrganization.trim() || !certificationDraft.yearObtained) return;
    try {
      await addCertification({
        title: certificationDraft.title.trim(),
        issuingOrganization: certificationDraft.issuingOrganization.trim(),
        yearObtained: Number(certificationDraft.yearObtained)
      }).unwrap();
      setCertificationDraft({ title: '', issuingOrganization: '', yearObtained: '' });
      setShowCertificationForm(false);
    } catch {
      /* پیام خطا توسط Toast سراسری نمایش داده می‌شود. */
    }
  };

  // ---------- مهارت‌ها (جایگزینی کامل فهرست) ----------
  const [skillDrafts, setSkillDrafts] = useState<CandidateSkillEntry[]>([]);
  const [skillsDirty, setSkillsDirty] = useState(false);
  useEffect(() => {
    if (resume) setSkillDrafts(resume.structuredSkills);
  }, [resume]);
  const [newSkill, setNewSkill] = useState({ name: '', category: '' });

  const addSkillRow = () => {
    if (!newSkill.name.trim() || !newSkill.category.trim()) return;
    setSkillDrafts((prev) => [...prev, { id: `draft-${Date.now()}`, name: newSkill.name.trim(), category: newSkill.category.trim(), level: 3 }]);
    setNewSkill({ name: '', category: '' });
    setSkillsDirty(true);
  };
  const removeSkillRow = (id: string) => {
    setSkillDrafts((prev) => prev.filter((s) => s.id !== id));
    setSkillsDirty(true);
  };
  const changeSkillLevel = (id: string, level: number) => {
    setSkillDrafts((prev) => prev.map((s) => (s.id === id ? { ...s, level } : s)));
    setSkillsDirty(true);
  };
  const handleSaveSkills = async () => {
    try {
      await updateSkills({ skills: skillDrafts.map(({ name, category, level }) => ({ name, category, level })) }).unwrap();
      setSkillsDirty(false);
    } catch {
      /* پیام خطا توسط Toast سراسری نمایش داده می‌شود. */
    }
  };

  // ---------- زبان‌ها (جایگزینی کامل فهرست) ----------
  const [languageDrafts, setLanguageDrafts] = useState<CandidateLanguageEntry[]>([]);
  const [languagesDirty, setLanguagesDirty] = useState(false);
  useEffect(() => {
    if (resume) setLanguageDrafts(resume.languages);
  }, [resume]);
  const [newLanguageName, setNewLanguageName] = useState('');

  const addLanguageRow = () => {
    if (!newLanguageName.trim()) return;
    setLanguageDrafts((prev) => [...prev, { id: `draft-${Date.now()}`, name: newLanguageName.trim(), proficiencyLevel: 'Intermediate' }]);
    setNewLanguageName('');
    setLanguagesDirty(true);
  };
  const removeLanguageRow = (id: string) => {
    setLanguageDrafts((prev) => prev.filter((l) => l.id !== id));
    setLanguagesDirty(true);
  };
  const changeLanguageRank = (id: string, rank: number) => {
    const level = PROFICIENCY_ORDER[Math.max(0, Math.min(4, rank - 1))];
    setLanguageDrafts((prev) => prev.map((l) => (l.id === id ? { ...l, proficiencyLevel: level } : l)));
    setLanguagesDirty(true);
  };
  const handleSaveLanguages = async () => {
    try {
      await updateLanguages({ languages: languageDrafts.map(({ name, proficiencyLevel }) => ({ name, proficiencyLevel })) }).unwrap();
      setLanguagesDirty(false);
    } catch {
      /* پیام خطا توسط Toast سراسری نمایش داده می‌شود. */
    }
  };

  // ---------- ترجیحات شغلی ----------
  const [preferences, setPreferences] = useState({
    preferredWorkType: null as string | null, minSalary: '', maxSalary: '', isActivelyLookingForJob: true
  });
  useEffect(() => {
    if (!resume) return;
    setPreferences({
      preferredWorkType: resume.preferredWorkType,
      minSalary: resume.minRequestedSalaryInToman != null ? String(resume.minRequestedSalaryInToman) : '',
      maxSalary: resume.maxRequestedSalaryInToman != null ? String(resume.maxRequestedSalaryInToman) : '',
      isActivelyLookingForJob: resume.isActivelyLookingForJob
    });
  }, [resume]);
  const [preferencesDirty, setPreferencesDirty] = useState(false);

  const handleSavePreferences = async () => {
    try {
      await updatePreferences({
        preferredWorkType: preferences.preferredWorkType,
        minRequestedSalaryInToman: preferences.minSalary ? Number(preferences.minSalary) : null,
        maxRequestedSalaryInToman: preferences.maxSalary ? Number(preferences.maxSalary) : null,
        isActivelyLookingForJob: preferences.isActivelyLookingForJob
      }).unwrap();
      setPreferencesDirty(false);
    } catch {
      /* پیام خطا توسط Toast سراسری نمایش داده می‌شود. */
    }
  };

  // طبق تصمیم صریح محصولی: این گیت فقط «در حال بارگذاری» واقعی را می‌گیرد، نه «هنوز رزومه‌ای
  // ندارد» — چون این صفحه اکنون باید بتواند اولین رزومهٔ یک کارجوی تازه را هم بسازد (به‌جای هدایت
  // او به رزومه‌ساز مرحله‌ای قدیمی).
  if (isLoadingProfile) {
    return (
      <div dir="rtl" className="font-vazir mx-auto max-w-xl px-4 py-16 text-center text-sm text-slate-500">
        در حال بارگذاری پروفایل...
      </div>
    );
  }

  return (
    // نکته: این صفحه اکنون داخل dash-content (که خودش padding دارد) رندر می‌شود، پس بدون
    // mx-auto/max-w/px/py خودش — تا فاصله‌گذاری دوبل و محدودیت عرض غیرلازم رخ ندهد.
    <div dir="rtl" className="font-vazir text-slate-800">
      {/* ---------- نوار بالا: ذخیره/پیش‌نمایش/درصد تکمیل ---------- */}
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-slate-200/80 bg-white px-5 py-3 shadow-sm">
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => (document.getElementById('personal-info-form') as HTMLFormElement | null)?.requestSubmit()}
            disabled={isSavingProfile}
            title="ذخیره مشخصات فردی (سایر بخش‌ها دکمه ذخیره مستقل خود را دارند)"
            className="inline-flex items-center gap-1.5 rounded-xl bg-emerald-600 px-4 py-2 text-xs font-semibold text-white disabled:opacity-60 hover:bg-emerald-700"
          >
            <Save size={14} /> {isSavingProfile ? 'در حال ذخیره...' : resume ? 'ذخیره تغییرات' : 'ایجاد رزومه'}
          </button>
          <Link
            to="/resume/view"
            className="inline-flex items-center gap-1.5 rounded-xl border border-slate-200 bg-white px-4 py-2 text-xs font-semibold text-slate-700 no-underline hover:bg-slate-50"
          >
            <Eye size={14} /> پیش‌نمایش
          </Link>
        </div>
        <div className="flex min-w-[200px] flex-1 items-center gap-3 sm:flex-initial sm:basis-72">
          <span className="whitespace-nowrap text-xs font-semibold text-emerald-700">تکمیل پروفایل {toPersianDigits(completionPercent)}٪</span>
          <div className="h-2 flex-1 overflow-hidden rounded-full bg-slate-100">
            <div className="h-full rounded-full bg-emerald-500 transition-all" style={{ width: `${completionPercent}%` }} />
          </div>
        </div>
      </div>

      {/* طبق تصمیم صریح محصولی: جریان پرداخت هزینه نهایی‌سازی رزومه اکنون مستقیماً همین‌جاست،
          نه پشت یک لینک به رزومه‌ساز مرحله‌ای قدیمی. */}
      {resume && !resume.isFeePaid && (
        <div className="mb-6">
          <ResumeFeePaymentCard amountInRials={resume.resumeCreationFeeAmountInRials} />
        </div>
      )}

      {/* طبق تصمیم صریح محصولی: این صفحه اکنون خودش می‌تواند اولین رزومهٔ یک کارجوی تازه را بسازد؛
          تا وقتی مشخصات پایه ذخیره نشده، بخش‌های سوابق/مهارت/زبان/ترجیحات (که به رزومهٔ ازپیش‌موجود
          نیاز دارند) پنهان هستند. */}
      {!resume && (
        <div className="mb-6 rounded-2xl border border-sky-200 bg-sky-50 p-4 text-xs leading-6 text-sky-800">
          هنوز رزومه‌ای برای شما ثبت نشده است. ابتدا مشخصات پایه زیر را تکمیل و ذخیره کنید؛ پس از آن
          بخش‌های سوابق شغلی/تحصیلی، مهارت‌ها، زبان‌ها و ترجیحات شغلی برای تکمیل در دسترس قرار می‌گیرند.
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-2">
        {/* ---------- مشخصات فردی ---------- */}
        <EditCard title="مشخصات فردی" icon={<User size={16} />} className={resume ? '' : 'lg:col-span-2'}>
          {resume && (
            <div className="mb-4 flex items-center gap-3">
              <div className="relative h-16 w-16 shrink-0">
                {avatarPreviewUrl || resume.avatarUrl ? (
                  <img
                    src={avatarPreviewUrl ?? resume.avatarUrl ?? undefined}
                    alt={resume.fullName}
                    className="h-16 w-16 rounded-full object-cover"
                  />
                ) : (
                  <span className="flex h-16 w-16 items-center justify-center rounded-full bg-emerald-50 text-emerald-600">
                    <User size={26} />
                  </span>
                )}
                {isUploadingAvatar && (
                  <span className="absolute inset-0 flex items-center justify-center rounded-full bg-black/40">
                    <Loader2 size={20} className="animate-spin text-white" />
                  </span>
                )}
              </div>
              <div>
                <label className="inline-flex cursor-pointer items-center gap-1.5 rounded-xl border border-slate-200 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50">
                  <Camera size={14} /> {isUploadingAvatar ? 'در حال آپلود...' : 'بارگذاری عکس'}
                  <input type="file" accept="image/jpeg,image/png,image/webp" className="hidden" disabled={isUploadingAvatar}
                    onChange={(e) => {
                      handleAvatarChange(e.target.files?.[0]);
                      e.target.value = '';
                    }} />
                </label>
                {avatarError && <p className="mt-1 text-[11px] text-rose-600">{avatarError}</p>}
              </div>
            </div>
          )}

          <form id="personal-info-form" onSubmit={handleSavePersonalInfo} className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <TextField label="نام و نام خانوادگی" value={personalInfo.fullName}
              onChange={(e) => setPersonalInfo((p) => ({ ...p, fullName: e.target.value }))} />
            <TextField label="عنوان شغلی" placeholder="مثلاً: تکنسین برق صنعتی" value={personalInfo.jobTitle}
              onChange={(e) => setPersonalInfo((p) => ({ ...p, jobTitle: e.target.value }))} />
            <TextField label="ایمیل" type="email" value={personalInfo.email}
              onChange={(e) => setPersonalInfo((p) => ({ ...p, email: e.target.value }))} />
            <label className="block text-xs font-semibold text-slate-600">
              شماره تماس
              <div className="mt-1 flex items-center gap-1.5 rounded-xl border border-slate-100 bg-slate-50 px-3 py-2 text-sm text-slate-500">
                <Phone size={14} /> {authUser?.mobileNumber ?? '—'}
              </div>
            </label>
            <TextField label="شهر محل سکونت" value={personalInfo.city}
              onChange={(e) => setPersonalInfo((p) => ({ ...p, city: e.target.value }))} />
            {/* طبق تصمیم صریح محصولی: پیش‌تر فقط در رزومه‌ساز مرحله‌ای (/resume) قابل‌ویرایش بود. */}
            <SelectField label="وضعیت نظام‌وظیفه" options={MilitaryServiceStatusLabels} value={personalInfo.militaryServiceStatus}
              onChange={(e) => setPersonalInfo((p) => ({ ...p, militaryServiceStatus: e.target.value }))} />
            <TextField label="آخرین مدرک تحصیلی" placeholder="مثلاً: کارشناسی برق قدرت" value={personalInfo.educationLevel}
              onChange={(e) => setPersonalInfo((p) => ({ ...p, educationLevel: e.target.value }))} />
            <TextField label="لینکدین" placeholder="https://linkedin.com/in/..." value={personalInfo.linkedInUrl}
              onChange={(e) => setPersonalInfo((p) => ({ ...p, linkedInUrl: e.target.value }))} />
            <TextField label="گیت‌هاب" placeholder="https://github.com/..." value={personalInfo.gitHubUrl}
              onChange={(e) => setPersonalInfo((p) => ({ ...p, gitHubUrl: e.target.value }))} />
            <TextField label="وب‌سایت شخصی / نمونه کار" placeholder="https://..." value={personalInfo.personalWebsiteUrl}
              onChange={(e) => setPersonalInfo((p) => ({ ...p, personalWebsiteUrl: e.target.value }))} />
            {/* طبق درخواست اصلاحی محصولی: این فیلد نقش «درباره من / خلاصه رزومه» را ایفا می‌کند —
                همان مفهومی که در رزومه‌ساز قدیمی «خلاصه رزومه» نامیده می‌شد. عنوان و راهنما به‌صورت
                صریح به هر دو نام اشاره می‌کنند تا کارجویی که از نسخهٔ قدیمی می‌آید گم نشود. */}
            <label className="col-span-full block text-xs font-semibold text-slate-600">
              درباره من / خلاصه رزومه
              <textarea rows={3} value={personalInfo.professionalSummary}
                placeholder="خلاصه‌ای کوتاه از تجربه، تخصص و اهداف شغلی خود بنویسید — همان چیزی که در رزومه‌ساز قدیمی «خلاصه رزومه» نام داشت."
                onChange={(e) => setPersonalInfo((p) => ({ ...p, professionalSummary: e.target.value }))}
                className="mt-1 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-800 focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
              />
            </label>

            {personalInfoError && <p className="col-span-full m-0 text-xs text-rose-600">{personalInfoError}</p>}
            {personalInfoSuccess && (
              <p className="col-span-full m-0 text-xs text-emerald-600">
                {resume ? 'مشخصات فردی ذخیره شد.' : 'رزومه شما ایجاد شد — اکنون می‌توانید سوابق، مهارت‌ها و سایر بخش‌ها را تکمیل کنید.'}
              </p>
            )}
          </form>
        </EditCard>

        {/* ---------- سوابق شغلی ---------- */}
        {resume && (
        <EditCard
          title="سوابق شغلی" icon={<Briefcase size={16} />}
          action={
            <button type="button" onClick={openNewExperience} className="inline-flex items-center gap-1 rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-emerald-700">
              <Plus size={13} /> افزودن سابقه کار
            </button>
          }
        >
          {experienceDraft && (
            <form onSubmit={handleSaveExperience} className="mb-4 rounded-xl border border-emerald-100 bg-emerald-50/40 p-3">
              <div className="mb-2 flex items-center justify-between">
                <strong className="text-xs text-emerald-800">{experienceDraft.id ? 'ویرایش سابقه شغلی' : 'افزودن سابقه شغلی'}</strong>
                <button type="button" onClick={() => setExperienceDraft(null)} className="bg-transparent p-0 text-slate-400 hover:text-slate-600"><X size={15} /></button>
              </div>
              <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
                <TextField label="عنوان شغلی" value={experienceDraft.jobTitle} onChange={(e) => setExperienceDraft((d) => d && { ...d, jobTitle: e.target.value })} />
                <TextField label="نام شرکت" value={experienceDraft.companyName} onChange={(e) => setExperienceDraft((d) => d && { ...d, companyName: e.target.value })} />
                <TextField label="سال شروع" type="number" value={experienceDraft.startYear} onChange={(e) => setExperienceDraft((d) => d && { ...d, startYear: e.target.value })} />
                <TextField
                  label="سال پایان" type="number" value={experienceDraft.endYear} disabled={experienceDraft.current}
                  onChange={(e) => setExperienceDraft((d) => d && { ...d, endYear: e.target.value })}
                />
                <label className="col-span-full flex items-center gap-2 text-xs font-medium text-slate-600">
                  <input type="checkbox" checked={experienceDraft.current} onChange={(e) => setExperienceDraft((d) => d && { ...d, current: e.target.checked })} />
                  هم‌اکنون مشغول هستم
                </label>
                <label className="col-span-full block text-xs font-semibold text-slate-600">
                  شرح وظایف و دستاوردها
                  <textarea rows={3} value={experienceDraft.description} onChange={(e) => setExperienceDraft((d) => d && { ...d, description: e.target.value })}
                    className="mt-1 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-800 focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500" />
                </label>
              </div>
              <div className="mt-2 flex gap-2">
                <button type="submit" disabled={isSavingExperience} className="rounded-lg bg-emerald-600 px-4 py-1.5 text-xs font-semibold text-white disabled:opacity-60">
                  {isSavingExperience ? 'در حال ذخیره...' : 'ذخیره'}
                </button>
                <button type="button" onClick={() => setExperienceDraft(null)} className="rounded-lg border border-slate-200 bg-white px-4 py-1.5 text-xs font-semibold text-slate-600">انصراف</button>
              </div>
            </form>
          )}

          {resume.workExperiences.length === 0 ? (
            <p className="m-0 text-sm text-slate-500">هنوز سابقه شغلی ثبت نشده است.</p>
          ) : (
            <ul className="m-0 flex list-none flex-col gap-3 p-0">
              {resume.workExperiences.map((exp) => (
                <li key={exp.id} className="rounded-xl bg-slate-50 p-3">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <p className="m-0 text-sm font-bold text-slate-800">{exp.jobTitle}</p>
                      <p className="m-0 text-xs text-slate-500">{exp.companyName}</p>
                      <p className="m-0 text-xs text-slate-400">
                        {toPersianDigits(exp.startYear)} - {exp.endYear ? toPersianDigits(exp.endYear) : 'در حال کار'}
                      </p>
                    </div>
                    <div className="flex shrink-0 gap-1">
                      <button type="button" onClick={() => openEditExperience(exp)} className="rounded-lg p-1.5 text-slate-500 hover:bg-slate-100"><Pencil size={14} /></button>
                      <button type="button" onClick={() => deleteWorkExperience(exp.id)} className="rounded-lg p-1.5 text-rose-500 hover:bg-rose-50"><Trash2 size={14} /></button>
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </EditCard>
        )}
      </div>

      {/* ---------- مهارت‌ها ---------- */}
      {resume && (
      <div className="mt-6">
        <EditCard
          title="مهارت‌ها" icon={<Award size={16} />}
          action={skillsDirty && (
            <button type="button" onClick={handleSaveSkills} disabled={isSavingSkills} className="inline-flex items-center gap-1 rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-semibold text-white disabled:opacity-60">
              <Save size={13} /> {isSavingSkills ? 'در حال ذخیره...' : 'ذخیره مهارت‌ها'}
            </button>
          )}
        >
          <div className="mb-3 flex flex-wrap gap-2">
            {skillDrafts.map((skill) => (
              <span key={skill.id} className="inline-flex items-center gap-2 rounded-lg bg-slate-100 py-1.5 pe-1.5 ps-2.5 text-xs font-medium text-slate-700">
                {skill.name}
                <RankPicker value={skill.level} onChange={(rank) => changeSkillLevel(skill.id, rank)} icon={Circle} />
                <button type="button" onClick={() => removeSkillRow(skill.id)} className="bg-transparent p-0 text-slate-400 hover:text-rose-500"><X size={13} /></button>
              </span>
            ))}
            {skillDrafts.length === 0 && <p className="m-0 text-sm text-slate-500">هنوز مهارتی ثبت نشده — یکی اضافه کنید.</p>}
          </div>
          <div className="flex flex-wrap items-end gap-2">
            <label className="text-xs font-semibold text-slate-600">
              نام مهارت
              <input value={newSkill.name} onChange={(e) => setNewSkill((s) => ({ ...s, name: e.target.value }))}
                placeholder="مثلاً: جوشکاری برق"
                className="mt-1 block rounded-xl border border-slate-200 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500" />
            </label>
            <label className="text-xs font-semibold text-slate-600">
              دسته‌بندی
              <input value={newSkill.category} onChange={(e) => setNewSkill((s) => ({ ...s, category: e.target.value }))}
                placeholder="مثلاً: فنی"
                className="mt-1 block rounded-xl border border-slate-200 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500" />
            </label>
            <button type="button" onClick={addSkillRow} className="inline-flex items-center gap-1 rounded-xl border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-50">
              <Plus size={14} /> افزودن مهارت
            </button>
          </div>
        </EditCard>
      </div>
      )}

      {resume && (
      <div className="mt-6 grid gap-6 lg:grid-cols-3">
        {/* ---------- تحصیلات و مدارک ---------- */}
        <EditCard title="تحصیلات و مدارک" icon={<GraduationCap size={16} />}>
          <div className="mb-3 flex flex-col gap-2">
            {resume.educations.map((edu) => (
              <div key={edu.id} className="flex items-start justify-between gap-2 rounded-xl bg-slate-50 p-2.5">
                <div>
                  <p className="m-0 text-xs font-bold text-slate-800">{edu.degreeLevel} — {edu.fieldOfStudy}</p>
                  <p className="m-0 text-[11px] text-slate-500">{edu.institutionName}{edu.graduationYear ? ` • ${toPersianDigits(edu.graduationYear)}` : ''}</p>
                </div>
                <button type="button" onClick={() => deleteEducation(edu.id)} className="shrink-0 rounded-lg p-1 text-rose-500 hover:bg-rose-50"><Trash2 size={13} /></button>
              </div>
            ))}
            {resume.certifications.map((cert) => (
              <div key={cert.id} className="flex items-start justify-between gap-2 rounded-xl bg-slate-50 p-2.5">
                <div>
                  <p className="m-0 flex items-center gap-1 text-xs font-bold text-slate-800"><Award size={12} /> {cert.title}</p>
                  <p className="m-0 text-[11px] text-slate-500">{cert.issuingOrganization} • {toPersianDigits(cert.yearObtained)}</p>
                </div>
                <button type="button" onClick={() => deleteCertification(cert.id)} className="shrink-0 rounded-lg p-1 text-rose-500 hover:bg-rose-50"><Trash2 size={13} /></button>
              </div>
            ))}
            {resume.educations.length === 0 && resume.certifications.length === 0 && (
              <p className="m-0 text-sm text-slate-500">هنوز موردی ثبت نشده است.</p>
            )}
          </div>

          <div className="flex gap-2">
            <button type="button" onClick={() => setShowEducationForm((v) => !v)} className="rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50">+ تحصیلات</button>
            <button type="button" onClick={() => setShowCertificationForm((v) => !v)} className="rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50">+ مدرک/گواهینامه</button>
          </div>

          {showEducationForm && (
            <form onSubmit={handleAddEducation} className="mt-3 flex flex-col gap-2 rounded-xl border border-slate-100 p-2.5">
              <TextField label="مقطع تحصیلی" value={educationDraft.degreeLevel} onChange={(e) => setEducationDraft((d) => ({ ...d, degreeLevel: e.target.value }))} />
              <TextField label="رشته تحصیلی" value={educationDraft.fieldOfStudy} onChange={(e) => setEducationDraft((d) => ({ ...d, fieldOfStudy: e.target.value }))} />
              <TextField label="نام مؤسسه/دانشگاه" value={educationDraft.institutionName} onChange={(e) => setEducationDraft((d) => ({ ...d, institutionName: e.target.value }))} />
              <TextField label="سال فارغ‌التحصیلی (اختیاری)" type="number" value={educationDraft.graduationYear} onChange={(e) => setEducationDraft((d) => ({ ...d, graduationYear: e.target.value }))} />
              <button type="submit" className="rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-semibold text-white">افزودن</button>
            </form>
          )}
          {showCertificationForm && (
            <form onSubmit={handleAddCertification} className="mt-3 flex flex-col gap-2 rounded-xl border border-slate-100 p-2.5">
              <TextField label="عنوان مدرک" value={certificationDraft.title} onChange={(e) => setCertificationDraft((d) => ({ ...d, title: e.target.value }))} />
              <TextField label="موسسه صادرکننده" value={certificationDraft.issuingOrganization} onChange={(e) => setCertificationDraft((d) => ({ ...d, issuingOrganization: e.target.value }))} />
              <TextField label="سال اخذ" type="number" value={certificationDraft.yearObtained} onChange={(e) => setCertificationDraft((d) => ({ ...d, yearObtained: e.target.value }))} />
              <button type="submit" className="rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-semibold text-white">افزودن</button>
            </form>
          )}
        </EditCard>

        {/* ---------- زبان‌ها ---------- */}
        <EditCard
          title="مهارت‌های زبانی" icon={<LanguagesIcon size={16} />}
          action={languagesDirty && (
            <button type="button" onClick={handleSaveLanguages} disabled={isSavingLanguages} className="inline-flex items-center gap-1 rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-semibold text-white disabled:opacity-60">
              <Save size={13} /> {isSavingLanguages ? 'در حال ذخیره...' : 'ذخیره'}
            </button>
          )}
        >
          <div className="mb-3 flex flex-col gap-2">
            {languageDrafts.map((lang) => (
              <div key={lang.id} className="flex items-center justify-between gap-2">
                <span className="text-sm font-medium text-slate-700">{lang.name}</span>
                <div className="flex items-center gap-2">
                  <RankPicker value={LanguageProficiencyLevelRank[lang.proficiencyLevel] ?? 0} onChange={(rank) => changeLanguageRank(lang.id, rank)} />
                  <span className="w-20 text-[11px] text-slate-400">{LanguageProficiencyLevelLabels[lang.proficiencyLevel] ?? lang.proficiencyLevel}</span>
                  <button type="button" onClick={() => removeLanguageRow(lang.id)} className="bg-transparent p-0 text-slate-400 hover:text-rose-500"><X size={14} /></button>
                </div>
              </div>
            ))}
            {languageDrafts.length === 0 && <p className="m-0 text-sm text-slate-500">هنوز زبانی ثبت نشده است.</p>}
          </div>
          <div className="flex gap-2">
            <input value={newLanguageName} onChange={(e) => setNewLanguageName(e.target.value)} placeholder="نام زبان (مثلاً: انگلیسی)"
              className="flex-1 rounded-xl border border-slate-200 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500" />
            <button type="button" onClick={addLanguageRow} className="inline-flex items-center gap-1 rounded-xl border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-50">
              <Plus size={14} /> افزودن
            </button>
          </div>
        </EditCard>

        {/* ---------- ترجیحات شغلی ---------- */}
        <EditCard
          title="ترجیحات شغلی" icon={<Wallet size={16} />}
          action={preferencesDirty && (
            <button type="button" onClick={handleSavePreferences} disabled={isSavingPreferences} className="inline-flex items-center gap-1 rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-semibold text-white disabled:opacity-60">
              <Save size={13} /> {isSavingPreferences ? 'در حال ذخیره...' : 'ذخیره'}
            </button>
          )}
        >
          <label className="mb-3 block text-xs font-semibold text-slate-600">
            محدوده حقوق درخواستی (تومان)
            <div className="mt-1 flex items-center gap-2">
              <input type="number" value={preferences.minSalary} placeholder="حداقل"
                onChange={(e) => { setPreferences((p) => ({ ...p, minSalary: e.target.value })); setPreferencesDirty(true); }}
                className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500" />
              <span className="text-slate-400">تا</span>
              <input type="number" value={preferences.maxSalary} placeholder="حداکثر"
                onChange={(e) => { setPreferences((p) => ({ ...p, maxSalary: e.target.value })); setPreferencesDirty(true); }}
                className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500" />
            </div>
            {(preferences.minSalary || preferences.maxSalary) && (
              <p className="m-0 mt-1 text-[11px] text-slate-400">
                {preferences.minSalary ? formatToman(Number(preferences.minSalary)) : '—'} تا {preferences.maxSalary ? formatToman(Number(preferences.maxSalary)) : '—'} تومان
              </p>
            )}
          </label>

          <div className="mb-3">
            <span className="mb-1 block text-xs font-semibold text-slate-600">نوع همکاری</span>
            <div className="flex gap-1.5">
              {WORK_TYPE_OPTIONS.map((opt) => (
                <button
                  key={opt.value} type="button"
                  onClick={() => { setPreferences((p) => ({ ...p, preferredWorkType: opt.value })); setPreferencesDirty(true); }}
                  className={`inline-flex flex-1 items-center justify-center gap-1 rounded-lg border px-2 py-1.5 text-[11px] font-semibold ${
                    preferences.preferredWorkType === opt.value ? 'border-emerald-600 bg-emerald-50 text-emerald-700' : 'border-slate-200 text-slate-600 hover:bg-slate-50'
                  }`}
                >
                  {opt.icon} {PreferredWorkTypeLabels[opt.value]}
                </button>
              ))}
            </div>
          </div>

          <label className="flex items-center justify-between gap-2 text-xs font-semibold text-slate-600">
            وضعیت جستجوی کار (فعال — در جستجوی فرصت‌های جدید)
            <button
              type="button" role="switch" aria-checked={preferences.isActivelyLookingForJob}
              onClick={() => { setPreferences((p) => ({ ...p, isActivelyLookingForJob: !p.isActivelyLookingForJob })); setPreferencesDirty(true); }}
              className={`relative h-6 w-11 shrink-0 rounded-full transition-colors ${preferences.isActivelyLookingForJob ? 'bg-emerald-500' : 'bg-slate-300'}`}
            >
              <span className={`absolute top-0.5 h-5 w-5 rounded-full bg-white shadow transition-transform ${preferences.isActivelyLookingForJob ? '-translate-x-0.5' : '-translate-x-5'}`} />
            </button>
          </label>
        </EditCard>
      </div>
      )}
    </div>
  );
}
