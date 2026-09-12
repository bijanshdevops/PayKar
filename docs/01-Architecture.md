# 🏛 سند معماری فنی سیستم (System Architecture)

| مشخصه | مقدار |
| :--- | :--- |
| **سند** | معماری فنی پلتفرم آگهی‌های صنعتی (IndustrialPlatform) |
| **ورژن** | 1.0.0 |
| **معمار** | شاهین |
| **الگوی پایه** | هگزاگونال یکپارچه (Ports & Adapters) + Clean Architecture |
| **ران‌تایم و فریمورک** | .NET 9 (C# 13) |

---

## ۱. فلسفه و مدل مفهومی معماری (Ports & Adapters)

معماری این پروژه بر پایه جداسازی دقیق منطق کسب‌وکار (Core) از فناوری‌های بیرونی و زیرساخت (Details) پیاده‌سازی شده است.  
هسته دامنه و کاربردها در مرکز قرار دارند و دنیای خارج صرفاً از طریق پورت‌ها (Ports) با این هسته ارتباط برقرار می‌کند.

### لایه‌بندی مفهومی جریان داده:
1. **API Layer (Driving Adapter):** دریافت درخواست‌های کلاینت و فراخوانی پورت‌های ورودی لایه Application.
2. **Application Layer (Use Cases & Ports):** مدیریت سناریوهای کاربری، تعریف پورت‌های ورودی و خروجی.
3. **Domain Layer (Core Logic):** انتیتی‌ها، اشیاء مقداری، قوانین دامنه و رویدادها.
4. **Persistence Layer (Driven Adapter):** پیاده‌سازی پورت‌های ریپازیتوری با EF Core و PostgreSQL.
5. **Infrastructure Layer (External Adapters):** پیاده‌سازی سرویس‌های خارجی مانند ملی‌پیامک، ردیس و زرین‌پال.
6. **Identity Layer (Auth & Security):** مدیریت نشست‌ها، کاربران، توکن‌های JWT و OTP.
7. **Shared Layer (Foundation):** زیرساخت‌های پایه مانند Result Pattern و انتیتی‌های پایه.

---

## ۲. تفکیک دقیق لایه‌ها و مسئولیت‌ها

### ۲.۱. لایه فونداسیون (IndustrialPlatform.Shared)
پایه‌ای‌ترین پروژه Solution که هیچ وابستگی دامنه‌ای ندارد و تنها شامل ابزارها و الگوهای تکرارشونده کل سیستم است:
- Result Pattern: کلاس‌های Result, Result<T>, Error, ErrorType
- انتیتی‌های پایه: BaseEntity<TId>, AggregateRoot<TId>, ISoftDeletable, IAuditableEntity
- قرارداد پاسخ استاندارد: ApiResponse<T>
- ابزارهای عمومی: الحاقات رشته‌ای فارسی، نرمال‌سازی حروف ک/ی، اعتبارسنجی عبارات باقاعده پایه

### ۲.۲. لایه دامنه (IndustrialPlatform.Domain)
قلب تپنده بیزینس بدون هیچ‌گونه وابستگی به دیتابیس یا فریمورک‌های بیرونی:
- انتیتی‌ها و ریشه‌های تجمیع (Company, JobAd, IndustrialZone, Resume)
- اشیاء مقداری غیرقابل تغییر (Value Objects) مانند NationalId, MobileNumber, WorkShift, SalaryRange
- رویدادهای دامنه (Domain Events) برای آگاه‌سازی سایر بخش‌ها از تغییرات وضعیتی بیزینس
- قوانین غنی اعتبارسنجی کسب‌وکار (عدم استفاده از انتیتی‌های تهی یا Anemic Model)

### ۲.۳. لایه کاربرد (IndustrialPlatform.Application)
ارکستراسیون سناریوهای کاربر و تعریف پورت‌ها:
- پیاده‌سازی Use Caseها بر پایه تفکیک دستور/پرس‌وجو (CQRS)
- تعریف تمام پورت‌های خروجی (ICompanyRepository, IJobAdRepository, ISmsService, IUnitOfWork, ICacheService)
- اشیاء انتقال داده (DTOs) و نگاشت‌ها
- اعتبارسنجی درخواست‌ها با ابزارهایی مانند FluentValidation

### ۲.۴. لایه هویت و دسترسی (IndustrialPlatform.Identity)
مدیریت کاربران، نشست‌ها و مجوزها به‌صورت یک ماژول مجزا و کپسوله:
- انتیتی‌های کاربری (User, Role, UserRole, RefreshToken)
- تولید و اعتبارسنجی JWT Access Token و چرخه تمدید با Refresh Token
- مدیریت تولید، ارسال و اعتبارسنجی کدهای یکبارمصرف (OTP)
- کنترل سطوح دسترسی مبتنی بر Permission / Claim

### ۲.۵. لایه پایگاه داده (IndustrialPlatform.Persistence)
آداپتور دسترسی به داده (Driven Adapter):
- پیکربندی تمام جداول با استفاده از Fluent API (هیچ Data Annotation در Domain مجاز نیست)
- مدیریت یکپارچه AppDbContext، ایندکس‌ها، کلیدهای خارجی و تراکنش‌ها (UnitOfWork)
- پیاده‌سازی Global Query Filter برای تمام انتیتی‌های دارای ISoftDeletable
- ثبت خودکار فیلدهای CreatedAtUtc, CreatedBy, LastModifiedAtUtc, LastModifiedBy از طریق SaveChangesInterceptor

### ۲.۶. لایه زیرساخت (IndustrialPlatform.Infrastructure)
آداپتورهای ابزارهای جانبی و第三方:
- آداپتور ارسال پیامک با وب‌سرویس پترن ملی‌پیامک (MeliPayamakSmsService)
- آداپتور کشینگ سریع روی ردیس (RedisCacheService)
- آداپتور درگاه پرداخت زرین‌پال (ZarinPalPaymentGateway)
- آداپتور ذخیره‌سازی فایل‌های رسانه‌ای (لوگو، مدارک شرکت)

### ۲.۷. لایه واسط برنامه‌نویسی (IndustrialPlatform.Api)
راننده سیستم (Driving Adapter) و Composition Root:
- اکسپوز کردن اندپوینت‌ها (Minimal APIs ترجیحی یا Controllers)
- پیاده‌سازی Exception Handling Middleware سراسری (برای خطاهای هندل‌نشده سیستمی)
- ثبت و کانفیگ وابستگی‌ها (IoC Container Setup)
- اندپوینت‌های مانیتورینگ سلامت (/health/live و /health/ready)

---

## ۳. ماتریس وابستگی پروژه‌ها (Project Dependencies Matrix)

جهت جدول وابستگی مجاز را نشان می‌دهد (پروژه سطر به پروژه ستون رفرنس دارد):

| پروژه | Shared | Domain | Application | Identity | Persistence | Infrastructure | Api |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| Shared | - | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Domain | ✅ | - | ❌ | ❌ | ❌ | ❌ | ❌ |
| Application | ✅ | ✅ | - | ❌ | ❌ | ❌ | ❌ |
| Identity | ✅ | ✅ | ✅ | - | ❌ | ❌ | ❌ |
| Persistence | ✅ | ✅ | ✅ | ❌ | - | ❌ | ❌ |
| Infrastructure | ✅ | ✅ | ✅ | ❌ | ❌ | - | ❌ |
| Api (Host) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | - |

> ⚠️ **خط قرمز معماری:** لایه Api هرگز نباید در کنترلرها یا اندپوینت‌ها مستقیماً AppDbContext یا ریپازیتوری‌های Persistence را تزریق کند. تمام تعاملات باید از طریق Use Caseها و پورت‌های Application و Identity هدایت شوند.

---

## ۴. استانداردهای رفتاری و پترن‌های مهندسی

### ۴.۱. مدیریت خطا بدون Exception (The Result Pattern)
پرتاب Exception برای جریان‌های منطقی بیزینس ممنوع است. تمام متدهای دامین و Use Caseها خروجی مشخصی از نوع Result یا Result<T> دارند.

### ۴.۲. حذف منطقی پایدار (Universal Soft Delete)
هیچ رکوردی در دیتابیس به صورت DELETE فیزیکی حذف نمی‌شود. تمامی انتیتی‌ها اینترفیس ISoftDeletable را پیاده‌سازی کرده و فیلتر سراسری روی آن‌ها اعمال می‌شود.

### ۴.۳. قرارداد خروجی استاندارد API (Response Envelope)
خروجی تمامی اندپوینت‌ها باید از ساختار یکنواخت با فیلدهای success, statusCode, message, data و errors تبعیت کند.

---

## ۵. آمادگی کانتینری و کوبرنتیز (Cloud-Native & Kubernetes Readiness)

سیستم به گونه‌ای طراحی شده که انتقال از اجرای محلی با Docker Compose به کلاستر Kubernetes (K8s) هیچ تغییری در بیزینس ایجاد نکند:

1. **Stateless API:** نشست کاربر در توکن JWT نگهداری شده و هیچ Stateای روی حافظه سرور ذخیره نمی‌شود.
2. **Health Probes:**
   - اندپوینت `GET /health/live`: بررسی زنده بودن کانتینر .NET (K8s Liveness Probe).
   - اندپوینت `GET /health/ready`: بررسی برقراری اتصال به PostgreSQL و Redis (K8s Readiness Probe).
3. **پیکربندی داینامیک:** تمام مقادیر اتصال از طریق Environment Variables بارگذاری شده و با ConfigMap و Secret در کوبرنتیز هماهنگ هستند.
4. **Graceful Shutdown:** پشتیبانی از توقف امن سیگنال SIGTERM در کانتینر جهت اتمام پردازش درخواست‌های در جریان.
