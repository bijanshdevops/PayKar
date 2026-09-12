# ✔️ معیار پذیرش ایجنت Backend — Definition of Done

هیچ Use Case/اندپوینتی «تمام‌شده» تلقی نمی‌شود مگر تمام موارد زیر برقرار باشد:

## معماری و کد
- [ ] جهت وابستگی پروژه‌ها دقیقاً طبق ماتریس سند ۰۱ رعایت شده (بدون رفرنس معکوس یا رفرنس Api→Persistence).
- [ ] هیچ Exception برای خطای بیزینسی/ولیدیشن پرتاب نشده؛ خروجی همیشه `Result` یا `Result<T>` است.
- [ ] هیچ Data Annotation روی کلاس‌های `Domain` وجود ندارد؛ تمام تنظیمات جدول در `IEntityTypeConfiguration<T>` لایه Persistence است.
- [ ] Entity جدید Interfaceهای `ISoftDeletable` و `IAuditableEntity` را پیاده‌سازی کرده (مگر جدول صرفاً Lookup/Seed ثابت باشد).
- [ ] عملیات حذف معادل `is_deleted = true` است؛ هیچ `DELETE FROM` مستقیمی در کد یا Migration وجود ندارد.

## دیتابیس
- [ ] نام جدول جمع/`snake_case`، نام ستون مفرد/`snake_case` (سند ۰۳).
- [ ] Constraintها با الگوی نام‌گذاری `pk_`, `fk_`, `ix_`, `ux_`, `ck_` تعریف شده‌اند.
- [ ] ایندکس روی تمام ستون‌های Foreign Key موجود است.
- [ ] Unique Index حساس (مثل `national_id`) دارای شرط Partial `WHERE is_deleted = false` است.
- [ ] Migration با سویچ Idempotent قابل تولید و اجرا در CI/CD است.

## API
- [ ] پاسخ اندپوینت (موفق و خطا) دقیقاً ساختار `ApiResponse<T>` سند ۰۴ را دارد.
- [ ] کدهای خطا از رجیستری سند ۰۴ استفاده می‌کنند (بدون رشته‌ی خطای Ad-hoc).
- [ ] اندپوینت‌های لیستی از پارامترهای استاندارد `page`, `pageSize`, `sortBy`, `sortDir` پشتیبانی می‌کنند.
- [ ] ولیدیشن ورودی با FluentValidation قبل از رسیدن به Domain اجرا می‌شود.

## امنیت
- [ ] اندپوینت‌های محافظت‌شده RBAC/Ownership Check را رعایت می‌کنند (سند ۰۵).
- [ ] هیچ شماره موبایل کامل کارفرما/کارجو در DTO عمومی افشا نشده (Privacy Proxy).
- [ ] عملیات پرداخت دارای Verify سمت سرور و پشتیبانی `Idempotency-Key` است.
- [ ] رویدادهای حساس (ورود ناموفق، تغییر وضعیت شرکت، پرداخت) Log شده‌اند.

## تست
- [ ] تست واحد برای منطق Domain (Value Objectها، انتقال وضعیت JobAd/JobApplication).
- [ ] تست یکپارچگی حداقل برای مسیر موفق و یک مسیر خطا (Validation/NotFound) هر Use Case.
- [ ] تمام تست‌های موجود پروژه سبز هستند؛ هیچ تست Skip/Disabled بدون توضیح باقی نمانده.

## مستندسازی
- [ ] در صورت افزودن اندپوینت جدید یا تغییر Contract، سند `04-Api-Contract.md` به‌روزرسانی شده.
- [ ] در صورت تصمیم معماری جدید، ADR مربوطه در `08-Decision-Log.md` ثبت شده.
- [ ] آیتم مربوطه در `docs/backend/Tasks.md` تیک خورده است.
