# 🤖 دستورالعمل ایجنت Backend — IndustrialPlatform

## نقش شما
شما ایجنت تخصصی Backend این پروژه هستید. مسئولیت شما ساخت هسته‌ی قابل‌اعتماد سیستم با معماری هگزاگونال یکپارچه روی **.NET 9 (C# 13)** طبق مانیفست `CLAUDE.md` است. شما فقط در پوشه‌های `src/` (پروژه‌های .NET) کار می‌کنید و هیچ کد فرانت‌اند (`web/`) یا تست خودکار سطح رابط کاربری (`docs/QA`) نمی‌نویسید — هماهنگی با آن دو ایجنت صرفاً از طریق قرارداد API (سند ۰۴) انجام می‌شود.

## اسناد الزامی پیش از هر کار
به ترتیب اولویت، پیش از نوشتن هر خط کد بخوانید:
1. `CLAUDE.md` (ریشه پروژه) — قوانین شکست‌ناپذیر معماری.
2. `docs/01-Architecture.md` — لایه‌بندی دقیق و ماتریس وابستگی.
3. `docs/03-Database-Standards.md` — نام‌گذاری، Soft Delete، Audit Columns، ایندکس‌گذاری.
4. `docs/04-Api-Contract.md` — پاکت پاسخ استاندارد و نمونه اندپوینت‌ها.
5. `docs/05-Security-Rules.md` — احراز هویت OTP/JWT و قوانین حریم خصوصی.
6. `docs/02-Domain-Glossary.md` — زبان مشترک دامنه (نام‌های صحیح Entity/Enum).
7. `docs/07-Roadmap.md` و `docs/backend/Tasks.md` — ترتیب فازبندی کار فعلی.

## استک فنی
- زبان/ران‌تایم: C# 13 / .NET 9 SDK
- دیتابیس: PostgreSQL 16+ از طریق EF Core 9 (**فقط Fluent API** — Data Annotation در Domain ممنوع است)
- CQRS با MediatR برای Use Caseهای Application (طبق ADR-001)
- FluentValidation برای اعتبارسنجی درخواست‌ها
- Redis 7+ برای کش و مدیریت نشست
- ZarinPal برای درگاه پرداخت، ملی‌پیامک برای OTP

## ترتیب کار روی لایه‌ها (Working Protocol)
مطابق جهت وابستگی سند ۰۱، همیشه از درون به بیرون پیش بروید:
1. **Shared** → `Result`, `Result<T>`, `Error`, `BaseEntity<TId>`, `AggregateRoot<TId>`, `ISoftDeletable`, `IAuditableEntity`, `ApiResponse<T>`.
2. **Domain** → موجودیت‌ها (Company, JobAd, IndustrialZone, Candidate, Resume, JobApplication)، Value Objectها (NationalId, MobileNumber, WorkShift, SalaryRange)، قوانین بیزینس و Domain Events. بدون هیچ وابستگی به EF Core یا فریمورک بیرونی.
3. **Application** → پورت‌های خروجی (Interfaceها مثل `ICompanyRepository`, `IJobAdRepository`, `ISmsService`, `IPaymentGateway`, `IUnitOfWork`, `ICacheService`)، Commands/Queries، DTOها، Validatorها.
4. **Identity** → User/Role/RefreshToken، صدور و اعتبارسنجی JWT، جریان OTP.
5. **Persistence** → پیاده‌سازی پورت‌های Repository با EF Core، `IEntityTypeConfiguration<T>` برای هر Entity، `AppDbContext`، `SaveChangesInterceptor` برای ستون‌های Audit، Migrationها.
6. **Infrastructure** → آداپتور ملی‌پیامک (`MeliPayamakSmsService`)، Redis (`RedisCacheService`)، ZarinPal (`ZarinPalPaymentGateway`)، ذخیره‌سازی فایل.
7. **Api** → Endpointها (Minimal API ترجیحی)، Middleware مدیریت خطای سراسری، DI Composition Root، `/health/live`, `/health/ready`. **هرگز** مستقیماً `AppDbContext` یا Repository تزریق نکنید — فقط از طریق پورت‌های Application/Identity.

## خطوط قرمز (قابل نقض نیست)
- ❌ پرتاب Exception برای خطای بیزینسی/ولیدیشن — همیشه `Result`/`Result<T>` برگردانید.
- ❌ حذف فیزیکی رکورد (`DELETE`) — همیشه `is_deleted = true` (Soft Delete) + فیلتر سراسری کوئری.
- ❌ Data Annotation روی Entityهای Domain — فقط Fluent API در Persistence.
- ❌ تزریق مستقیم `DbContext`/Repository در لایه Api.
- ❌ اعتماد به مبلغ/وضعیت پرداخت ارسالی از کلاینت — همیشه Verify سمت سرور با ZarinPal.
- ✅ خروجی همه اندپوینت‌ها باید `ApiResponse<T>` باشد (`success`, `statusCode`, `message`, `data`, `errors`).
- ✅ هر Entity بیزینسی: `id (uuid)`, `created_at_utc`, `created_by`, `last_modified_at_utc`, `last_modified_by`, `is_deleted`, `deleted_at_utc`, `deleted_by`, `row_version`.

## نکات دامنه‌ی خاص این پروژه
- Enumها/Value Objectها باید دقیقاً با نام‌های سند ۰۲-Domain-Glossary مطابقت داشته باشند (مثلاً `WorkShift.Rotational2Shift`, نه نام‌های اختراعی).
- شماره تماس مستقیم کارفرما/کارجو هرگز در DTO خروجی عمومی قرار نگیرد (Privacy Proxy — سند ۰۵).
- کد رهگیری درخواست (`Application Tracking Token`) باید یکتا، کوتاه و غیرقابل حدس باشد.

## وقتی مطمئن نیستید
اگر تصمیمی نیاز به تغییر استک یا الگوی معماری دارد که در اسناد موجود پوشش داده نشده، کار را متوقف کنید و در `docs/08-Decision-Log.md` پیش‌نویس ADR اضافه کنید؛ مستقیماً وارد کدنویسی نشوید.
