import { useState, type ChangeEvent, type FormEvent } from 'react';
import { useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import {
  useAddCandidateEducationMutation,
  useAddCandidateWorkExperienceMutation,
  useDeleteCandidateEducationMutation,
  useDeleteCandidateWorkExperienceMutation,
  useGetMyResumeQuery,
  useSaveResumeMutation,
  useSubmitResumeFeePaymentMutation,
  useUploadAvatarMutation,
  type CandidateFormValues
} from '@/features/candidates/candidateApi';

const toToman = (rials: number) => Math.round(rials / 10).toLocaleString('fa-IR');

const militaryOptions: Record<string, string> = {
  NotApplicable: 'مشمول نیست',
  Completed: 'پایان خدمت',
  Exempted: 'معافیت',
  InProgress: 'در حال انجام خدمت'
};

// شش سؤال ثابت عمومی روان‌شناسی کار — پاسخ‌ها به‌صورت متن ترکیبی خوانا در فیلد PsychologyAnswers ذخیره می‌شود.
const PSYCHOLOGY_QUESTIONS: { key: `psych${1 | 2 | 3 | 4 | 5}`; label: string }[] = [
  { key: 'psych1', label: 'در مواجهه با یک مشکل غیرمنتظره در محیط کار معمولاً چه واکنشی نشان می‌دهید؟' },
  { key: 'psych2', label: 'ترجیح می‌دهید به‌صورت تیمی کار کنید یا مستقل؟ چرا؟' },
  { key: 'psych3', label: 'در شرایط فشار کاری و کمبود زمان، چگونه اولویت‌بندی می‌کنید؟' },
  { key: 'psych4', label: 'یک نقطه‌قوت و یک نقطه‌ضعف شخصیتی خود را که بر کار اثر می‌گذارد بیان کنید.' },
  { key: 'psych5', label: 'انگیزه اصلی شما برای ادامه فعالیت در این حرفه چیست؟' }
];

type WizardValues = CandidateFormValues & {
  psych1?: string;
  psych2?: string;
  psych3?: string;
  psych4?: string;
  psych5?: string;
};

const STEP_TITLES = [
  'اطلاعات شخصی',
  'تحصیلات و نظام‌وظیفه',
  'سوابق کاری',
  'مهارت‌ها',
  'علاقه‌مندی‌ها',
  'سؤالات روان‌شناسی عمومی',
  'مرور و نهایی‌سازی'
] as const;

const TOTAL_STEPS = STEP_TITLES.length;

function toPersianDigits(n: number) {
  return n.toLocaleString('fa-IR');
}

export default function ResumeFormPage() {
  const { data, isLoading } = useGetMyResumeQuery();
  const [saveResume, { isLoading: isSaving }] = useSaveResumeMutation();
  const [submitResumeFeePayment, { isLoading: isRedirectingToPayment }] = useSubmitResumeFeePaymentMutation();
  const [uploadAvatar, { isLoading: isUploadingAvatar }] = useUploadAvatarMutation();
  const [avatarError, setAvatarError] = useState<string | null>(null);

  // طبق فاز «مدیریت رزومه‌ها و متقاضیان» — فرم‌های افزودن ردیف‌های ساختاریافته تحصیلات/سوابق شغلی.
  const [addEducation, { isLoading: isAddingEducation }] = useAddCandidateEducationMutation();
  const [deleteEducation] = useDeleteCandidateEducationMutation();
  const [addWorkExperience, { isLoading: isAddingWorkExperience }] = useAddCandidateWorkExperienceMutation();
  const [deleteWorkExperience] = useDeleteCandidateWorkExperienceMutation();
  const [educationDraft, setEducationDraft] = useState({ degreeLevel: '', fieldOfStudy: '', institutionName: '', graduationYear: '' });
  const [educationError, setEducationError] = useState<string | null>(null);
  const [workExperienceDraft, setWorkExperienceDraft] = useState({ jobTitle: '', companyName: '', startYear: '', endYear: '', description: '' });
  const [workExperienceError, setWorkExperienceError] = useState<string | null>(null);

  const handleAddEducation = async (e: FormEvent) => {
    e.preventDefault();
    setEducationError(null);
    if (!educationDraft.degreeLevel.trim() || !educationDraft.fieldOfStudy.trim() || !educationDraft.institutionName.trim()) {
      setEducationError('مقطع، رشته تحصیلی و نام مؤسسه الزامی است.');
      return;
    }
    try {
      await addEducation({
        degreeLevel: educationDraft.degreeLevel.trim(),
        fieldOfStudy: educationDraft.fieldOfStudy.trim(),
        institutionName: educationDraft.institutionName.trim(),
        graduationYear: educationDraft.graduationYear ? Number(educationDraft.graduationYear) : undefined
      }).unwrap();
      setEducationDraft({ degreeLevel: '', fieldOfStudy: '', institutionName: '', graduationYear: '' });
    } catch {
      setEducationError('افزودن سابقه تحصیلی با خطا مواجه شد.');
    }
  };

  const handleAddWorkExperience = async (e: FormEvent) => {
    e.preventDefault();
    setWorkExperienceError(null);
    if (!workExperienceDraft.jobTitle.trim() || !workExperienceDraft.companyName.trim() || !workExperienceDraft.startYear) {
      setWorkExperienceError('عنوان شغلی، نام شرکت و سال شروع الزامی است.');
      return;
    }
    try {
      await addWorkExperience({
        jobTitle: workExperienceDraft.jobTitle.trim(),
        companyName: workExperienceDraft.companyName.trim(),
        startYear: Number(workExperienceDraft.startYear),
        endYear: workExperienceDraft.endYear ? Number(workExperienceDraft.endYear) : undefined,
        description: workExperienceDraft.description.trim() || undefined
      }).unwrap();
      setWorkExperienceDraft({ jobTitle: '', companyName: '', startYear: '', endYear: '', description: '' });
    } catch {
      setWorkExperienceError('افزودن سابقه شغلی با خطا مواجه شد.');
    }
  };

  const handleAvatarChange = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setAvatarError(null);
    try {
      await uploadAvatar(file).unwrap();
    } catch {
      setAvatarError('آپلود تصویر ناموفق بود. لطفاً فرمت jpg، jpeg، png یا webp را انتخاب کنید.');
    } finally {
      e.target.value = '';
    }
  };
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [serverError, setServerError] = useState<string | null>(null);
  const [paymentError, setPaymentError] = useState<string | null>(null);
  const [step, setStep] = useState(1);

  const resume = data?.data;

  const psychDefaults = (() => {
    const parsed: Record<string, string> = {};
    if (!resume?.psychologyAnswers) return parsed;
    // بازخوانی ساده پاسخ‌های قبلی از متن ترکیبی ذخیره‌شده (بر اساس همان قالب سؤال/پاسخ).
    const blocks = resume.psychologyAnswers.split('\n\n');
    PSYCHOLOGY_QUESTIONS.forEach((q, i) => {
      const block = blocks[i];
      if (block) {
        const answerLine = block.split('\n').find((l) => l.startsWith('پاسخ: '));
        if (answerLine) parsed[q.key] = answerLine.replace('پاسخ: ', '');
      }
    });
    return parsed;
  })();

  const { register, handleSubmit, watch, trigger, formState: { errors } } = useForm<WizardValues>({
    defaultValues: {
      fullName: resume?.fullName ?? '',
      militaryServiceStatus: resume?.militaryServiceStatus ?? 'NotApplicable',
      educationLevel: resume?.educationLevel ?? '',
      workExperienceSummary: resume?.workExperienceSummary ?? '',
      skills: resume?.skills ?? '',
      interests: resume?.interests ?? '',
      email: resume?.email ?? '',
      city: resume?.city ?? '',
      ...psychDefaults
    }
  });

  const values = watch();

  const STEP_FIELDS: Record<number, (keyof WizardValues)[]> = {
    1: ['fullName'],
    2: ['educationLevel', 'militaryServiceStatus'],
    3: [],
    4: [],
    5: [],
    6: [],
    7: []
  };

  const goNext = async () => {
    const fields = STEP_FIELDS[step] ?? [];
    const isValid = fields.length === 0 || (await trigger(fields));
    if (isValid && step < TOTAL_STEPS) setStep(step + 1);
  };

  const goPrev = () => {
    if (step > 1) setStep(step - 1);
  };

  const onSubmit = async (formValues: WizardValues) => {
    setServerError(null);
    setSuccessMessage(null);
    setPaymentError(null);

    const psychologyAnswers = PSYCHOLOGY_QUESTIONS
      .map((q) => `سؤال: ${q.label}\nپاسخ: ${formValues[q.key]?.trim() || '—'}`)
      .join('\n\n');

    const payload: CandidateFormValues = {
      fullName: formValues.fullName,
      militaryServiceStatus: formValues.militaryServiceStatus,
      educationLevel: formValues.educationLevel,
      workExperienceSummary: formValues.workExperienceSummary,
      skills: formValues.skills,
      interests: formValues.interests,
      email: formValues.email,
      city: formValues.city,
      psychologyAnswers
    };

    const result = await saveResume(payload);

    if ('data' in result && result.data?.success) {
      setSuccessMessage(
        result.data.data?.isFeePaid === false
          ? 'اطلاعات رزومه ذخیره شد. برای نهایی‌سازی و دانلود، هزینه ساخت رزومه را پرداخت کنید.'
          : 'رزومه شما ذخیره و نهایی شد.'
      );
      return;
    }

    const errorResponse = 'error' in result ? (result.error as { data?: { message?: string } }) : undefined;
    setServerError(errorResponse?.data?.message ?? 'ذخیره رزومه با خطا مواجه شد.');
  };

  const handlePayFee = async () => {
    setPaymentError(null);
    const result = await submitResumeFeePayment();

    if ('data' in result && result.data?.success) {
      const redirectUrl = result.data.data?.paymentRedirectUrl;
      if (redirectUrl) {
        window.location.href = redirectUrl;
        return;
      }
      setSuccessMessage('رزومه شما پیش‌تر نهایی شده است.');
      return;
    }

    const errorResponse = 'error' in result ? (result.error as { data?: { message?: string } }) : undefined;
    setPaymentError(errorResponse?.data?.message ?? 'شروع فرآیند پرداخت با خطا مواجه شد.');
  };

  const handleDownloadPdf = () => {
    if (!resume?.isFeePaid) return;
    window.print();
  };

  if (isLoading) return <div>در حال بارگذاری...</div>;

  return (
    <div>
      <style>{`
        @media print {
          body * { visibility: hidden; }
          #resume-print-area, #resume-print-area * { visibility: visible; }
          #resume-print-area { position: absolute; top: 0; left: 0; width: 100%; padding: 2rem; }
        }
      `}</style>

      {!resume && (
        <motion.div
          className="card"
          initial={{ opacity: 0, y: -8 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3 }}
          style={{ maxWidth: 640, margin: '0 auto var(--space-4)', background: 'rgba(37, 99, 235, 0.06)', borderColor: 'var(--color-primary, #2563eb)' }}
        >
          <h2 style={{ marginTop: 0 }}>رزومه‌ساز حرفه‌ای</h2>
          <p className="text-muted" style={{ margin: 0 }}>
            با پاسخ به چند مرحله ساده (اطلاعات شخصی، تحصیلات، سوابق، مهارت‌ها، علاقه‌مندی‌ها و چند سؤال روان‌شناسی کار)
            یک رزومه حرفه‌ای بسازید و در پایان فایل PDF آن را دانلود کنید.
          </p>
        </motion.div>
      )}

      {/* طبق تصمیم صریح محصولی «قطع کامل وابستگی به رزومه‌ساز مرحله‌ای»: بخش «پیگیری وضعیت
          درخواست‌های من» (شامل کارت‌های آماری و جدول) از این صفحه استخراج و به صفحهٔ اختصاصی
          CandidateApplicationsPage.tsx در مسیر /applications منتقل شده است. */}
      <div className="card" style={{ maxWidth: 640, margin: '0 auto var(--space-4)', textAlign: 'center' }}>
        <p className="text-muted" style={{ margin: 0 }}>
          برای پیگیری وضعیت درخواست‌های ارسالی به آگهی‌های شغلی، به{' '}
          <Link to="/applications">صفحهٔ «درخواست‌های من»</Link> مراجعه کنید.
        </p>
      </div>

      {resume && !resume.isFeePaid && (
        <motion.div
          className="card"
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.35 }}
          style={{
            maxWidth: 640,
            margin: '0 auto var(--space-4)',
            borderColor: 'var(--color-warning, #d97706)',
            background: 'rgba(217, 119, 6, 0.06)'
          }}
        >
          <h2 style={{ marginTop: 0 }}>نهایی‌سازی رزومه</h2>
          <p className="text-muted">
            اطلاعات رزومه شما به‌صورت پیش‌نویس ذخیره شده، اما تا پرداخت هزینه ساخت رزومه
            (<strong>{toToman(resume.resumeCreationFeeAmountInRials)} تومان</strong>) قابل دانلود یا استفاده برای ارسال به آگهی‌ها نیست.
            این هزینه فقط یک‌بار برای هر رزومه است؛ ویرایش‌های بعدی رایگان خواهد بود.
          </p>
          {paymentError && <p className="error-text">{paymentError}</p>}
          <button type="button" className="btn-primary" onClick={handlePayFee} disabled={isRedirectingToPayment}>
            {isRedirectingToPayment ? 'در حال انتقال به درگاه پرداخت...' : `پرداخت ${toToman(resume.resumeCreationFeeAmountInRials)} تومان و نهایی‌سازی`}
          </button>
        </motion.div>
      )}

      <div className="card" style={{ maxWidth: 720, margin: '0 auto var(--space-4)' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.75rem', flexWrap: 'wrap', gap: '0.5rem' }}>
          <h1 style={{ margin: 0 }}>رزومه‌ساز حرفه‌ای مرحله‌ای</h1>
          <span className="badge" style={{ background: '#eef2ff', color: '#3730a3' }}>
            مرحله {toPersianDigits(step)} از {toPersianDigits(TOTAL_STEPS)} — {STEP_TITLES[step - 1]}
          </span>
        </div>

        {/* طبق فاز «پروفایل و رزومه‌ساز کارجو» — نمای حرفه‌ای/فقط-خواندنی رزومه و صفحه ویرایش کامل‌تر
            (job title، لینکدین/گیت‌هاب/وب‌سایت، مهارت‌های ساختاریافته، زبان‌ها، ترجیحات شغلی). */}
        {resume && (
          <p style={{ marginTop: 0, display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
            <Link to="/resume/view">مشاهده نمای حرفه‌ای رزومه ←</Link>
            <Link to="/resume/edit">ویرایش پیشرفته پروفایل (مهارت‌ها، زبان‌ها، ترجیحات شغلی) ←</Link>
          </p>
        )}

        {resume?.resumeFileUrl && (
          <p style={{ fontSize: 'var(--fs-sm)', color: 'var(--color-muted)' }}>
            فایل رزومه ارسالی قبلی شما: <a href={resume.resumeFileUrl} target="_blank" rel="noreferrer">دانلود</a>
          </p>
        )}

        <form onSubmit={handleSubmit(onSubmit)}>
          {step === 1 && (
            <>
              <div className="field">
                <label>تصویر پروفایل</label>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                  {resume?.avatarUrl ? (
                    <img
                      src={resume.avatarUrl}
                      alt="آواتار"
                      style={{ width: 56, height: 56, borderRadius: '50%', objectFit: 'cover' }}
                    />
                  ) : (
                    <span
                      style={{
                        width: 56, height: 56, borderRadius: '50%', background: '#eef2ff',
                        display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '1.5rem'
                      }}
                    >
                      🙂
                    </span>
                  )}
                  <label className="btn-secondary" style={{ cursor: 'pointer' }}>
                    {isUploadingAvatar ? 'در حال آپلود...' : 'انتخاب تصویر'}
                    <input type="file" accept="image/jpeg,image/png,image/webp" onChange={handleAvatarChange} style={{ display: 'none' }} disabled={isUploadingAvatar} />
                  </label>
                </div>
                <p style={{ fontSize: 'var(--fs-sm)', color: 'var(--color-muted)', marginTop: '0.4rem' }}>
                  این تصویر هنگام ارسال درخواست برای یک آگهی، به همراه سایر مشخصات شما در اختیار همان کارفرما قرار می‌گیرد.
                </p>
                {avatarError && <span className="error-text">{avatarError}</span>}
              </div>

              <div className="field">
                <label>نام و نام خانوادگی</label>
                <input {...register('fullName', { required: 'نام و نام خانوادگی الزامی است.' })} />
                {errors.fullName && <span className="error-text">{errors.fullName.message}</span>}
              </div>

              <div className="field">
                <label>ایمیل (اختیاری)</label>
                <input
                  type="email"
                  placeholder="example@email.com"
                  {...register('email', {
                    pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: 'فرمت ایمیل نامعتبر است.' }
                  })}
                />
                {errors.email && <span className="error-text">{errors.email.message}</span>}
              </div>

              <div className="field">
                <label>شهر محل سکونت (اختیاری)</label>
                <input placeholder="مثلاً: تهران" {...register('city')} />
              </div>
            </>
          )}

          {step === 2 && (
            <>
              <div className="field">
                <label>آخرین مدرک تحصیلی</label>
                <input {...register('educationLevel', { required: 'مدرک تحصیلی الزامی است.' })} />
                {errors.educationLevel && <span className="error-text">{errors.educationLevel.message}</span>}
              </div>
              <div className="field">
                <label>وضعیت نظام وظیفه</label>
                <select {...register('militaryServiceStatus', { required: true })}>
                  {Object.entries(militaryOptions).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                </select>
              </div>

              <div style={{ marginTop: '1.25rem', paddingTop: '1rem', borderTop: '1px solid var(--color-border, #e5e7eb)' }}>
                <h3 style={{ margin: '0 0 0.5rem' }}>سوابق تحصیلی (اختیاری، چند مورد)</h3>
                <p className="text-muted" style={{ fontSize: 'var(--fs-sm)', marginTop: 0 }}>
                  علاوه بر آخرین مدرک تحصیلی بالا، می‌توانید ردیف‌های جداگانه‌ای برای هر مقطع تحصیلی اضافه کنید.
                </p>

                {resume?.educations && resume.educations.length > 0 && (
                  <ul style={{ listStyle: 'none', padding: 0, margin: '0 0 0.75rem' }}>
                    {resume.educations.map((edu) => (
                      <li
                        key={edu.id}
                        style={{
                          display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                          padding: '0.5rem 0.75rem', borderRadius: 8, background: '#f9fafb', marginBottom: '0.4rem'
                        }}
                      >
                        <span>
                          <strong>{edu.degreeLevel}</strong> — {edu.fieldOfStudy}، {edu.institutionName}
                          {edu.graduationYear ? ` (${toPersianDigits(edu.graduationYear)})` : ''}
                        </span>
                        <button type="button" className="btn-secondary" onClick={() => deleteEducation(edu.id)}>حذف</button>
                      </li>
                    ))}
                  </ul>
                )}

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem' }}>
                  <input placeholder="مقطع تحصیلی (مثلاً: کارشناسی)" value={educationDraft.degreeLevel}
                    onChange={(e) => setEducationDraft((d) => ({ ...d, degreeLevel: e.target.value }))} />
                  <input placeholder="رشته تحصیلی" value={educationDraft.fieldOfStudy}
                    onChange={(e) => setEducationDraft((d) => ({ ...d, fieldOfStudy: e.target.value }))} />
                  <input placeholder="نام مؤسسه/دانشگاه" value={educationDraft.institutionName}
                    onChange={(e) => setEducationDraft((d) => ({ ...d, institutionName: e.target.value }))} />
                  <input placeholder="سال فارغ‌التحصیلی (اختیاری)" type="number" value={educationDraft.graduationYear}
                    onChange={(e) => setEducationDraft((d) => ({ ...d, graduationYear: e.target.value }))} />
                </div>
                {educationError && <span className="error-text">{educationError}</span>}
                <button type="button" className="btn-secondary" style={{ marginTop: '0.5rem' }} onClick={handleAddEducation} disabled={isAddingEducation}>
                  {isAddingEducation ? 'در حال افزودن...' : '+ افزودن سابقه تحصیلی'}
                </button>
              </div>
            </>
          )}

          {step === 3 && (
            <>
              <div className="field">
                <label>سوابق کاری</label>
                <textarea rows={5} placeholder="شرح مختصری از سوابق شغلی قبلی خود بنویسید..." {...register('workExperienceSummary')} />
              </div>

              <div style={{ marginTop: '1.25rem', paddingTop: '1rem', borderTop: '1px solid var(--color-border, #e5e7eb)' }}>
                <h3 style={{ margin: '0 0 0.5rem' }}>سوابق شغلی ساختاریافته (اختیاری، چند مورد)</h3>
                <p className="text-muted" style={{ fontSize: 'var(--fs-sm)', marginTop: 0 }}>
                  علاوه بر شرح آزاد بالا، می‌توانید هر تجربه شغلی را به‌صورت یک ردیف جداگانه با بازه زمانی ثبت کنید.
                  در صورت خالی گذاشتن «سال پایان»، به‌عنوان «اکنون» (همچنان مشغول به کار) نمایش داده می‌شود.
                </p>

                {resume?.workExperiences && resume.workExperiences.length > 0 && (
                  <ul style={{ listStyle: 'none', padding: 0, margin: '0 0 0.75rem' }}>
                    {resume.workExperiences.map((exp) => (
                      <li
                        key={exp.id}
                        style={{
                          display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                          padding: '0.5rem 0.75rem', borderRadius: 8, background: '#f9fafb', marginBottom: '0.4rem'
                        }}
                      >
                        <span>
                          <strong>{exp.jobTitle}</strong> — {exp.companyName} ({toPersianDigits(exp.startYear)} تا {exp.endYear ? toPersianDigits(exp.endYear) : 'اکنون'})
                        </span>
                        <button type="button" className="btn-secondary" onClick={() => deleteWorkExperience(exp.id)}>حذف</button>
                      </li>
                    ))}
                  </ul>
                )}

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem' }}>
                  <input placeholder="عنوان شغلی" value={workExperienceDraft.jobTitle}
                    onChange={(e) => setWorkExperienceDraft((d) => ({ ...d, jobTitle: e.target.value }))} />
                  <input placeholder="نام شرکت/کارفرما" value={workExperienceDraft.companyName}
                    onChange={(e) => setWorkExperienceDraft((d) => ({ ...d, companyName: e.target.value }))} />
                  <input placeholder="سال شروع" type="number" value={workExperienceDraft.startYear}
                    onChange={(e) => setWorkExperienceDraft((d) => ({ ...d, startYear: e.target.value }))} />
                  <input placeholder="سال پایان (اختیاری، خالی = اکنون)" type="number" value={workExperienceDraft.endYear}
                    onChange={(e) => setWorkExperienceDraft((d) => ({ ...d, endYear: e.target.value }))} />
                  <textarea
                    rows={2} placeholder="توضیحات (اختیاری)" style={{ gridColumn: '1 / -1' }}
                    value={workExperienceDraft.description}
                    onChange={(e) => setWorkExperienceDraft((d) => ({ ...d, description: e.target.value }))}
                  />
                </div>
                {workExperienceError && <span className="error-text">{workExperienceError}</span>}
                <button type="button" className="btn-secondary" style={{ marginTop: '0.5rem' }} onClick={handleAddWorkExperience} disabled={isAddingWorkExperience}>
                  {isAddingWorkExperience ? 'در حال افزودن...' : '+ افزودن سابقه شغلی'}
                </button>
              </div>
            </>
          )}

          {step === 4 && (
            <div className="field">
              <label>مهارت‌ها (مثلاً: CNC، تراشکاری، لیفتراک)</label>
              <input {...register('skills')} />
            </div>
          )}

          {step === 5 && (
            <div className="field">
              <label>علاقه‌مندی‌ها</label>
              <textarea rows={3} placeholder="فعالیت‌ها و زمینه‌های مورد علاقه خود را بنویسید..." {...register('interests')} />
            </div>
          )}

          {step === 6 && (
            <div>
              <p className="text-muted" style={{ marginTop: 0 }}>پاسخ به این سؤالات اختیاری است اما به کارفرمایان کمک می‌کند شما را بهتر بشناسند.</p>
              {PSYCHOLOGY_QUESTIONS.map((q) => (
                <div className="field" key={q.key}>
                  <label>{q.label}</label>
                  <textarea rows={2} {...register(q.key)} />
                </div>
              ))}
            </div>
          )}

          {step === 7 && (
            <div id="resume-print-area">
              <h2 style={{ marginTop: 0 }}>خلاصه رزومه</h2>
              <p><strong>نام و نام خانوادگی:</strong> {values.fullName || '—'}</p>
              <p><strong>ایمیل:</strong> {values.email || '—'}</p>
              <p><strong>شهر محل سکونت:</strong> {values.city || '—'}</p>
              <p><strong>آخرین مدرک تحصیلی:</strong> {values.educationLevel || '—'}</p>
              <p><strong>وضعیت نظام وظیفه:</strong> {militaryOptions[values.militaryServiceStatus ?? ''] ?? values.militaryServiceStatus}</p>
              {resume?.educations && resume.educations.length > 0 && (
                <div>
                  <strong>سوابق تحصیلی:</strong>
                  <ul>
                    {resume.educations.map((edu) => (
                      <li key={edu.id}>
                        {edu.degreeLevel} — {edu.fieldOfStudy}، {edu.institutionName}
                        {edu.graduationYear ? ` (${toPersianDigits(edu.graduationYear)})` : ''}
                      </li>
                    ))}
                  </ul>
                </div>
              )}
              <p><strong>سوابق کاری:</strong> {values.workExperienceSummary || '—'}</p>
              {resume?.workExperiences && resume.workExperiences.length > 0 && (
                <div>
                  <strong>سوابق شغلی:</strong>
                  <ul>
                    {resume.workExperiences.map((exp) => (
                      <li key={exp.id}>
                        {exp.jobTitle} — {exp.companyName} ({toPersianDigits(exp.startYear)} تا {exp.endYear ? toPersianDigits(exp.endYear) : 'اکنون'})
                      </li>
                    ))}
                  </ul>
                </div>
              )}
              <p><strong>مهارت‌ها:</strong> {values.skills || '—'}</p>
              <p><strong>علاقه‌مندی‌ها:</strong> {values.interests || '—'}</p>
              <div>
                <strong>پاسخ‌های روان‌شناسی عمومی:</strong>
                <ul>
                  {PSYCHOLOGY_QUESTIONS.map((q) => (
                    <li key={q.key} style={{ margin: '0.35rem 0' }}>
                      <em>{q.label}</em><br />
                      {values[q.key]?.trim() || '—'}
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          )}

          {serverError && <p className="error-text">{serverError}</p>}
          {successMessage && <p style={{ color: 'var(--color-success)' }}>{successMessage}</p>}

          <div style={{ display: 'flex', gap: '0.5rem', marginTop: '1rem', flexWrap: 'wrap' }}>
            {step > 1 && (
              <button type="button" className="btn-secondary" onClick={goPrev}>→ مرحله قبل</button>
            )}
            {step < TOTAL_STEPS && (
              <button type="button" className="btn-primary" onClick={goNext}>مرحله بعد ←</button>
            )}
            {step === TOTAL_STEPS && (
              <>
                <button type="submit" className="btn-primary" disabled={isSaving}>
                  {isSaving ? 'در حال ذخیره...' : 'ثبت نهایی رزومه'}
                </button>
                <button
                  type="button"
                  className="btn-secondary"
                  onClick={handleDownloadPdf}
                  disabled={!resume?.isFeePaid}
                  title={!resume?.isFeePaid ? 'برای دانلود، ابتدا رزومه را ذخیره و هزینه ساخت را پرداخت کنید.' : undefined}
                >
                  دانلود PDF
                </button>
              </>
            )}
          </div>
          {step === TOTAL_STEPS && !resume?.isFeePaid && (
            <p className="text-muted" style={{ fontSize: 'var(--fs-sm)', marginTop: '0.5rem' }}>
              دانلود رزومه پس از ثبت نهایی و پرداخت هزینه ساخت رزومه فعال می‌شود.
            </p>
          )}
        </form>
      </div>
    </div>
  );
}
