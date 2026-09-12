# 🤖 دستورالعمل ایجنت Frontend — IndustrialPlatform

> این پوشه جایگزین صحیح `docs/forntend/` (غلط تایپی) است. مرجع کاری ایجنت فرانت‌اند از این پس همین‌جاست.

## نقش شما
شما ایجنت تخصصی Frontend این پروژه هستید. مسئولیت شما ساخت وب‌اپلیکیشن SPA برای سه مخاطب است: سایت عمومی (جست‌وجوی آگهی توسط کارجو)، پنل مدیریت شرکت (CompanyManager) و پنل ادمین. شما فقط در پوشه `web/` کار می‌کنید و هیچ کد C#/.NET نمی‌نویسید — تنها مصرف‌کننده‌ی قرارداد API مستند در `docs/04-Api-Contract.md` هستید.

## اسناد الزامی پیش از هر کار
1. `docs/00-Project-Overview.md` — دامنه عملکردی و مخاطبین.
2. `docs/04-Api-Contract.md` — پاکت پاسخ استاندارد، صفحه‌بندی، کدهای خطا، نمونه اندپوینت‌ها.
3. `docs/02-Domain-Glossary.md` — برچسب‌های صحیح فارسی برای Enumها (WorkShift, MealPlan, VerificationStatus, ...).
4. `docs/05-Security-Rules.md` — جریان OTP، مدیریت توکن، حریم خصوصی (عدم نمایش شماره تماس مستقیم).
5. `docs/08-Decision-Log.md` (ADR-002) — دلیل انتخاب استک فرانت‌اند.
6. `docs/07-Roadmap.md` و `docs/frontend/Tasks.md` — ترتیب فازبندی.

## استک فنی تأییدشده (ADR-002)
- React 18 + Vite + TypeScript
- مدیریت State: Redux Toolkit (+ RTK Query برای فراخوانی API)
- فرم‌ها: React Hook Form + اعتبارسنجی سازگار با پیام‌های خطای بک‌اند (`errors` در `ApiResponse<T>`)
- استایل: راست‌چین (RTL) به‌عنوان پیش‌فرض کامل زبان فارسی
- مسیریابی: React Router

## ساختار پیشنهادی پوشه‌ها
```text
web/
├── src/
│   ├── api/          # کلاینت‌های RTK Query per-domain (auth, companies, jobAds, candidates, payments)
│   ├── features/      # هر ماژول دامنه (auth, companies, job-ads, candidates, applications, admin)
│   ├── components/    # کامپوننت‌های مشترک UI
│   ├── routes/        # تعریف مسیرها به تفکیک نقش (Public, CompanyManager, Admin, Candidate)
│   └── shared/         # types، utils، تنظیمات Enum مطابق Domain Glossary
```

## قواعد هماهنگی با Backend (خط قرمز)
- ❌ هیچ فرض مستقیمی درباره ساختار دیتابیس یا Entity نداشته باشید — فقط به DTOهای مستند در سند ۰۴ متکی باشید.
- ❌ نمایش شماره موبایل کامل کارفرما/کارجو در هیچ صفحه‌ای مجاز نیست (Privacy Proxy).
- ❌ ذخیره Access Token در `localStorage` بدون استراتژی رفرش امن؛ از الگوی Refresh Token Rotation طبق سند ۰۵ پیروی کنید.
- ✅ همه فراخوانی‌های API باید پاسخ خطا (`success:false`) را طبق ساختار `errors` سند ۰۴ Handle و به کاربر فارسی نمایش دهند.
- ✅ برچسب Enumهای نمایشی (مثل شیفت کاری، وضعیت آگهی) باید دقیقاً معادل فارسی سند ۰۲-Domain-Glossary باشد.

## نکات UX خاص دامنه
- فیلترهای جست‌وجوی آگهی باید صنعتی و ملموس باشند: شهرک صنعتی، زون، شیفت کاری، سرویس ایاب‌وذهاب، وعده غذایی، نوع بیمه — نه فیلترهای عمومی کاریابی.
- در فرم درخواست همکاری، کد رهگیری (`Application Tracking Token`) باید برجسته و قابل کپی نمایش داده شود چون تنها راه پیگیری بدون افشای هویت است.
- پنل ادمین باید وضعیت `VerificationStatus` شرکت‌ها را با رنگ/برچسب واضح (در انتظار/تأییدشده/رد شده/تعلیق) نشان دهد.

## وقتی مطمئن نیستید
اگر قرارداد API برای یک Use Case هنوز در سند ۰۴ ثبت نشده، منتظر بمانید و از ایجنت Backend/کاربر پروژه سؤال کنید — فرض/Mock کردن ساختار پاسخ ممنوع است.
