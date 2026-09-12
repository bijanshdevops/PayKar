# IndustrialPlatform

پلتفرم آگهی‌های شغلی شهرک‌های صنعتی — بک‌اند .NET 9 (معماری هگزاگونال) و فرانت‌اند React 18 + Vite + TypeScript.

این ریپازیتوری در ابتدا توسط هوش مصنوعی و بدون اجرای واقعی کامپایلر نوشته شد. مراحل زیر روی سیستم واقعی اجرا و باگ‌های واقعی‌اش (نسخه پکیج‌ها، پیکربندی DI، مدل‌سازی EF Core) پیدا و رفع شده‌اند — این README نسخه تأییدشده و کارکردن است.

## ساختار پروژه

```
IndustrialPlatform/
├── docs/                 # مستندات معماری، قرارداد API، قوانین امنیتی، نقشه راه و ...
│   ├── backend/          # AGENT.md, Tasks.md, Definition-Of-Done.md برای ایجنت بک‌اند
│   ├── frontend/         # همان بسته برای ایجنت فرانت‌اند
│   └── QA/                # Test-Plan.md و Definition-Of-Done.md برای ایجنت تست
├── backend/              # سولوشن .NET 9 (7 پروژه، معماری هگزاگونال)
├── frontend/             # اپلیکیشن React 18 + Vite + TypeScript
└── docker-compose.yml    # PostgreSQL 16 + Redis 7 + Api برای توسعه محلی
```

نکته: پوشه قدیمی و اشتباه `docs/forntend` (با غلط املایی) باقی مانده و باید دستی حذف شود — من در این محیط دسترسی به حذف فایل ندارم.

## پیش‌نیازها

- .NET SDK 9
- Node.js 20+ و npm
- Docker + Docker Compose (برای PostgreSQL و Redis) — یا نصب محلی PostgreSQL 16 و Redis 7
- (اختیاری برای پرداخت واقعی) یک حساب کاربری در درگاه زرین‌پال (Sandbox کافی است)

## راه‌اندازی گام‌به‌گام

### ۱. بالا آوردن PostgreSQL و Redis

```bash
docker compose up -d postgres redis
```

نکته مهم: پورت میزبان Postgres عمداً روی `5433` تنظیم شده (نه `5432` پیش‌فرض)، چون در محیط توسعه ممکن است یک PostgreSQL محلی دیگر از قبل روی `5432` گوش بدهد و باعث خطای `password authentication failed` شود (این دقیقاً همان چیزی است که در تست این پروژه رخ داد). اگر روی سیستم شما پورت ۵۴۳۲ آزاد است و ترجیح می‌دهید همان را استفاده کنید، هم در `docker-compose.yml` (`ports: "5432:5432"`) و هم در `appsettings.json` (`ConnectionStrings:Postgres`) مقدار پورت را هماهنگ تغییر دهید — کانکشن‌استرینگ داخلی سرویس `api` در `docker-compose.yml` (`Host=postgres;Port=5432`) نیازی به تغییر ندارد چون آن یک ارتباط داخلی بین کانتینرهاست.

### ۲. تنظیم متغیرهای محیطی بک‌اند

فایل `backend/src/IndustrialPlatform.Api/appsettings.json` از قبل با مقادیر توسعه محلی پر شده، اما این مقادیر را حتماً پیش از استفاده واقعی جایگزین کنید (به‌خصوص در appsettings.Development.json یا متغیرهای محیطی، نه در فایل commit‌شده):

- `Jwt:SigningKey` — یک رشته تصادفی حداقل ۳۲ کاراکتری.
- `MeliPayamak:Username` / `MeliPayamak:Password` — اطلاعات حساب ملی‌پیامک.
- `ZarinPal:MerchantId` — کد پذیرنده زرین‌پال (Sandbox یا واقعی).
- `PublicApiBaseUrl` — آدرسی که خود Api از بیرون با آن در دسترس است (زرین‌پال کاربر را به `{PublicApiBaseUrl}/api/v1/payments/callback` برمی‌گرداند). در توسعه محلی معمولاً `https://localhost:5001`.
- `FrontendBaseUrl` — آدرس اپ فرانت‌اند (پیش‌فرض `http://localhost:5173`) که بعد از پردازش Callback پرداخت، کاربر به `{FrontendBaseUrl}/payment-result` هدایت می‌شود.
- `Cors:AllowedOrigins` — باید شامل آدرس فرانت‌اند باشد.

### ۳. ساخت Migration های اولیه

دو `DbContext` مستقل داریم (طبق قانون معماری، Identity و Persistence نباید به هم وابسته باشند) که باید هرکدام جداگانه Migration بگیرند. این مراحل روی محیط واقعی تست و تأیید شده‌اند:

```bash
cd backend

# نصب ابزار dotnet-ef در صورت نیاز
dotnet tool install --global dotnet-ef

# Migration برای دامنه اصلی (Company/JobAd/Candidate/Payment/Geography)
dotnet ef migrations add InitialCreate \
  --project src/IndustrialPlatform.Persistence \
  --startup-project src/IndustrialPlatform.Api \
  --context AppDbContext

# Migration برای ماژول احراز هویت (User/Role/RefreshToken)
dotnet ef migrations add InitialCreate \
  --project src/IndustrialPlatform.Identity \
  --startup-project src/IndustrialPlatform.Api \
  --context IdentityDbContext

# اعمال هر دو Migration روی دیتابیس
dotnet ef database update --project src/IndustrialPlatform.Persistence --startup-project src/IndustrialPlatform.Api --context AppDbContext
dotnet ef database update --project src/IndustrialPlatform.Identity --startup-project src/IndustrialPlatform.Api --context IdentityDbContext
```

اگر پیام `password authentication failed for user "industrial_platform"` دیدید، تقریباً همیشه یعنی یک PostgreSQL دیگر روی همان پورت میزبان در حال گوش دادن است (بررسی کنید: `netstat -ano | findstr :5432` یا پورتی که استفاده می‌کنید) — راه‌حل، هماهنگ کردن یا تغییر پورت است (بخش ۱ همین سند).

پس از این مرحله، طبق `docs/08-Decision-Log.md` (ADR-004) باید یک Migration دستی SQL هم برای ایندکس‌های GIN/`pg_trgm` جستجوی فارسی روی جدول `job_ads` اضافه کنید — این ایندکس با Fluent API قابل تعریف نیست و باید با `migrationBuilder.Sql(...)` نوشته شود.

### ۴. اجرای بک‌اند

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/IndustrialPlatform.Api
```

Swagger روی `https://localhost:5001/swagger` در دسترس خواهد بود (در محیط Development).

### ۵. اجرای فرانت‌اند

```bash
cd frontend
npm install
npm run dev
```

یک فایل `.env` در `frontend/` بسازید (یا `.env.local`) و آدرس API را ست کنید:

```
VITE_API_BASE_URL=https://localhost:5001/api/v1
```

اپ روی `http://localhost:5173` بالا می‌آید.

### ۶. اجرا با Docker Compose (جایگزین مرحله ۴)

```bash
docker compose up -d --build
```

Api روی پورت `5000` (map شده از `8080` داخل کانتینر) در دسترس خواهد بود.

## نکات مهم برای اولین بیلد

- چون کد بدون کامپایل واقعی نوشته شده، احتمال خطاهای جزئی (namespace گم‌شده، امضای متد نادرست و…) وجود دارد. **در همین Cowork خطاهای کامپایل را برایم بفرستید تا اصلاح کنم.**
- تمام Handlerها/Repositoryها/Endpointها طبق سند `docs/backend/AGENT.md` و ماتریس وابستگی `CLAUDE.md` نوشته شده‌اند؛ یک بازبینی خودکار (Agent) روی کل بک‌اند/فرانت‌اند انجام شد و مشکل کامپایلی مسدودکننده‌ای پیدا نشد، اما این جایگزین کامپایل واقعی نیست.
- طی این بازبینی یک باگ واقعی پیدا و رفع شد: پراپرتی مرده `RowVersion` در `Shared/Entities/BaseEntity.cs` که با مکانیزم `UseXminAsConcurrencyToken()` تداخل ستون داشت و برخلاف قرارداد نام‌گذاری snake_case سند `03-Database-Standards.md` بود؛ حذف شد چون توکن همروندی به‌صورت shadow property از طریق xmin پستگرس مدیریت می‌شود.
- فاز پرداخت (ZarinPal) هنوز با کلید Sandbox تست نشده — قبل از استفاده واقعی حتماً با یک تراکنش آزمایشی در محیط Sandbox زرین‌پال تست کنید.

## وضعیت پیاده‌سازی MVP

| فاز | وضعیت |
|---|---|
| فاز ۰ — اسکلت Solution و زیرساخت | ✅ کامل |
| فاز ۱ — Auth/OTP/JWT | ✅ کامل |
| فاز ۲ — پروفایل شرکت و تایید ادمین | ✅ کامل |
| فاز ۳ — آگهی شغلی (CRUD/انتشار/جستجو) | ✅ کامل |
| فاز ۴ — پروفایل کارجو و درخواست‌ها | ✅ کامل |
| فاز ۵ — پرداخت و ارتقاء آگهی (ZarinPal) | ✅ کامل |

برای جزئیات کامل هر فاز، `docs/07-Roadmap.md` و `docs/backend/Tasks.md` / `docs/frontend/Tasks.md` را ببینید. برای سناریوهای تست، `docs/QA/Test-Plan.md`.
