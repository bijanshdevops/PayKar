# 🗄 استانداردهای پایگاه داده (Database Standards & Guidelines)

| مشخصه | مقدار |
| :--- | :--- |
| **سند** | راهنما و استانداردهای پایگاه داده (`IndustrialPlatform`) |
| **ورژن** | 1.0.0 |
| **ناظر فنی** | شاهین |
| **پایگاه داده** | PostgreSQL 16+ |
| **فریمورک دسترسی** | Entity Framework Core 9 (Fluent API Only) |

---

## ۱. اصول کلی و استانداردهای نام‌گذاری (Naming Conventions)

برای حفظ سازگاری کامل با استانداردهای استاندارد PostgreSQL و جلوگیری از مشکلات مربوط به نقل‌قول‌های دوتایی (`"QuotedIdentifiers"`):

### ۱.۱. جداول و فیلدها
- **جداول (Tables):** به صورت جمع (`Plural`) و با ساختار `snake_case` (مثال: `companies`, `job_ads`, `industrial_zones`, `resumes`).
- **ستون‌ها (Columns):** به صورت مفرد و با ساختار `snake_case` (مثال: `id`, `first_name`, `created_at_utc`, `is_verified`).
- **کلیدهای اصلی (Primary Keys):** همواره ستون `id`.
- **کلیدهای خارجی (Foreign Keys):** نام موجودیت به صورت مفرد به همراه `_id` (مثال: `company_id`, `industrial_zone_id`, `user_id`).

### ۱.۲. ایندکس‌ها و محدودیت‌ها (Constraints & Indexes)
- **کلید اصلی:** `pk_{table_name}` (مثال: `pk_job_ads`)
- **کلید خارجی:** `fk_{table_name}_{foreign_table}_{foreign_column}` (مثال: `fk_job_ads_companies_company_id`)
- **ایندکس ساده / مرکب:** `ix_{table_name}_{columns}` (مثال: `ix_job_ads_company_id_created_at_utc`)
- **ایندکس یکتا:** `ux_{table_name}_{columns}` (مثال: `ux_companies_national_id`)
- **چک کانسترینت:** `ck_{table_name}_{condition_name}` (مثال: `ck_job_ads_salary_min_positive`)

---

## ۲. نگاشت انواع داده (PostgreSQL Data Types Mapping)

| مفهوم سیستمی (C#) | نوع داده در دیتابیس (PostgreSQL) | توضیحات |
| :--- | :--- | :--- |
| `Guid` | `uuid` | کلیدهای اصلی و شناسه‌های سراسری یکتا |
| `string` (طول ثابت / محدود) | `varchar(n)` | برای فیلدهای با طول معین (مثلاً موبایل `varchar(15)`) |
| `string` (متن آزاد / طولانی) | `text` | برای شرح شغل، آدرس‌های طولانی، لاگ‌ها |
| `int` | `integer` | مقادیر عددی متعارف و شمارنده‌ها |
| `long` | `bigint` | ارقام پولی بزرگ و لاگ‌های حجیم |
| `decimal` | `numeric(18,2)` | ارقام مالی دقیق، حقوق و دستمزدها |
| `bool` | `boolean` | فلگ‌های صفر و یکی (`true` / `false`) |
| `DateTime` | `timestamptz` | همواره بر حسب استاندارد زمان جهانی (UTC) |
| `DateOnly` | `date` | صرفاً تاریخ تقویمی (مانند تاریخ تولد یا تاسیس) |
| `TimeOnly` | `time` | ساعت شیفت کاری بدون تاریخ |
| `List<string>` / آرایه‌ها | `jsonb` یا آرایه‌های محلی | متناسب با نیاز به جست‌وجو و ایندکس GIN |

---

## ۳. ستون‌های پایه، حسابرسی و ردگیری (Audit & Base Columns)

تمامی جداول اصلی بیزینس از اینترفیس‌های `IAuditableEntity` و `ISoftDeletable` تبعیت می‌کنند:

| ستون | نوع داده | وضعیت | شرح |
| :--- | :--- | :--- | :--- |
| `id` | `uuid` | NOT NULL (PK) | شناسه منحصر‌به‌فرد رکورد |
| `created_at_utc` | `timestamptz` | NOT NULL | زمان ایجاد به وقت جهانی (UTC) |
| `created_by` | `uuid` | NULL | شناسه کاربری که رکورد را ایجاد کرده |
| `last_modified_at_utc` | `timestamptz` | NULL | زمان آخرین ویرایش (UTC) |
| `last_modified_by` | `uuid` | NULL | شناسه کاربری که آخرین ویرایش را انجام داده |
| `is_deleted` | `boolean` | NOT NULL (Default: false) | فلگ حذف منطقی |
| `deleted_at_utc` | `timestamptz` | NULL | زمان حذف منطقی رکورد |
| `deleted_by` | `uuid` | NULL | شناسه کاربری که رکورد را حذف کرده |
| `xmin` / `row_version` | `uint` | NOT NULL | مدیریت همروندی خوش‌بینانه (Concurrency Token) |

---

## ۴. قواعد حذف منطقی (Soft Delete Rules)

1. **حذف فیزیکی ممنوع:** در محیط‌های عملیاتی، هیچ تراکنش `DELETE` مستقیمی روی جداول بیزینس اجرا نمی‌شود؛ فقط فیلد `is_deleted = true` می‌گردد.
2. **فیلتر سراسری کوئری (Global Query Filter):**  
   تمامی کوئری‌های EF Core به طور خودکار فیلتر `is_deleted == false` را اعمال می‌کنند، مگر در سناریوهای مشخص مدیریتی یا بازیابی که از `IgnoreQueryFilters()` استفاده شود.
3. **ایندکس‌های حساس به حذف:**  
   در تعریف ایندکس‌های منحصر‌به‌فرد، همیشه شرط پارشیال برای حذف منطقی اعمال شود:
   `CREATE UNIQUE INDEX ux_companies_national_id ON companies (national_id) WHERE is_deleted = false;`

---

## ۵. استراتژی ایندکس‌گذاری و کارایی (Indexing Strategy)

- **ایندکس روی تمام کلیدهای خارجی:** تمام ستون‌های Foreign Key باید دارای Index باشند تا کوئری‌های `JOIN` و عملیات بررسی ارجاع دچار اسکن جدول نشوند.
- **ایندکس‌های فیلترشده (Partial Indexes):** برای رکوردهای فعال و رایج، مانند آگهی‌های منتشرشده:
  `CREATE INDEX ix_job_ads_active ON job_ads (created_at_utc DESC) WHERE is_deleted = false AND status = 'Published';`
- **جست‌وجوی متنی پیشرفته (Full-Text Search):** برای جست‌وجوی عناوین شغلی و شهرک‌های صنعتی از اکستنشن `pg_trgm` و ایندکس‌های نوع `GIN` یا `GIST` بر روی ستون‌های متن فارسی استفاده می‌شود.

---

## ۶. قواعد مایگریشن‌ها و تغییرات شمای دیتابیس (Migrations Rules)

1. **انحصاری بودن Fluent API:** استفاده از Data Annotations (مانند `[Required]`, `[MaxLength]`) روی انتیتی‌های Domain اکیداً ممنوع است. تمام تعاریف باید در کلاس‌های `IEntityTypeConfiguration<T>` لایه `Persistence` قرار گیرند.
2. **مایگریشن‌های برگشت‌پذیر (Idempotent):** فایل‌های اسکریپت مایگریشن برای اجرا در پایپ‌لاین‌های CI/CD باید با سویچ `--idempotent` تولید شوند.
3. **داده‌های اولیه (Seeding):** داده‌های پایه‌ای ثابت (مانند لیست استان‌ها، زون‌های صنعتی پایه، نقش‌ها و پرمیشن‌ها) از طریق متدهای Seed مشخص در لایه Persistence اعمال می‌شوند.
