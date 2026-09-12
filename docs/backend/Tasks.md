# ✅ بک‌لاگ ایجنت Backend — IndustrialPlatform

> منبع فازبندی: `docs/07-Roadmap.md`. هر آیتم باید طبق `Definition-Of-Done.md` همین پوشه تکمیل تلقی شود. ترتیب اجرا از بالا به پایین است؛ از فاز بعدی شروع نکنید مگر فاز قبل تکمیل شده باشد.

## فاز ۰ — فونداسیون و زیرساخت
- [ ] ایجاد Solution با ۷ پروژه: `Shared`, `Domain`, `Application`, `Identity`, `Persistence`, `Infrastructure`, `Api` + رعایت ماتریس وابستگی سند ۰۱
- [ ] `Shared`: `Result`, `Result<T>`, `Error`, `ErrorType`, `BaseEntity<TId>`, `AggregateRoot<TId>`, `ISoftDeletable`, `IAuditableEntity`, `ApiResponse<T>`
- [ ] `Persistence`: اسکلت `AppDbContext` + `SaveChangesInterceptor` برای پرکردن خودکار Audit Columns
- [ ] `Api`: Global Exception Handling Middleware + نگاشت به `ApiResponse<T>`
- [ ] اندپوینت‌های `/health/live`, `/health/ready` (بررسی PostgreSQL + Redis)
- [ ] `docker-compose.yml`: Api + PostgreSQL 16 + Redis 7
- [ ] Seed اولیه: استان‌ها، شهرها، شهرک‌های صنعتی نمونه، نقش‌ها (Admin, CompanyManager, Candidate)

## فاز ۱ — احراز هویت و جغرافیای صنعتی
- [ ] Domain: Value Objectهای `MobileNumber`, `NationalId`
- [ ] Identity: موجودیت‌های `User`, `Role`, `UserRole`, `RefreshToken`
- [ ] Application/Identity: Use Case `RequestOtpCommand`, `VerifyOtpCommand` (صدور Access/Refresh Token)
- [ ] Infrastructure: آداپتور `MeliPayamakSmsService` (پیاده‌سازی پورت `ISmsService`)
- [ ] Rate Limiting روی `POST /auth/otp/request` (سند ۰۵)
- [ ] Domain: `Province`, `City`, `IndustrialZone`, `ZoneType`
- [ ] Persistence: Fluent Config + Migration برای موجودیت‌های جغرافیایی + Seed Data

## فاز ۲ — پروفایل شرکت‌ها
- [ ] Domain: Aggregate `Company` (NationalId, RegistrationNumber, VerificationStatus, IndustrialZoneId, AddressDetail)
- [ ] Application: `CreateCompanyCommand`, `UpdateCompanyCommand`, `RequestCompanyVerificationCommand`, `ApproveCompanyCommand` (Admin-only), `GetCompanyByIdQuery`
- [ ] Persistence: Unique Partial Index روی `national_id` (`WHERE is_deleted = false`)
- [ ] Infrastructure: آداپتور ذخیره‌سازی فایل برای لوگو/گالری/مدارک
- [ ] Api: اندپوینت‌های `POST/PUT /api/v1/companies`, `POST /api/v1/companies/{id}/verification-request`, `POST /api/v1/admin/companies/{id}/approve`

## فاز ۳ — آگهی‌های استخدام
- [ ] Domain: Aggregate `JobAd` با چرخه وضعیت `Draft→Published→Expired→Closed→Archived` + Value Objectهای `WorkShift`, `CommuteService`, `MealPlan`, `InsuranceType`, `SalaryRange`
- [ ] Application: `CreateJobAdCommand`, `UpdateJobAdCommand`, `PublishJobAdCommand`, `CloseJobAdCommand`, `SearchJobAdsQuery` (فیلتر شهرک/زون/شیفت/سرویس)
- [ ] Persistence: ایندکس Partial برای آگهی‌های `Published`، اکستنشن `pg_trgm`/GIN برای جست‌وجوی متنی فارسی (ADR-004)
- [ ] Api: `POST/PUT /api/v1/job-ads`, `GET /api/v1/job-ads` (با پارامترهای فیلتر و صفحه‌بندی طبق سند ۰۴)
- [ ] Job زمان‌بندی‌شده (Background Job) برای انتقال خودکار آگهی‌های منقضی به وضعیت `Expired`

## فاز ۴ — کارجویان و درخواست‌ها
- [ ] Domain: `Candidate`, `Resume` (سوابق، مهارت‌های دستگاهی، وضعیت نظام‌وظیفه)
- [ ] Domain: `JobApplication` با چرخه وضعیت `Submitted→Reviewed→InterviewScheduled→Accepted/Rejected` + `TrackingToken`
- [ ] Application: `CreateResumeCommand`, `SubmitJobApplicationCommand`, `UpdateApplicationStatusCommand` (Company-only، فقط برای آگهی‌های متعلق به همان شرکت)
- [ ] Api: `POST /api/v1/candidates/resumes`, `POST /api/v1/job-ads/{id}/applications`, `GET /api/v1/applications/track/{trackingToken}`

## فاز ۵ — پرداخت و سخت‌سازی
- [ ] Domain: `AdBoostPlan` (پلن‌های ارتقاء/نردبان آگهی)
- [ ] Infrastructure: آداپتور `ZarinPalPaymentGateway` (Request + Callback Verify سمت سرور، پشتیبانی `Idempotency-Key`)
- [ ] Application: `PurchaseAdBoostCommand`, `VerifyPaymentCommand`
- [ ] بازبینی امنیتی کامل طبق `docs/05-Security-Rules.md` (Rate Limiting سراسری، لاگ حسابرسی)
- [ ] بررسی آمادگی Kubernetes: Graceful Shutdown، ConfigMap/Secret، بدون State در حافظه Api

---

## خارج از محدوده فعلی (Backlog آتی — از Roadmap فاز Post-MVP)
- ماژول تأمین‌کنندگان صنعتی (B2B)
- سامانه پیام‌رسان درون‌پلتفرمی
