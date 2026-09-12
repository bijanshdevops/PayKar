# 07. نقشه‌راه و نقاط عطف پروژه (Project Roadmap & Milestones)

| مشخصه | مقدار |
| :--- | :--- |
| **سند** | نقشه‌راه پلتفرم آگهی‌های صنعتی (`IndustrialPlatform`) |
| **ورژن** | 2.0.0 (بازنویسی کامل — نسخه قبلی مربوط به پروژه‌ای دیگر با موضوع Machine/Telemetry بود) |

> این نقشه‌راه مستقیماً از «دامنه عملکردی سیستم» در سند `00-Project-Overview.md` استخراج شده و مبنای `Tasks.md` هر سه ایجنت (Backend/Frontend/QA) است.

---

## فاز ۰: فونداسیون و زیرساخت (هفته ۱-۲)
- [ ] اسکلت Solution با ۷ پروژه طبق سند ۰۱-Architecture (`Shared`, `Domain`, `Application`, `Identity`, `Persistence`, `Infrastructure`, `Api`)
- [ ] پیاده‌سازی `Result`, `Result<T>`, `Error`, `BaseEntity`, `AggregateRoot`, `ISoftDeletable`, `IAuditableEntity` در `Shared`
- [ ] `AppDbContext` اولیه + `SaveChangesInterceptor` برای ستون‌های Audit
- [ ] Docker Compose محلی (Api + PostgreSQL 16 + Redis 7)
- [ ] اندپوینت‌های `/health/live` و `/health/ready`
- [ ] اسکلت پروژه فرانت‌اند (React + Vite + TypeScript + Redux Toolkit)
- [ ] راه‌اندازی CI پایه (Build + Unit Test + Lint)

## فاز ۱: احراز هویت و شهرک‌های صنعتی (هفته ۳-۴)
- [ ] ماژول Identity: ورود/ثبت‌نام با OTP (ملی‌پیامک)، JWT Access/Refresh، RBAC سه‌نقشی (Admin, CompanyManager, Candidate)
- [ ] موجودیت‌های پایه جغرافیایی: Province, City, IndustrialZone, ZoneType + Seed Data
- [ ] صفحات فرانت‌اند: ورود با موبایل/OTP، انتخاب شهرک صنعتی

## فاز ۲: پروفایل شرکت‌ها (هفته ۵-۶)
- [ ] Use Caseهای Company: ثبت، ویرایش، آپلود لوگو/گالری، درخواست احراز هویت
- [ ] چرخه Verification توسط Admin (`PendingVerification → Verified/Rejected/Suspended`)
- [ ] پنل ادمین برای بررسی و تایید شرکت‌ها (فرانت‌اند)
- [ ] پنل شرکت (Dashboard) برای مدیریت پروفایل

## فاز ۳: آگهی‌های استخدام (هفته ۷-۹)
- [ ] Use Caseهای JobAd: ایجاد، ویرایش، انتشار، انقضا، بستن، آرشیو
- [ ] فیلدهای تخصصی صنعتی: WorkShift, CommuteService/ServiceRoutes, MealPlan, InsuranceType, SalaryRange
- [ ] API جست‌وجو و فیلتر پیشرفته (شهرک، زون، شیفت، سرویس ایاب‌وذهاب) با `pg_trgm`/GIN برای جست‌وجوی متنی فارسی
- [ ] صفحات فرانت‌اند: لیست آگهی‌های عمومی با فیلتر، فرم ایجاد/ویرایش آگهی در پنل شرکت

## فاز ۴: کارجویان و درخواست‌های همکاری (هفته ۱۰-۱۱)
- [ ] Use Caseهای Candidate/Resume: ثبت اطلاعات فردی، سوابق، مهارت‌های دستگاهی
- [ ] Use Caseهای JobApplication: ارسال درخواست، چرخه وضعیت (`Submitted → Reviewed → InterviewScheduled → Accepted/Rejected`)
- [ ] Application Tracking Token برای پیگیری بدون افشای هویت
- [ ] صفحات فرانت‌اند: پروفایل/رزومه کارجو، ارسال درخواست، پیگیری وضعیت

## فاز ۵: پرداخت، ارتقاء آگهی و آماده‌سازی انتشار (هفته ۱۲-۱۳)
- [ ] یکپارچه‌سازی درگاه ZarinPal برای بسته‌های ارتقاء/نردبان آگهی
- [ ] Rate Limiting و سخت‌سازی امنیتی طبق سند ۰۵
- [ ] تست رگرسیون کامل QA + تست بار پایه
- [ ] اعتبارسنجی آمادگی Kubernetes (Graceful Shutdown, ConfigMap/Secret, Probes)
- [ ] استقرار نسخه MVP در محیط Staging/Production

---

## فازهای آتی (Post-MVP — طبق سند ۰۰)
- **ماژول تأمین‌کنندگان صنعتی (B2B):** نیازمندی‌های خرید/فروش مواد اولیه، ضایعات صنعتی، ماشین‌آلات دست‌دوم، خدمات پیمانکاری.
- **سامانه پیام‌رسان درون‌پلتفرمی:** گفتگوی امن میان کارفرما و کارجو/تأمین‌کننده، با رعایت اصل Privacy Proxy.

> این دو مورد **خارج از محدوده MVP** هستند و نباید در `Tasks.md` فاز فعلی هیچ‌یک از سه ایجنت گنجانده شوند.
