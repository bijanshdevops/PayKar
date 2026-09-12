# ۰۹ - راهنمای نهایی Migration و بررسی رفرنس‌های شکسته

این سند خروجی تسک #62 است: یک بررسی نهایی روی کل پروژه برای یافتن رفرنس‌های شکسته/باقیمانده،
به‌همراه یک راهنمای کامل و قابل‌اجرا برای اعمال تمام Migrationهای دو DbContext پروژه
(`AppDbContext` و `IdentityDbContext`) روی یک checkout تازه.

---

## ۱. اجرای Migrationها از صفر (Clean Checkout)

پروژه دو DbContext مستقل با دو تاریخچه Migration جداگانه دارد. هرکدام باید جداگانه و با
فلگ `--context` صریح اجرا شوند. ترتیب اجرا مهم نیست (هرکدام روی جداول خودشان کار می‌کنند)
اما پیشنهاد می‌شود ابتدا `AppDbContext` و سپس `IdentityDbContext` اجرا شود.

پیش‌نیاز: کانتینرهای Docker (`industrial-platform-postgres`, `industrial-platform-redis`) باید
بالا باشند (`docker compose up -d` از ریشه پروژه).

از ریشه پروژه (`F:\IndustrialPlatform\backend`) اجرا کنید:

```powershell
# اعمال تمام Migrationهای AppDbContext (دامنه بیزینسی: شرکت، آگهی، پرداخت، تیکت، بنر و ...)
dotnet ef database update `
  --context AppDbContext `
  --project src\IndustrialPlatform.Persistence `
  --startup-project src\IndustrialPlatform.Api

# اعمال تمام Migrationهای IdentityDbContext (کاربر، نقش، احراز هویت)
dotnet ef database update `
  --context IdentityDbContext `
  --project src\IndustrialPlatform.Identity `
  --startup-project src\IndustrialPlatform.Api
```

برای افزودن Migration جدید در آینده، همین الگو با `migrations add <Name>` به‌جای
`database update` استفاده می‌شود:

```powershell
dotnet ef migrations add <NameOfMigration> `
  --context AppDbContext `
  --project src\IndustrialPlatform.Persistence `
  --startup-project src\IndustrialPlatform.Api
```

### رشته اتصال دیتابیس (`appsettings.json`)

```
Host=localhost;Port=5433;Database=industrial_platform;Username=industrial_platform;Password=changeme
```

---

## ۲. تاریخچه کامل Migrationهای `AppDbContext`

به ترتیب زمانی اعمال (این ترتیب توسط خود EF Core بر اساس timestamp نام فایل تضمین می‌شود):

| # | Migration | شرح |
|---|---|---|
| 1 | `20260829215728_BusinessFoundation_AdminApprovalAndFlatFee` | جایگزینی سیستم Boost با جریان تایید ادمین + هزینه ثابت برای JobAd؛ **حذف جدول `ad_boost_plans` در Up()** |
| 2 | `20260829221203_CompanyDocuments` | افزودن مدارک احراز هویت شرکت (`company_documents`) |
| 3 | `20260829223149_CandidateMobileAndResume` | افزودن موبایل و رزومه کارجو |
| 4 | `20260830010926_InitialCreate` | Baseline بازتولیدشده (squash)؛ **ایجاد مجدد جدول `ad_boost_plans` در Up()** — به بخش ۴ مراجعه شود |
| 5 | `20260830142023_AddSupportTickets` | جداول `support_tickets`, `support_messages` |
| 6 | `20260830143625_AddBannerAds` | جدول `banner_ads`، nullable شدن `payment_transactions.job_ad_id`، افزودن `payment_transactions.banner_ad_id` |

مدل فعلی و معتبر EF در `AppDbContextModelSnapshot.cs` نگه‌داری می‌شود و **هیچ ارجاعی به
`AdBoostPlan` ندارد** — یعنی از نظر کد فعلی، این موجودیت کاملاً بازنشسته شده است.

## ۳. تاریخچه کامل Migrationهای `IdentityDbContext`

| # | Migration | شرح |
|---|---|---|
| 1 | `20260829224005_AddUsernamePasswordAuth` | افزودن ورود با نام‌کاربری/رمزعبور در کنار OTP |
| 2 | `20260830011833_InitialCreate` | Baseline کاربران/نقش‌ها/توکن‌ها |
| 3 | `20260830130618_AddEmailAndMobileVerification` | افزودن ایمیل و وضعیت تایید موبایل |

این تاریخچه سالم است و هیچ ناسازگاری‌ای در آن یافت نشد.

---

## ۴. یافته مهم: جدول یتیم (Orphaned) `ad_boost_plans` در `AppDbContext`

### شرح مشکل

بررسی متوالی Migrationهای `AppDbContext` نشان داد:

1. Migration شماره ۱ (`BusinessFoundation_AdminApprovalAndFlatFee`) به‌درستی جدول
   `ad_boost_plans` را در متد `Up()` حذف می‌کند (این همان جایی است که تصمیم محصولی
   "جایگزینی Boost با هزینه ثابت" پیاده‌سازی شد).
2. اما Migration شماره ۴ (`InitialCreate`، با تاریخ *جدیدتر* از شماره ۱ — به دلیل یک
   بازتولید/Squash تاریخچه Migration که در همان بازه انجام شده) دوباره جدول
   `ad_boost_plans` را در متد `Up()` خودش می‌سازد (این migration ظاهراً از روی یک وضعیت مدل
   قدیمی‌تر — پیش از خالی‌شدن `AdBoostPlanConfiguration.cs` — تولید شده است).
3. هیچ‌کدام از Migrationهای بعدی (`AddSupportTickets`, `AddBannerAds`) این جدول را دوباره
   حذف نمی‌کنند (این مورد به‌صراحت بررسی و رد شد — تنها `DropTable` موجود در
   `AddBannerAds` مربوط به متد `Down()` خودِ جدول `banner_ads` است، نه `ad_boost_plans`).

**نتیجه**: روی هر دیتابیسی که این ۶ Migration به ترتیب روی آن اعمال شده باشد (یعنی دیتابیس
فعلی پروژه)، جدول فیزیکی `ad_boost_plans` **هنوز در دیتابیس وجود دارد**، درحالی‌که مدل فعلی
EF (`AppDbContextModelSnapshot.cs`) هیچ اطلاعی از آن ندارد. این یک "Model Drift" بی‌خطر اما
کثیف است: کد هیچ کوئری‌ای به این جدول نمی‌زند (چون Entity آن حذف شده)، ولی جدول در دیتابیس
باقی مانده و در هیچ تاریخچه Migration به‌صورت صریح ردیابی نمی‌شود.

### چرا در همین جلسه اصلاح نشد

ابزار اجرای دستورات شل (هم `mcp__workspace__bash` و هم Desktop Commander) در پایان این
جلسه در دسترس نبودند (خطای اتصال/عدم پشتیبانی محیط). از آنجا که این پروژه در تمام مراحل قبلی
با انضباط "build → migration → تایید مستقیم با psql" پیش رفته، از نوشتن دستی فایل‌های
Migration بدون امکان build/تایید خودداری شد تا خطای غیرقابل‌بازبینی وارد تاریخچه Migration
نشود.

### راه‌حل دقیق (باید در اولین فرصت با دسترسی به شل اجرا شود)

چون Entity مربوطه از مدل حذف شده، دستور `dotnet ef migrations add` به‌تنهایی چیزی برای این
جدول تشخیص نمی‌دهد (تفاوتی در مدل نمی‌بیند). باید یک Migration خالی ساخته و دستی ویرایش شود:

```powershell
dotnet ef migrations add DropOrphanedAdBoostPlansTable `
  --context AppDbContext `
  --project src\IndustrialPlatform.Persistence `
  --startup-project src\IndustrialPlatform.Api
```

سپس در فایل تولیدشده (`..._DropOrphanedAdBoostPlansTable.cs`) به‌صورت دستی:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("DROP TABLE IF EXISTS ad_boost_plans;");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // بازسازی جدول برای rollback (کپی دقیق از ستون‌های تعریف‌شده در InitialCreate)
    migrationBuilder.CreateTable(
        name: "ad_boost_plans",
        columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            duration_days = table.Column<int>(type: "integer", nullable: false),
            price_in_rials = table.Column<long>(type: "bigint", nullable: false),
            is_active = table.Column<bool>(type: "boolean", nullable: false),
            xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
            created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            created_by = table.Column<Guid>(type: "uuid", nullable: true),
            last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
        },
        constraints: table => table.PrimaryKey("pk_ad_boost_plans", x => x.id));
}
```

سپس اجرا و تایید:

```powershell
dotnet ef database update --context AppDbContext --project src\IndustrialPlatform.Persistence --startup-project src\IndustrialPlatform.Api
docker exec industrial-platform-postgres psql -U industrial_platform -d industrial_platform -c "\dt ad_boost_plans"
```

خروجی مرحله آخر باید `Did not find any relation named "ad_boost_plans"` باشد.

> از `migrationBuilder.Sql("DROP TABLE ...")` به‌جای `migrationBuilder.DropTable(...)` استفاده
> شده، چون متد `DropTable` انتظار دارد جدول در مدل/Snapshot تعریف شده باشد؛ برای حذف جدولی که
> از دید EF اصلاً وجود ندارد، ساده‌ترین و امن‌ترین راه SQL خام است.

### فایل‌های Stub مرتبط (پس از اجرای موفق Migration بالا قابل حذف فیزیکی هستند)

- `backend/src/IndustrialPlatform.Domain/Payments/AdBoostPlan.cs`
- `backend/src/IndustrialPlatform.Application/Payments/IAdBoostPlanRepository.cs`
- `backend/src/IndustrialPlatform.Application/Payments/Commands/PurchaseAdBoostCommand.cs`
- `backend/src/IndustrialPlatform.Persistence/Repositories/AdBoostPlanRepository.cs`
- `backend/src/IndustrialPlatform.Persistence/Configurations/AdBoostPlanConfiguration.cs`

توجه: `backend/src/IndustrialPlatform.Application/Payments/Queries/GetActiveAdBoostPlansQuery.cs`
**نباید حذف شود** — این فایل بازنویسی شده و اکنون میزبان `GetJobAdListingFeeQuery` است که در
حال استفاده فعال است (فقط نامش قدیمی مانده).

---

## ۵. نتیجه بررسی کامل رفرنس‌های شکسته (تسک #62 - بخش grep)

بررسی‌های زیر روی کل backend و frontend انجام شد:

- **`grep "AdBoostPlan|Boost"` در backend**: تمام موارد یا کامنت مستندسازی بازنشستگی هستند
  (فایل‌های Stub بالا) یا رفرنس امن به `GetActiveAdBoostPlansQuery.cs` بازنویسی‌شده. هیچ
  رفرنس شکسته/کامپایل‌نشدنی یافت نشد (تایید شده با `dotnet build` بدون خطا).
- **`grep "TODO|FIXME|HACK|XXX"` در backend**: صفر مورد.
- **`grep "TODO|FIXME|HACK|XXX"` در frontend**: یک مورد که False Positive بود
  (`placeholder="APP-XXXXXX"` در `TrackApplicationPage.tsx` — صرفاً فرمت نمونه کد رهگیری، نه یک TODO واقعی).
- **تاریخچه Migration `IdentityDbContext`**: سه Migration، ترتیب و محتوا سالم، بدون رفرنس شکسته.
- **باگ از پیش موجود در `CompanyJobAdsPage.tsx`** (فراخوانی دوباره `.toLocaleString('fa-IR')` روی
  یک رشته از‌قبل‌فرمت‌شده که باعث خطای TS2554 می‌شد) در همین بازه بررسی نهایی پیدا و اصلاح شد.
  با `npx tsc --noEmit` تایید شد که کل فرانت‌اند اکنون بدون خطای TypeScript کامپایل می‌شود.
- **تنها یافته باز**: جدول یتیم `ad_boost_plans` (بخش ۴ بالا) — یک مشکل داده‌ای بی‌خطر
  (بدون تاثیر عملکردی) که راه‌حل دقیق آن مستند شد اما به دلیل عدم دسترسی به ابزار اجرای شل
  در این نشست، اجرا نشده و باید در اولین فرصت اجرا شود.

---

## ۶. جمع‌بندی وضعیت تسک #62

| بخش | وضعیت |
|---|---|
| Grep کل پروژه برای رفرنس‌های شکسته | ✅ کامل — بدون رفرنس شکسته؛ یک باگ جانبی (`CompanyJobAdsPage.tsx`) پیدا و اصلاح شد |
| راهنمای Migration نهایی (این سند) | ✅ کامل |
| اصلاح جدول یتیم `ad_boost_plans` | ⏳ مستندسازی کامل، اجرا در انتظار دسترسی به ابزار شل (بخش ۴) |

تسک #58 (پیام‌رسانی مستقیم کارفرما-کارجو) طبق دستور صریح کاربر همچنان **معلق و اجرا نشده**
باقی می‌ماند.
