import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useGetJobAdByIdQuery, useRecordJobAdViewMutation, useSetJobAdFeaturedMutation } from '@/features/jobAds/jobAdApi';
import { useAppSelector } from '@/app/hooks';
import {
  WorkShiftLabels,
  MealPlanLabels,
  InsuranceTypeLabels,
  SalaryRangeTypeLabels,
  ContractTypeLabels,
  GenderPreferenceLabels,
  EducationLevelLabels,
  MilitaryServiceStatusLabels
} from '@/shared/enums';
import { useSubmitApplicationMutation, useSubmitDirectApplicationMutation } from '@/features/applications/applicationApi';
import { useGetMyResumeQuery } from '@/features/candidates/candidateApi';
import PersianDate from '@/shared/components/PersianDate';

export default function JobAdDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data, isLoading } = useGetJobAdByIdQuery(id ?? '', { skip: !id });
  const [recordView] = useRecordJobAdViewMutation();

  // ثبت یک بازدید واقعی — یک‌بار در mount شدن صفحه، مستقل از کش RTK Query صفحه جزئیات
  // (Command مجزا طبق تصمیم صریح محصولی؛ خطای احتمالی این فراخوانی نباید تجربه کاربر را مختل کند).
  useEffect(() => {
    if (id) {
      recordView(id).catch(() => undefined);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const user = useAppSelector((state) => state.auth.user);
  const isAdmin = Boolean(user?.roles.includes('Admin'));
  const [setFeatured, { isLoading: isTogglingFeatured }] = useSetJobAdFeaturedMutation();
  const isCandidate = Boolean(user?.roles.includes('Candidate'));
  const { data: resumeData, isFetching: isResumeLoading } = useGetMyResumeQuery(undefined, { skip: !isCandidate });
  const [submitApplication, { isLoading: isApplying }] = useSubmitApplicationMutation();
  const [submitDirectApplication, { isLoading: isSubmittingDirect }] = useSubmitDirectApplicationMutation();
  const [trackingToken, setTrackingToken] = useState<string | null>(null);
  const [applyError, setApplyError] = useState<string | null>(null);
  const [showDirectForm, setShowDirectForm] = useState(false);
  const [directFullName, setDirectFullName] = useState('');
  const [directFile, setDirectFile] = useState<File | null>(null);
  // طبق ADR-011 — دکمه «تماس با کارفرما» فقط شماره تماس شرکت را نمایش می‌دهد (بدون چت).
  const [showContactNumber, setShowContactNumber] = useState(false);

  const jobAd = data?.data;
  const hasResume = Boolean(resumeData?.success && resumeData.data);

  if (isLoading) return <div className="container">در حال بارگذاری...</div>;
  if (!jobAd) return <div className="container">آگهی یافت نشد.</div>;

  const salary = jobAd.salaryRange;
  const salaryText =
    salary.type === 'FixedAmount'
      ? `${salary.fixedAmount?.toLocaleString('fa-IR')} تومان`
      : salary.type === 'Range'
        ? `${salary.minAmount?.toLocaleString('fa-IR')} تا ${salary.maxAmount?.toLocaleString('fa-IR')} تومان`
        : SalaryRangeTypeLabels[salary.type] ?? salary.type;

  const handleApply = async () => {
    setApplyError(null);
    setTrackingToken(null);
    const result = await submitApplication(jobAd.id);

    if ('data' in result && result.data?.success && result.data.data) {
      setTrackingToken(result.data.data.trackingToken);
      return;
    }

    const errorResponse = 'error' in result ? (result.error as { data?: { message?: string } }) : undefined;
    setApplyError(errorResponse?.data?.message ?? 'ثبت درخواست با خطا مواجه شد.');
  };

  const handleDirectSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setApplyError(null);
    setTrackingToken(null);

    if (!directFile || !directFullName.trim()) {
      setApplyError('نام و نام خانوادگی و فایل رزومه الزامی است.');
      return;
    }

    const result = await submitDirectApplication({ jobAdId: jobAd.id, fullName: directFullName.trim(), file: directFile });

    if ('data' in result && result.data?.success && result.data.data) {
      setTrackingToken(result.data.data.trackingToken);
      setShowDirectForm(false);
      return;
    }

    const errorResponse = 'error' in result ? (result.error as { data?: { message?: string } }) : undefined;
    setApplyError(errorResponse?.data?.message ?? 'ثبت درخواست با خطا مواجه شد.');
  };

  const requirementItems: { label: string; value: string }[] = [];
  if (jobAd.genderPreference && jobAd.genderPreference !== 'Any') {
    requirementItems.push({ label: 'جنسیت موردنیاز', value: GenderPreferenceLabels[jobAd.genderPreference] ?? jobAd.genderPreference });
  }
  if (jobAd.minAge || jobAd.maxAge) {
    requirementItems.push({ label: 'محدوده سنی', value: `${jobAd.minAge ?? '—'} تا ${jobAd.maxAge ?? '—'} سال` });
  }
  if (jobAd.minEducationLevel && jobAd.minEducationLevel !== 'Unspecified') {
    requirementItems.push({ label: 'حداقل مدرک تحصیلی', value: EducationLevelLabels[jobAd.minEducationLevel] ?? jobAd.minEducationLevel });
  }
  if (typeof jobAd.minExperienceYears === 'number') {
    requirementItems.push({ label: 'حداقل سابقه کار', value: `${jobAd.minExperienceYears} سال` });
  }
  if (jobAd.militaryServiceStatus) {
    requirementItems.push({ label: 'وضعیت سربازی', value: MilitaryServiceStatusLabels[jobAd.militaryServiceStatus] ?? jobAd.militaryServiceStatus });
  }
  if (jobAd.requiredSkills) {
    requirementItems.push({ label: 'مهارت‌های موردنیاز', value: jobAd.requiredSkills });
  }

  return (
    <div className="container" style={{ maxWidth: 1080, margin: '2rem auto' }}>
      <motion.div
        className="jobad-hero"
        initial={{ opacity: 0, y: -16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4 }}
      >
        <span className="jobad-hero__company">🏭 {jobAd.companyName}</span>
        <h1>{jobAd.title}</h1>
        <div className="jobad-hero__chips">
          <span className="jobad-chip">{ContractTypeLabels[jobAd.contractType] ?? jobAd.contractType}</span>
          <span className="jobad-chip">{WorkShiftLabels[jobAd.workShift] ?? jobAd.workShift}</span>
          <span className="jobad-chip">{salaryText}</span>
          {typeof jobAd.headcountNeeded === 'number' && (
            <span className="jobad-chip">نیاز به {jobAd.headcountNeeded.toLocaleString('fa-IR')} نفر</span>
          )}
          {jobAd.isFeatured && <span className="jobad-chip" style={{ background: '#0ea5e9', color: '#fff' }}>ویژه ★</span>}
        </div>

        {/* پنل ادمین — فلگ ساده «ویژه»، بدون هیچ پلن پرداختی/Boost (طبق تصمیم صریح محصولی این فاز). */}
        {isAdmin && (
          <button
            type="button"
            className="btn-secondary"
            style={{ marginTop: '0.75rem' }}
            disabled={isTogglingFeatured}
            onClick={() => setFeatured({ id: jobAd.id, isFeatured: !jobAd.isFeatured })}
          >
            {jobAd.isFeatured ? 'حذف از حالت ویژه' : 'علامت‌گذاری به‌عنوان ویژه'}
          </button>
        )}
      </motion.div>

      <div className="jobad-layout">
        <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.4, delay: 0.1 }}>
          <div className="card" style={{ marginBottom: 'var(--space-4)' }}>
            <h2 style={{ marginTop: 0 }}>شرح موقعیت شغلی</h2>
            <p style={{ whiteSpace: 'pre-wrap' }}>{jobAd.description}</p>
          </div>

          <div className="card" style={{ marginBottom: 'var(--space-4)' }}>
            <h2 style={{ marginTop: 0 }}>شرایط رفاهی و محل کار</h2>
            <div className="jobad-requirements">
              <div className="jobad-req-card">
                <div className="jobad-req-card__label">وعده غذایی</div>
                <div className="jobad-req-card__value">{MealPlanLabels[jobAd.mealPlan] ?? jobAd.mealPlan}</div>
              </div>
              <div className="jobad-req-card">
                <div className="jobad-req-card__label">سرویس ایاب‌وذهاب</div>
                <div className="jobad-req-card__value">{jobAd.hasCommuteService ? (jobAd.commuteServiceRoutes || 'دارد') : 'ندارد'}</div>
              </div>
              <div className="jobad-req-card">
                <div className="jobad-req-card__label">بیمه</div>
                <div className="jobad-req-card__value">{jobAd.insuranceTypes.map((t) => InsuranceTypeLabels[t] ?? t).join('، ') || 'ذکر نشده'}</div>
              </div>
              {jobAd.additionalBenefits && (
                <div className="jobad-req-card">
                  <div className="jobad-req-card__label">مزایای اضافی</div>
                  <div className="jobad-req-card__value">{jobAd.additionalBenefits}</div>
                </div>
              )}
            </div>
          </div>

          {requirementItems.length > 0 && (
            <div className="card" style={{ marginBottom: 'var(--space-4)' }}>
              <h2 style={{ marginTop: 0 }}>شرایط احراز</h2>
              <div className="jobad-requirements">
                {requirementItems.map((item) => (
                  <div className="jobad-req-card" key={item.label}>
                    <div className="jobad-req-card__label">{item.label}</div>
                    <div className="jobad-req-card__value">{item.value}</div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {jobAd.status !== 'Published' && (
            <p className="error-text">این آگهی در حال حاضر برای ارسال درخواست فعال نیست.</p>
          )}

          {jobAd.status === 'Published' && (
            <div className="card">
              <h2 style={{ marginTop: 0 }}>ارسال درخواست همکاری</h2>

              {!user && <p style={{ color: 'var(--color-muted)' }}>برای ارسال درخواست ابتدا وارد شوید.</p>}

              {isCandidate && isResumeLoading && <p>در حال بررسی وضعیت رزومه شما...</p>}

              {isCandidate && !isResumeLoading && hasResume && (
                <button className="btn-primary" disabled={isApplying} onClick={handleApply}>
                  {isApplying ? 'در حال ارسال...' : 'ارسال درخواست همکاری'}
                </button>
              )}

              {isCandidate && !isResumeLoading && !hasResume && !showDirectForm && (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                  <div
                    style={{
                      border: '1px solid var(--color-primary, #2563eb)',
                      background: 'rgba(37, 99, 235, 0.06)',
                      borderRadius: 'var(--radius-md, 0.75rem)',
                      padding: '0.9rem 1rem'
                    }}
                  >
                    <p style={{ margin: '0 0 0.5rem', fontWeight: 700 }}>پیشنهاد ما: از رزومه‌ساز حرفه‌ای استفاده کنید</p>
                    <p style={{ margin: '0 0 0.75rem', color: 'var(--color-muted)' }}>
                      با تکمیل پروفایل، یک رزومه کامل و حرفه‌ای می‌سازید که برای همه آگهی‌های آینده هم قابل استفاده است.
                    </p>
                    <Link to="/resume/edit" className="btn-primary">تکمیل رزومه‌ساز حرفه‌ای</Link>
                  </div>
                  <button
                    className="btn-secondary"
                    style={{ alignSelf: 'flex-start' }}
                    onClick={() => setShowDirectForm(true)}
                  >
                    یا ارسال مستقیم با فایل رزومه
                  </button>
                </div>
              )}

              {isCandidate && showDirectForm && (
                <form onSubmit={handleDirectSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', marginTop: '0.5rem' }}>
                  <div className="field">
                    <label>نام و نام خانوادگی</label>
                    <input value={directFullName} onChange={(e) => setDirectFullName(e.target.value)} required />
                  </div>
                  <div className="field">
                    <label>فایل رزومه (PDF یا Word)</label>
                    <input
                      type="file"
                      accept=".pdf,.doc,.docx"
                      onChange={(e) => setDirectFile(e.target.files?.[0] ?? null)}
                      required
                    />
                  </div>
                  <div style={{ display: 'flex', gap: '0.5rem' }}>
                    <button type="submit" className="btn-primary" disabled={isSubmittingDirect}>
                      {isSubmittingDirect ? 'در حال ارسال...' : 'ارسال درخواست'}
                    </button>
                    <button type="button" className="btn-secondary" onClick={() => setShowDirectForm(false)}>انصراف</button>
                  </div>
                </form>
              )}

              {applyError && <p className="error-text">{applyError}</p>}

              {trackingToken && (
                <div className="card" style={{ marginTop: '1rem', background: '#f0fdf4' }}>
                  <p style={{ margin: 0 }}>درخواست شما ثبت شد. کد پیگیری خود را نگه دارید:</p>
                  <strong style={{ fontSize: '1.25rem' }}>{trackingToken}</strong>
                </div>
              )}
            </div>
          )}
        </motion.div>

        <motion.aside
          className="jobad-sidebar"
          initial={{ opacity: 0, y: 12 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.4, delay: 0.2 }}
        >
          <div className="card">
            <h3 style={{ marginTop: 0 }}>خلاصه آگهی</h3>
            <div className="jobad-fact"><span className="text-muted">حقوق</span><strong>{salaryText}</strong></div>
            <div className="jobad-fact"><span className="text-muted">نوع قرارداد</span><strong>{ContractTypeLabels[jobAd.contractType] ?? jobAd.contractType}</strong></div>
            <div className="jobad-fact"><span className="text-muted">شیفت کاری</span><strong>{WorkShiftLabels[jobAd.workShift] ?? jobAd.workShift}</strong></div>
            {typeof jobAd.headcountNeeded === 'number' && (
              <div className="jobad-fact"><span className="text-muted">تعداد نیرو</span><strong>{jobAd.headcountNeeded.toLocaleString('fa-IR')} نفر</strong></div>
            )}
            {jobAd.applicationDeadlineUtc && (
              <div className="jobad-fact"><span className="text-muted">مهلت ارسال رزومه</span><strong><PersianDate date={jobAd.applicationDeadlineUtc} /></strong></div>
            )}
          </div>

          <div className="card">
            <h3 style={{ marginTop: 0 }}>تماس با کارفرما</h3>
            <p className="text-muted" style={{ marginTop: 0, fontSize: 'var(--fs-sm)' }}>
              شماره تماس شرکت «{jobAd.companyName}» را مشاهده کنید. طبق سیاست پلتفرم، امکان چت مستقیم در حال حاضر ارائه نمی‌شود.
            </p>

            {jobAd.companyContactPhoneNumber ? (
              !showContactNumber ? (
                <button type="button" className="btn-primary" style={{ width: '100%' }} onClick={() => setShowContactNumber(true)}>
                  📞 نمایش شماره تماس کارفرما
                </button>
              ) : (
                <a className="jobad-contact-number" href={`tel:${jobAd.companyContactPhoneNumber}`}>
                  {jobAd.companyContactPhoneNumber}
                </a>
              )
            ) : (
              <p className="text-muted" style={{ fontSize: 'var(--fs-sm)' }}>این شرکت هنوز شماره تماس عمومی ثبت نکرده است.</p>
            )}
          </div>
        </motion.aside>
      </div>
    </div>
  );
}
