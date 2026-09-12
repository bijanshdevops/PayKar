# 06. فرآیند توسعه و استانداردهای Git (Development Workflow & Git Standards)

| مشخصه | مقدار |
| :--- | :--- |
| **سند** | فرآیند توسعه پلتفرم آگهی‌های صنعتی (`IndustrialPlatform`) |
| **ورژن** | 2.0.0 (بازنویسی برای هم‌راستایی با ساختار سه‌تیمی backend/frontend/QA) |

---

## ۱. ساختار مخزن (Repository Layout)

پروژه به‌صورت سه بخش موازی پیش می‌رود که هرکدام مستندات و بک‌لاگ اختصاصی خود را در `docs/` دارند:

```text
docs/
├── backend/    # AGENT.md, Tasks.md, Definition-Of-Done.md
├── frontend/   # AGENT.md, Tasks.md, Definition-Of-Done.md
└── QA/         # AGENT.md, Test-Plan.md, Definition-Of-Done.md
src/            # کد بک‌اند (.NET 9) — ساختار طبق سند 01-Architecture
web/            # کد فرانت‌اند (React + Vite + TypeScript)
```

---

## ۲. قواعد نام‌گذاری Branch

- `feature/{scope}-{short-description}` — مثال: `feature/job-ads-industrial-filters`
- `bugfix/{scope}-{short-description}` — مثال: `bugfix/otp-rate-limit`
- `hotfix/{scope}-{short-description}` — برای رفع فوری در Production
- شاخه‌های ویژه: `main` (آماده Deploy)، `develop` (یکپارچه‌سازی فعال)

`{scope}` باید یکی از حوزه‌های تعریف‌شده باشد: `auth`, `company`, `job-ads`, `candidates`, `applications`, `payments`, `identity`, `infra`, `api`, `web`, `qa`.

---

## ۳. پیام‌های Commit (Conventional Commits)

فرمت: `type(scope): description`

مثال‌های واقعی این پروژه:
- `feat(auth): add OTP request rate limiting`
- `feat(job-ads): implement industrial zone and shift filters`
- `fix(company): correct national id uniqueness check on soft-deleted records`
- `feat(payments): integrate zarinpal callback verification`
- `docs(db): update indexing strategy for job_ads table`
- `test(applications): add scenario tests for application status transitions`
- `refactor(persistence): extract fluent api configs per entity`

انواع مجاز `type`: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `perf`.

---

## ۴. چرخه‌ی Pull Request

1. Branch از `develop` گرفته شود (نه مستقیم از `main`).
2. پیاده‌سازی کد + رعایت قوانین معماری `CLAUDE.md` (Result Pattern، جهت وابستگی، Soft Delete، Fluent API).
3. تست‌های واحد/یکپارچگی مرتبط نوشته و لوکال اجرا شوند.
4. باز کردن PR با توضیح: چه چیزی تغییر کرد، چرا، و کدام Use Case/اندپوینت را پوشش می‌دهد.
5. عبور از CI/CD (Build, Static Analysis, Unit Tests) اجباری است.
6. حداقل یک تایید (Review) از توسعه‌دهنده ارشد یا ایجنت مسئول همان لایه.
7. **چک‌لیست ویژه Review معماری:**
   - آیا لایه `Api` مستقیم به `Persistence`/`DbContext` وصل شده؟ (رد قطعی PR)
   - آیا Exception به‌جای `Result<T>` برای خطای بیزینسی پرتاب شده؟
   - آیا حذف فیزیکی (`DELETE`) به‌جای Soft Delete استفاده شده؟
   - آیا پاسخ API از پاکت استاندارد `ApiResponse<T>` عبور کرده؟
8. استراتژی ادغام: **Squash and Merge**.

---

## ۵. هماهنگی بین سه تیم (Backend / Frontend / QA)

- هرگونه تغییر در قرارداد API (سند ۰۴) باید همزمان در `docs/backend/Tasks.md` و `docs/frontend/Tasks.md` منعکس شود.
- تیم QA پیش از شروع تست هر Use Case، سند `docs/QA/Test-Plan.md` را بر اساس آخرین نسخه‌ی API Contract به‌روزرسانی می‌کند.
- تغییرات تصمیمی سطح بالا (تغییر استک، تغییر معماری) باید در `08-Decision-Log.md` ثبت شوند، نه فقط در پیام Commit.
