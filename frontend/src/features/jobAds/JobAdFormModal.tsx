import { useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { X } from 'lucide-react';
import { useCreateJobAdMutation, useUpdateJobAdMutation, type JobAd, type JobAdFormValues } from '@/features/jobAds/jobAdApi';
import PersianDatePicker from '@/shared/components/PersianDatePicker';
import {
  WorkShiftLabels,
  MealPlanLabels,
  InsuranceTypeLabels,
  ContractTypeLabels,
  GenderPreferenceLabels,
  EducationLevelLabels,
  MilitaryServiceStatusLabels
} from '@/shared/enums';

/**
 * فرم دومرحله‌ای ثبت/ویرایش آگهی — استخراج‌شده از CompanyJobAdsPage.tsx (تسک #92) به یک مودال
 * مشترک قابل استفاده مجدد، طبق فاز بازطراحی «آگهی‌های شرکت من» (CompanyJobListingsPage.tsx).
 * منطق و فیلدها بدون تغییر نسبت به نسخه اصلی حفظ شده‌اند؛ فقط پوسته بصری به Tailwind منتقل شده است.
 */

const insuranceOptions = Object.keys(InsuranceTypeLabels);

const inputClass =
  'w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-800 transition focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20';
const labelClass = 'mb-1.5 block text-xs font-semibold text-slate-600';

const emptyDefaults: JobAdFormValues = {
  title: '',
  description: '',
  workShift: 'MorningOnly',
  hasCommuteService: false,
  mealPlan: 'None',
  insuranceTypes: [],
  salaryRangeType: 'MinistryOfLabor',
  contractType: 'Permanent'
};

function toFormValues(ad: JobAd): JobAdFormValues {
  return {
    title: ad.title,
    description: ad.description,
    workShift: ad.workShift,
    hasCommuteService: ad.hasCommuteService,
    commuteServiceRoutes: ad.commuteServiceRoutes ?? undefined,
    mealPlan: ad.mealPlan,
    insuranceTypes: ad.insuranceTypes,
    salaryRangeType: ad.salaryRange.type,
    fixedAmount: ad.salaryRange.fixedAmount ?? undefined,
    minAmount: ad.salaryRange.minAmount ?? undefined,
    maxAmount: ad.salaryRange.maxAmount ?? undefined,
    contractType: ad.contractType,
    genderPreference: ad.genderPreference,
    minAge: ad.minAge ?? undefined,
    maxAge: ad.maxAge ?? undefined,
    minEducationLevel: ad.minEducationLevel,
    minExperienceYears: ad.minExperienceYears ?? undefined,
    militaryServiceStatus: ad.militaryServiceStatus ?? undefined,
    headcountNeeded: ad.headcountNeeded ?? undefined,
    applicationDeadlineUtc: ad.applicationDeadlineUtc ?? undefined,
    requiredSkills: ad.requiredSkills ?? undefined,
    additionalBenefits: ad.additionalBenefits ?? undefined
  };
}

export default function JobAdFormModal({
  editingAd,
  onClose,
  onSuccess
}: {
  /** null یعنی حالت ایجاد آگهی جدید؛ در غیر این صورت حالت ویرایش همین آگهی است. */
  editingAd: JobAd | null;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [createJobAd, { isLoading: isCreating }] = useCreateJobAdMutation();
  const [updateJobAd, { isLoading: isUpdating }] = useUpdateJobAdMutation();
  const [step, setStep] = useState<1 | 2>(1);
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    watch,
    trigger,
    control,
    formState: { errors }
  } = useForm<JobAdFormValues>({
    defaultValues: editingAd ? toFormValues(editingAd) : emptyDefaults
  });

  const STEP_1_FIELDS = ['title', 'description', 'workShift', 'contractType', 'mealPlan', 'salaryRangeType'] as const;
  const salaryRangeType = watch('salaryRangeType');

  const goToStep2 = async () => {
    const isValid = await trigger([...STEP_1_FIELDS]);
    if (isValid) setStep(2);
  };

  /**
   * این تابع دیگر هرگز از طریق submit طبیعی مرورگر (که علت اصلی باگ «کلیک روی مرحله بعد، آگهی
   * ناقص می‌سازد» بود) فراخوانی نمی‌شود — فقط و فقط با کلیک صریح روی دکمهٔ نهایی «ثبت آگهی» در
   * مرحله ۲، از طریق handleSubmit(onSubmit)() به‌صورت مستقیم در onClick صدا زده می‌شود (نگاه کنید
   * به <form onSubmit> پایین‌تر که عمداً preventDefault-only شده). شرط step===1 هم به‌عنوان یک
   * لایهٔ دفاعی اضافه، برای اطمینان کامل، نگه داشته شده است.
   */
  const onSubmit = async (values: JobAdFormValues) => {
    if (step === 1) {
      await goToStep2();
      return;
    }

    setServerError(null);
    const result = editingAd ? await updateJobAd({ id: editingAd.id, body: values }) : await createJobAd(values);

    if ('data' in result && result.data?.success) {
      onSuccess();
      return;
    }

    const errorResponse = 'error' in result ? (result.error as { data?: { message?: string } }) : undefined;
    setServerError(errorResponse?.data?.message ?? 'ثبت آگهی با خطا مواجه شد.');
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm" onClick={onClose}>
      <div
        dir="rtl"
        onClick={(e) => e.stopPropagation()}
        className="flex max-h-[90vh] w-full max-w-2xl flex-col overflow-hidden rounded-2xl bg-white shadow-xl"
      >
        <div className="flex shrink-0 items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <h2 className="text-base font-bold text-slate-800">{editingAd ? 'ویرایش آگهی' : 'ثبت آگهی استخدام جدید'}</h2>
            <p className="mt-0.5 text-xs text-slate-400">مرحله {step === 1 ? '۱' : '۲'} از ۲ — {step === 1 ? 'اطلاعات پایه' : 'تنظیمات تکمیلی'}</p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="flex h-8 w-8 items-center justify-center rounded-lg text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
            aria-label="بستن"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        <form
          onSubmit={(e) => {
            // ریشهٔ اصلی باگ «کلیک روی مرحله بعد، آگهی ناقص می‌سازد و مودال بسته می‌شود» این بود
            // که هر دو مرحله در یک <form> واحد هستند و رویداد submit طبیعی مرورگر (مثلاً با
            // Enter در یک اینپوت متنی، یا هر رفتار غیرمنتظرهٔ دیگر مرورگر/افزونه) مستقیماً به
            // handleSubmit(onSubmit) می‌رسید و همان لحظه به API ارسال می‌شد. راه‌حل قطعی: فرم
            // دیگر هیچ‌وقت از طریق submit طبیعی ارسال نمی‌شود؛ همیشه preventDefault می‌شود و ارسال
            // واقعی فقط با کلیک صریح روی دکمهٔ «ثبت آگهی» در مرحله ۲ (پایین‌تر، onClick مستقیم)
            // انجام می‌گیرد.
            e.preventDefault();
            if (step === 1) void goToStep2();
          }}
          onKeyDown={(e) => {
            // در مرحله ۱، Enter داخل اینپوت‌های تک‌خطی (به‌جز textarea) به‌جای submit، به مرحله بعد می‌رود.
            if (e.key === 'Enter' && step === 1 && (e.target as HTMLElement).tagName !== 'TEXTAREA') {
              e.preventDefault();
              void goToStep2();
            }
          }}
          className="flex flex-1 flex-col overflow-hidden"
        >
          <div className="flex-1 space-y-4 overflow-y-auto px-6 py-5">
            {step === 1 && (
              <>
                <div>
                  <label className={labelClass}>عنوان شغلی *</label>
                  <input className={inputClass} {...register('title', { required: 'عنوان الزامی است.' })} />
                  {errors.title && <span className="mt-1 block text-xs text-rose-600">{errors.title.message}</span>}
                </div>

                <div>
                  <label className={labelClass}>شرح آگهی *</label>
                  <textarea rows={4} className={inputClass} {...register('description', { required: 'شرح آگهی الزامی است.' })} />
                  {errors.description && <span className="mt-1 block text-xs text-rose-600">{errors.description.message}</span>}
                </div>

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                  <div>
                    <label className={labelClass}>شیفت کاری *</label>
                    <select className={inputClass} {...register('workShift', { required: true })}>
                      {Object.entries(WorkShiftLabels).map(([value, label]) => (
                        <option key={value} value={value}>{label}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className={labelClass}>نوع قرارداد *</label>
                    <select className={inputClass} {...register('contractType', { required: true })}>
                      {Object.entries(ContractTypeLabels).map(([value, label]) => (
                        <option key={value} value={value}>{label}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className={labelClass}>وعده غذایی *</label>
                    <select className={inputClass} {...register('mealPlan', { required: true })}>
                      {Object.entries(MealPlanLabels).map(([value, label]) => (
                        <option key={value} value={value}>{label}</option>
                      ))}
                    </select>
                  </div>
                </div>

                <label className="flex items-center gap-2 text-sm text-slate-600">
                  <input type="checkbox" className="h-4 w-4 rounded border-slate-300 text-emerald-600 focus:ring-emerald-500" {...register('hasCommuteService')} />
                  دارای سرویس ایاب‌وذهاب
                </label>

                <div>
                  <label className={labelClass}>مسیرهای سرویس (اختیاری)</label>
                  <input className={inputClass} {...register('commuteServiceRoutes')} placeholder="مثلاً: میدان آزادی -> کرج -> شهرک صنعتی" />
                </div>

                <div>
                  <label className={labelClass}>نوع بیمه (چندانتخابی، اختیاری)</label>
                  <div className="flex flex-wrap gap-3">
                    {insuranceOptions.map((option) => (
                      <label key={option} className="flex items-center gap-1.5 text-xs text-slate-600">
                        <input type="checkbox" value={option} className="h-3.5 w-3.5 rounded border-slate-300 text-emerald-600 focus:ring-emerald-500" {...register('insuranceTypes')} />
                        {InsuranceTypeLabels[option]}
                      </label>
                    ))}
                  </div>
                </div>

                <div>
                  <label className={labelClass}>نوع بازه حقوق *</label>
                  <select className={inputClass} {...register('salaryRangeType', { required: true })}>
                    <option value="MinistryOfLabor">طبق قانون کار</option>
                    <option value="FixedAmount">مبلغ ثابت</option>
                    <option value="Range">بازه کف و سقف</option>
                    <option value="Agreement">توافقی</option>
                  </select>
                </div>

                {salaryRangeType === 'FixedAmount' && (
                  <div>
                    <label className={labelClass}>مبلغ ثابت (تومان)</label>
                    <input type="number" className={inputClass} {...register('fixedAmount', { valueAsNumber: true })} />
                  </div>
                )}

                {salaryRangeType === 'Range' && (
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className={labelClass}>کف حقوق (تومان)</label>
                      <input type="number" className={inputClass} {...register('minAmount', { valueAsNumber: true })} />
                    </div>
                    <div>
                      <label className={labelClass}>سقف حقوق (تومان)</label>
                      <input type="number" className={inputClass} {...register('maxAmount', { valueAsNumber: true })} />
                    </div>
                  </div>
                )}
              </>
            )}

            {step === 2 && (
              <>
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div>
                    <label className={labelClass}>ترجیح جنسیت</label>
                    <select className={inputClass} {...register('genderPreference')}>
                      {Object.entries(GenderPreferenceLabels).map(([value, label]) => (
                        <option key={value} value={value}>{label}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className={labelClass}>حداقل مدرک تحصیلی</label>
                    <select className={inputClass} {...register('minEducationLevel')}>
                      {Object.entries(EducationLevelLabels).map(([value, label]) => (
                        <option key={value} value={value}>{label}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className={labelClass}>وضعیت خدمت سربازی</label>
                    <select className={inputClass} {...register('militaryServiceStatus')}>
                      <option value="">فرقی ندارد</option>
                      {Object.entries(MilitaryServiceStatusLabels).map(([value, label]) => (
                        <option key={value} value={value}>{label}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className={labelClass}>حداقل سن</label>
                    <input type="number" className={inputClass} {...register('minAge', { valueAsNumber: true })} />
                  </div>
                  <div>
                    <label className={labelClass}>حداکثر سن</label>
                    <input type="number" className={inputClass} {...register('maxAge', { valueAsNumber: true })} />
                  </div>
                  <div>
                    <label className={labelClass}>حداقل سابقه کار (سال)</label>
                    <input type="number" className={inputClass} {...register('minExperienceYears', { valueAsNumber: true })} />
                  </div>
                  <div>
                    <label className={labelClass}>تعداد نیروی موردنیاز</label>
                    <input type="number" className={inputClass} {...register('headcountNeeded', { valueAsNumber: true })} />
                  </div>
                  <div>
                    <label className={labelClass}>مهلت ارسال رزومه</label>
                    <Controller
                      name="applicationDeadlineUtc"
                      control={control}
                      render={({ field }) => (
                        <PersianDatePicker
                          value={field.value ?? null}
                          onChange={(iso) => field.onChange(iso ?? undefined)}
                          placeholder="انتخاب مهلت ارسال رزومه"
                        />
                      )}
                    />
                  </div>
                </div>

                <div>
                  <label className={labelClass}>مهارت‌های موردنیاز</label>
                  <textarea rows={2} className={inputClass} {...register('requiredSkills')} placeholder="مثلاً: آشنایی با اتوکد، دارای گواهینامه ایمنی و..." />
                </div>

                <div>
                  <label className={labelClass}>مزایای اضافی</label>
                  <textarea rows={2} className={inputClass} {...register('additionalBenefits')} placeholder="مثلاً: وام قرض‌الحسنه، پاداش سالانه، خوابگاه و..." />
                </div>

                {serverError && <p className="text-xs font-medium text-rose-600">{serverError}</p>}
              </>
            )}
          </div>

          <div className="flex shrink-0 items-center justify-between gap-2 border-t border-slate-100 px-6 py-4">
            {step === 2 ? (
              <button
                type="button"
                onClick={() => setStep(1)}
                className="rounded-xl border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-600 transition-colors hover:bg-slate-50"
              >
                → مرحله قبل
              </button>
            ) : (
              <button
                type="button"
                onClick={onClose}
                className="rounded-xl border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-600 transition-colors hover:bg-slate-50"
              >
                انصراف
              </button>
            )}

            {step === 1 ? (
              <button
                type="button"
                onClick={goToStep2}
                className="rounded-xl bg-emerald-600 px-5 py-2 text-sm font-bold text-white transition-colors hover:bg-emerald-700"
              >
                مرحله بعد ←
              </button>
            ) : (
              <button
                type="button"
                onClick={handleSubmit(onSubmit)}
                disabled={isCreating || isUpdating}
                className="rounded-xl bg-emerald-600 px-5 py-2 text-sm font-bold text-white transition-colors hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {editingAd ? 'ذخیره تغییرات' : 'ثبت آگهی (پیش‌نویس)'}
              </button>
            )}
          </div>
        </form>
      </div>
    </div>
  );
}
