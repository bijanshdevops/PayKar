import { useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import {
  Award,
  Briefcase,
  Building2,
  Calendar,
  ChevronRight,
  CircleCheck,
  Clock,
  Code,
  Download,
  Globe,
  GraduationCap,
  Languages as LanguagesIcon,
  Laptop,
  Link as LinkIcon,
  Mail,
  MapPin,
  Pencil,
  Shuffle,
  Sparkles,
  Star,
  User,
  Wallet
} from 'lucide-react';
import { useGetMyCandidateProfileQuery, type Candidate, type CandidateSkillEntry } from '@/features/candidates/candidateApi';
import { PreferredWorkTypeLabels, LanguageProficiencyLevelLabels, LanguageProficiencyLevelRank } from '@/shared/enums';
import ResumeFeePaymentCard from '@/features/candidates/ResumeFeePaymentCard';

// طبق فاز «پروفایل و رزومه‌ساز کارجو» — نمای فقط-خواندنی کامل رزومه کارجو، مطابق karjoo-profile.png.
// طبق اصلاح محصولی بعدی: این صفحه اکنون به‌عنوان یکی از فرزندان DashboardLayout رندر می‌شود
// (هدر + سایدبار داشبورد کارجو در اطراف آن باقی می‌ماند)، نه به‌صورت بوم مستقل.
// نکته شفاف‌سازی «بدون داده جعلی»: مدرک ادعای «Verified»/نوع همکاری «تمام‌وقت/پاره‌وقت» در دامنه فعلی
// کارجو داده واقعی ندارد؛ بنابراین به‌جای آن از دو فیلد واقعی موجود استفاده شده: وضعیت پرداخت هزینه
// رزومه (IsFeePaid → «رزومه نهایی‌شده») و وضعیت جستجوی کار (IsActivelyLookingForJob). همچنین شماره
// تماس در این DTO (نمای خودِ کارجو) وجود ندارد؛ فقط ایمیل/لینکدین/گیت‌هاب/وب‌سایت نمایش داده می‌شود.

function toPersianDigits(n: number) {
  return n.toLocaleString('fa-IR');
}

function formatToman(amount: number) {
  return amount.toLocaleString('fa-IR');
}

function SkeletonBlock({ className = '' }: { className?: string }) {
  return <div className={`animate-pulse rounded-xl bg-slate-200/80 ${className}`} />;
}

function SectionCard({ title, icon, children }: { title: string; icon: ReactNode; children: ReactNode }) {
  return (
    <section className="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-sm">
      <h2 className="mb-4 flex items-center gap-2 text-base font-bold text-slate-800">
        <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-emerald-50 text-emerald-600">{icon}</span>
        {title}
      </h2>
      {children}
    </section>
  );
}

function EmptyHint({ text }: { text: string }) {
  return <p className="m-0 rounded-xl bg-slate-50 px-4 py-3 text-sm text-slate-500">{text}</p>;
}

function ResumeGauge({ percent }: { percent: number }) {
  const radius = 46;
  const circumference = 2 * Math.PI * radius;
  const clamped = Math.min(100, Math.max(0, percent));
  const offset = circumference * (1 - clamped / 100);
  const color = clamped >= 80 ? '#059669' : clamped >= 50 ? '#d97706' : '#f43f5e';

  return (
    <div className="relative flex h-32 w-32 items-center justify-center">
      <svg viewBox="0 0 100 100" className="h-32 w-32 -rotate-90">
        <circle cx="50" cy="50" r={radius} fill="none" stroke="#e5e7eb" strokeWidth="9" />
        <circle
          cx="50" cy="50" r={radius} fill="none" stroke={color} strokeWidth="9" strokeLinecap="round"
          strokeDasharray={circumference} strokeDashoffset={offset}
          style={{ transition: 'stroke-dashoffset 0.6s ease' }}
        />
      </svg>
      <div className="absolute flex flex-col items-center">
        <span className="text-2xl font-extrabold text-slate-800">{toPersianDigits(clamped)}٪</span>
        <span className="text-[11px] text-slate-500">تکمیل رزومه</span>
      </div>
    </div>
  );
}

function LanguageStars({ level }: { level: string }) {
  const rank = LanguageProficiencyLevelRank[level] ?? 0;
  return (
    <div className="flex items-center gap-0.5" title={LanguageProficiencyLevelLabels[level] ?? level}>
      {[1, 2, 3, 4, 5].map((i) => (
        <Star key={i} size={14} className={i <= rank ? 'fill-amber-400 text-amber-400' : 'text-slate-300'} />
      ))}
    </div>
  );
}

const WORK_TYPE_ICON: Record<string, ReactNode> = {
  OnSite: <Building2 size={16} />,
  Remote: <Laptop size={16} />,
  Hybrid: <Shuffle size={16} />
};

export default function CandidateProfileView() {
  const { data, isLoading } = useGetMyCandidateProfileQuery();
  const [showAllSuggestions, setShowAllSuggestions] = useState(false);

  const payload = data?.data;
  const resume: Candidate | undefined = payload?.profile;
  const completionPercent = payload?.resumeCompletionPercent ?? 0;
  const suggestions = payload?.suggestions ?? [];
  const visibleSuggestions = showAllSuggestions ? suggestions : suggestions.slice(0, 3);

  if (isLoading) {
    return (
      <div dir="rtl" className="font-vazir">
        <div className="grid gap-6 lg:grid-cols-[2fr_1fr]">
          <SkeletonBlock className="h-56" />
          <SkeletonBlock className="h-56" />
        </div>
        <div className="mt-6 grid gap-6 lg:grid-cols-3">
          <div className="space-y-6 lg:col-span-2">
            <SkeletonBlock className="h-32" />
            <SkeletonBlock className="h-64" />
            <SkeletonBlock className="h-48" />
          </div>
          <div className="space-y-6">
            <SkeletonBlock className="h-40" />
            <SkeletonBlock className="h-40" />
            <SkeletonBlock className="h-40" />
          </div>
        </div>
      </div>
    );
  }

  if (!resume) {
    return (
      <div dir="rtl" className="font-vazir mx-auto max-w-xl px-4 py-16 text-center">
        <div className="rounded-2xl border border-slate-200/80 bg-white p-8 shadow-sm">
          <Sparkles className="mx-auto mb-3 text-emerald-600" size={32} />
          <h1 className="m-0 mb-2 text-lg font-bold text-slate-800">هنوز رزومه‌ای نساخته‌اید</h1>
          <p className="m-0 mb-5 text-sm text-slate-500">
            برای مشاهده نمای حرفه‌ای پروفایل، ابتدا اطلاعات رزومه‌ساز خود را تکمیل کنید.
          </p>
          {/* طبق تصمیم صریح محصولی «قطع کامل وابستگی به رزومه‌ساز مرحله‌ای»: CandidateProfileEdit.tsx
              اکنون خودش می‌تواند اولین رزومه را بسازد (وضعیت نظام‌وظیفه/مدرک تحصیلی هم آنجاست). */}
          <Link
            to="/resume/edit"
            className="inline-flex items-center gap-2 rounded-xl bg-emerald-600 px-5 py-2.5 text-sm font-semibold text-white no-underline hover:bg-emerald-700"
          >
            <Pencil size={16} /> تکمیل رزومه‌ساز
          </Link>
        </div>
      </div>
    );
  }

  const educations = resume.educations ?? [];
  const workExperiences = [...(resume.workExperiences ?? [])].sort((a, b) => {
    if (a.endYear === null && b.endYear !== null) return -1;
    if (a.endYear !== null && b.endYear === null) return 1;
    return b.startYear - a.startYear;
  });
  const certifications = resume.certifications ?? [];
  const structuredSkills = resume.structuredSkills ?? [];
  const languages = resume.languages ?? [];

  const skillsByCategory = structuredSkills.reduce<Record<string, CandidateSkillEntry[]>>((acc, skill) => {
    (acc[skill.category] ??= []).push(skill);
    return acc;
  }, {});

  const hasSalaryRange = resume.minRequestedSalaryInToman != null || resume.maxRequestedSalaryInToman != null;
  const salaryText = !hasSalaryRange
    ? 'تعیین نشده'
    : resume.minRequestedSalaryInToman != null && resume.maxRequestedSalaryInToman != null
      ? `${formatToman(resume.minRequestedSalaryInToman)} تا ${formatToman(resume.maxRequestedSalaryInToman)} تومان`
      : resume.minRequestedSalaryInToman != null
        ? `از ${formatToman(resume.minRequestedSalaryInToman)} تومان`
        : `تا ${formatToman(resume.maxRequestedSalaryInToman!)} تومان`;

  const contactLinks: { icon: ReactNode; label: string; href: string }[] = [];
  if (resume.email) contactLinks.push({ icon: <Mail size={15} />, label: resume.email, href: `mailto:${resume.email}` });
  if (resume.linkedInUrl) contactLinks.push({ icon: <LinkIcon size={15} />, label: 'لینکدین', href: resume.linkedInUrl });
  if (resume.gitHubUrl) contactLinks.push({ icon: <Code size={15} />, label: 'گیت‌هاب', href: resume.gitHubUrl });
  if (resume.personalWebsiteUrl) contactLinks.push({ icon: <Globe size={15} />, label: 'وب‌سایت شخصی', href: resume.personalWebsiteUrl });

  return (
    // نکته: این صفحه اکنون داخل dash-content (که خودش padding دارد) رندر می‌شود، پس بدون
    // mx-auto/max-w/px/py خودش — تا فاصله‌گذاری دوبل و محدودیت عرض غیرلازم رخ ندهد.
    <div dir="rtl" className="font-vazir text-slate-800">
      <style>{`
        @media print {
          body * { visibility: hidden; }
          #candidate-resume-print, #candidate-resume-print * { visibility: visible; }
          #candidate-resume-print { position: absolute; inset: 0; padding: 1.5rem; }
        }
      `}</style>

      <div id="candidate-resume-print">
        {/* ---------- هدر: کارت هویت (راست) + گیج تکمیل رزومه و پیشنهادهای هوشمند (چپ) ---------- */}
        <div className="grid gap-6 lg:grid-cols-[2fr_1fr]">
          <div className="flex flex-col gap-4 rounded-2xl border border-slate-200/80 bg-white p-6 shadow-sm sm:flex-row sm:items-start">
            {resume.avatarUrl ? (
              <img src={resume.avatarUrl} alt={resume.fullName} className="h-20 w-20 shrink-0 rounded-full object-cover" />
            ) : (
              <span className="flex h-20 w-20 shrink-0 items-center justify-center rounded-full bg-emerald-50 text-emerald-600">
                <User size={32} />
              </span>
            )}

            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <h1 className="m-0 text-xl font-extrabold text-slate-900">{resume.fullName}</h1>
                {resume.isFeePaid && (
                  <span className="inline-flex items-center gap-1 rounded-full bg-sky-50 px-2.5 py-0.5 text-xs font-medium text-sky-700">
                    <CircleCheck size={13} /> رزومه نهایی‌شده
                  </span>
                )}
                <span className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium ${resume.isActivelyLookingForJob ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
                  <Clock size={13} /> {resume.isActivelyLookingForJob ? 'در جستجوی فرصت شغلی' : 'در حال حاضر جویای کار نیست'}
                </span>
              </div>

              {resume.jobTitle && <p className="m-0 mt-1 text-sm font-medium text-slate-600">{resume.jobTitle}</p>}
              {resume.city && (
                <p className="m-0 mt-1 flex items-center gap-1 text-xs text-slate-500">
                  <MapPin size={13} /> {resume.city}
                </p>
              )}

              {contactLinks.length > 0 && (
                <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1.5">
                  {contactLinks.map((link) => (
                    <a
                      key={link.label}
                      href={link.href}
                      target={link.href.startsWith('http') ? '_blank' : undefined}
                      rel="noreferrer"
                      className="inline-flex items-center gap-1 text-xs text-slate-600 no-underline hover:text-emerald-700"
                    >
                      {link.icon} {link.label}
                    </a>
                  ))}
                </div>
              )}

              <div className="mt-4 flex flex-wrap gap-2 print:hidden">
                <button
                  type="button"
                  onClick={() => resume.isFeePaid && window.print()}
                  disabled={!resume.isFeePaid}
                  title={!resume.isFeePaid ? 'برای دانلود، ابتدا رزومه را نهایی و هزینه آن را پرداخت کنید.' : undefined}
                  className="inline-flex items-center gap-1.5 rounded-xl border border-slate-200 bg-white px-4 py-2 text-xs font-semibold text-slate-700 disabled:cursor-not-allowed disabled:opacity-50 hover:bg-slate-50"
                >
                  <Download size={15} /> دانلود رزومه (PDF)
                </button>
                <Link
                  to="/resume/edit"
                  className="inline-flex items-center gap-1.5 rounded-xl bg-emerald-600 px-4 py-2 text-xs font-semibold text-white no-underline hover:bg-emerald-700"
                >
                  <Pencil size={15} /> ویرایش پروفایل
                </Link>
              </div>
            </div>
          </div>

          <div className="flex flex-col items-center gap-4 rounded-2xl border border-slate-200/80 bg-white p-5 shadow-sm">
            <ResumeGauge percent={completionPercent} />
            <div className="w-full">
              <h3 className="m-0 mb-2 flex items-center gap-1.5 text-sm font-bold text-slate-800">
                <Sparkles size={15} className="text-emerald-600" /> پیشنهادهای هوشمند
              </h3>
              {suggestions.length === 0 ? (
                <p className="m-0 text-xs text-slate-500">رزومه شما کامل است — پیشنهادی برای تکمیل وجود ندارد.</p>
              ) : (
                <>
                  <ul className="m-0 flex list-none flex-col gap-1.5 p-0">
                    {visibleSuggestions.map((s, i) => (
                      <li key={i} className="rounded-lg bg-amber-50 px-2.5 py-1.5 text-xs text-amber-800">{s}</li>
                    ))}
                  </ul>
                  {suggestions.length > 3 && (
                    <button
                      type="button"
                      onClick={() => setShowAllSuggestions((v) => !v)}
                      className="mt-2 inline-flex items-center gap-1 bg-transparent p-0 text-xs font-semibold text-emerald-700 hover:text-emerald-800"
                    >
                      {showAllSuggestions ? 'نمایش کمتر' : 'مشاهده همه'} <ChevronRight size={13} />
                    </button>
                  )}
                </>
              )}
            </div>
          </div>
        </div>

        {/* طبق تصمیم صریح محصولی: جریان پرداخت هزینه نهایی‌سازی رزومه اکنون مستقیماً همین‌جاست،
            نه پشت یک لینک به رزومه‌ساز مرحله‌ای قدیمی. */}
        {!resume.isFeePaid && (
          <div className="mt-6">
            <ResumeFeePaymentCard amountInRials={resume.resumeCreationFeeAmountInRials} />
          </div>
        )}

        {/* ---------- بدنه اصلی ---------- */}
        <div className="mt-6 grid gap-6 lg:grid-cols-3">
          <div className="space-y-6 lg:col-span-2">
            <SectionCard title="درباره من" icon={<User size={16} />}>
              {resume.professionalSummary ? (
                <p className="m-0 whitespace-pre-line text-sm leading-7 text-slate-600">{resume.professionalSummary}</p>
              ) : (
                <EmptyHint text="هنوز خلاصه‌ای درباره خود ننوشته‌اید." />
              )}
            </SectionCard>

            <SectionCard title="سوابق شغلی" icon={<Briefcase size={16} />}>
              {workExperiences.length === 0 ? (
                <EmptyHint text="هنوز سابقه شغلی ساختاریافته‌ای ثبت نشده است." />
              ) : (
                <ol className="m-0 flex list-none flex-col gap-5 border-e-2 border-slate-100 p-0 pe-5">
                  {workExperiences.map((exp) => (
                    <li key={exp.id} className="relative">
                      <span className="absolute -end-[1.6rem] top-1 h-2.5 w-2.5 rounded-full bg-emerald-500" />
                      <div className="flex flex-wrap items-center justify-between gap-1">
                        <strong className="text-sm font-bold text-slate-800">{exp.jobTitle}</strong>
                        <span className="flex items-center gap-1 text-xs text-slate-500">
                          <Calendar size={12} />
                          {toPersianDigits(exp.startYear)} تا {exp.endYear ? toPersianDigits(exp.endYear) : 'اکنون'}
                        </span>
                      </div>
                      <p className="m-0 mt-0.5 text-xs font-medium text-slate-500">{exp.companyName}</p>
                      {exp.description && <p className="m-0 mt-1.5 whitespace-pre-line text-xs leading-6 text-slate-600">{exp.description}</p>}
                    </li>
                  ))}
                </ol>
              )}
            </SectionCard>

            <SectionCard title="تحصیلات و مدارک" icon={<GraduationCap size={16} />}>
              <div className="grid gap-5 sm:grid-cols-2">
                <div>
                  <h3 className="m-0 mb-2 text-xs font-bold text-slate-500">تحصیلات</h3>
                  {educations.length === 0 ? (
                    <EmptyHint text="سابقه تحصیلی ساختاریافته‌ای ثبت نشده." />
                  ) : (
                    <ul className="m-0 flex list-none flex-col gap-2.5 p-0">
                      {educations.map((edu) => (
                        <li key={edu.id} className="text-sm">
                          <p className="m-0 font-semibold text-slate-800">{edu.degreeLevel} — {edu.fieldOfStudy}</p>
                          <p className="m-0 text-xs text-slate-500">
                            {edu.institutionName}{edu.graduationYear ? ` • ${toPersianDigits(edu.graduationYear)}` : ''}
                          </p>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
                <div>
                  <h3 className="m-0 mb-2 flex items-center gap-1 text-xs font-bold text-slate-500"><Award size={13} /> مدارک و گواهینامه‌ها</h3>
                  {certifications.length === 0 ? (
                    <EmptyHint text="هنوز مدرک/گواهینامه‌ای ثبت نشده." />
                  ) : (
                    <ul className="m-0 flex list-none flex-col gap-2.5 p-0">
                      {certifications.map((cert) => (
                        <li key={cert.id} className="text-sm">
                          <p className="m-0 font-semibold text-slate-800">{cert.title}</p>
                          <p className="m-0 text-xs text-slate-500">{cert.issuingOrganization} • {toPersianDigits(cert.yearObtained)}</p>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
              </div>
            </SectionCard>
          </div>

          <div className="space-y-6">
            <SectionCard title="مهارت‌ها" icon={<Code size={16} />}>
              {structuredSkills.length === 0 ? (
                resume.skills ? (
                  <p className="m-0 text-sm leading-6 text-slate-600">{resume.skills}</p>
                ) : (
                  <EmptyHint text="هنوز مهارتی ثبت نشده است." />
                )
              ) : (
                <div className="flex flex-col gap-3">
                  {Object.entries(skillsByCategory).map(([category, items]) => (
                    <div key={category}>
                      <h4 className="m-0 mb-1.5 text-xs font-bold text-slate-500">{category}</h4>
                      <div className="flex flex-wrap gap-1.5">
                        {items.map((skill) => (
                          <span key={skill.id} className="rounded-lg bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-700">
                            {skill.name} <span className="text-slate-400">• {toPersianDigits(skill.level)}/۵</span>
                          </span>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </SectionCard>

            <SectionCard title="زبان‌ها" icon={<LanguagesIcon size={16} />}>
              {languages.length === 0 ? (
                <EmptyHint text="هنوز زبانی ثبت نشده است." />
              ) : (
                <ul className="m-0 flex list-none flex-col gap-2.5 p-0">
                  {languages.map((lang) => (
                    <li key={lang.id} className="flex items-center justify-between text-sm">
                      <span className="font-medium text-slate-700">{lang.name}</span>
                      <LanguageStars level={lang.proficiencyLevel} />
                    </li>
                  ))}
                </ul>
              )}
            </SectionCard>

            <SectionCard title="ترجیحات شغلی" icon={<Wallet size={16} />}>
              <ul className="m-0 flex list-none flex-col gap-3 p-0 text-sm">
                <li className="flex items-center justify-between gap-2">
                  <span className="text-slate-500">نوع همکاری</span>
                  <span className="flex items-center gap-1.5 font-medium text-slate-800">
                    {resume.preferredWorkType ? (
                      <>
                        {WORK_TYPE_ICON[resume.preferredWorkType]}
                        {PreferredWorkTypeLabels[resume.preferredWorkType] ?? resume.preferredWorkType}
                      </>
                    ) : (
                      'تعیین نشده'
                    )}
                  </span>
                </li>
                <li className="flex items-center justify-between gap-2">
                  <span className="text-slate-500">محدوده حقوق درخواستی</span>
                  <span className="font-medium text-slate-800">{salaryText}</span>
                </li>
                <li className="flex items-center justify-between gap-2">
                  <span className="text-slate-500">وضعیت جستجوی کار</span>
                  <span className={`font-medium ${resume.isActivelyLookingForJob ? 'text-emerald-700' : 'text-slate-500'}`}>
                    {resume.isActivelyLookingForJob ? 'در حال جستجو' : 'جویای کار نیست'}
                  </span>
                </li>
              </ul>
            </SectionCard>
          </div>
        </div>
      </div>
    </div>
  );
}
