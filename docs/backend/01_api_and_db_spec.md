# 🛠️ Backend Architecture, DB Schema & API Specification (`01_backend_api_and_db.md`)

## ۱. اصول معماری و قراردادهای فنی (Core Guidelines)
- **معماری پیشنهادی:** Clean / Hexagonal Architecture همراه با الگوی **CQRS (MediatR)**.
- **پایگاه داده:** PostgreSQL 16+ به همراه **Entity Framework Core**.
- **فرمت تبادل داده:** JSON راست‌چین و استاندارد کمل‌کیس (`camelCase`)، خروجی تاریخ‌ها به فرمت ISO-8601 UTC و تبدیل به شمسی در لایه فرانت‌اند/DTO.
- **احراز هویت و دسترسی:** JWT Bearer Token بر پایه نقش‌ها (`Admin`, `Employer`, `JobSeeker`).
- **واحد پول:** تومان (Toman) برای یکپارچگی با درگاه زرین‌پال و بیزینس مدل (ثبت آگهی: ۵۰,۰۰۰ تومان | ساخت رزومه: ۲۵,۰۰۰ تومان).

---

## ۲. مدل داده‌ای و موجودیت‌های دیتابیس (PostgreSQL ERD Schema)

### ۲.۱. جدول کاربران و احراز هویت (`Users`)
نگهداری اطلاعات هویتی پایه برای لاگین با کد تایید پیامکی (OTP - ملی‌پیامک).
```sql
CREATE TABLE "Users" (
"Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
"PhoneNumber" VARCHAR(15) NOT NULL UNIQUE,
"Role" VARCHAR(20) NOT NULL, -- 'Admin', 'Employer', 'JobSeeker'
"IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
"CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
"UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);
CREATE INDEX "IX_Users_PhoneNumber" ON "Users" ("PhoneNumber");
۲.۲. جدول شرکت‌ها / کارفرمایان (Companies)
پروفایل حقوقی کارخانه/شرکت واقع در شهرک‌های صنعتی.

sql
CREATE TABLE "Companies" (
"Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
"UserId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
"Name" VARCHAR(150) NOT NULL,
"LogoUrl" VARCHAR(500),
"IndustrialZone" VARCHAR(100) NOT NULL, -- نام شهرک صنعتی (مثلاً شمس‌آباد، خزر)
"Province" VARCHAR(50) NOT NULL,
"City" VARCHAR(50) NOT NULL,
"Address" TEXT,
"Phone" VARCHAR(20),
"IsVerified" BOOLEAN NOT NULL DEFAULT FALSE, -- تایید ادمین برای کارفرما
"WalletBalance" NUMERIC(12, 0) NOT NULL DEFAULT 0, -- مانده کیف پول به تومان
"CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);
CREATE INDEX "IX_Companies_IndustrialZone" ON "Companies" ("IndustrialZone");
۲.۳. جدول آگهی‌های استخدام (Jobs)
ثبت آگهی‌ها با هزینه ۵۰ هزار تومان و نیازمند تایید ادمین.

sql
CREATE TYPE job_status AS ENUM ('Pending', 'Approved', 'Rejected', 'Expired');

CREATE TABLE "Jobs" (
"Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
"CompanyId" UUID NOT NULL REFERENCES "Companies"("Id") ON DELETE CASCADE,
"Title" VARCHAR(150) NOT NULL,
"Category" VARCHAR(100) NOT NULL, -- تراشکاری، برق صنعتی، کنترل کیفیت، HSE
"IndustrialZone" VARCHAR(100) NOT NULL,
"Description" TEXT NOT NULL,
"Requirements" TEXT[], -- آرایه رشته‌ای مهارت‌ها ['CNC', 'SolidWorks']
"RequiredCount" INT NOT NULL DEFAULT 1, -- تعداد نیروی مورد نیاز
"Status" job_status NOT NULL DEFAULT 'Pending',
"IsFeatured" BOOLEAN NOT NULL DEFAULT FALSE, -- آگهی ویژه (جهت اسلایدر صفحه اصلی)
"ViewsCount" INT NOT NULL DEFAULT 0,
"ExpiresAt" TIMESTAMP WITH TIME ZONE NOT NULL,
"CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);
CREATE INDEX "IX_Jobs_Status_IsFeatured" ON "Jobs" ("Status", "IsFeatured");
CREATE INDEX "IX_Jobs_IndustrialZone" ON "Jobs" ("IndustrialZone");
۲.۴. جدول کارجویان و رزومه (JobSeekers & Resumes)
نگهداری رزومه آنلاین ۲۵ هزار تومانی و مشخصات کارجو.

sql
CREATE TABLE "JobSeekers" (
"Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
"UserId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
"FullName" VARCHAR(100) NOT NULL,
"AvatarUrl" VARCHAR(500),
"JobTitle" VARCHAR(100), -- عنوان شغلی کارجو (مهندس مکانیک)
"City" VARCHAR(50),
"HasPaidResume" BOOLEAN NOT NULL DEFAULT FALSE, -- پرداخت ۲۵ هزار تومان
"ResumeCompletionPercentage" INT NOT NULL DEFAULT 0, -- درصد تکمیل رزومه (مثلاً ۸۵٪)
"CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE TABLE "Resumes" (
"Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
"JobSeekerId" UUID NOT NULL UNIQUE REFERENCES "JobSeekers"("Id") ON DELETE CASCADE,
"AboutMe" TEXT,
"Experiences" JSONB DEFAULT '[]', -- لیست سوابق کاری
"Educations" JSONB DEFAULT '[]', -- سوابق تحصیلی
"Skills" TEXT[], -- آرایه مهارت‌ها
"Languages" TEXT[],
"AttachmentUrl" VARCHAR(500), -- فایل رزومه آپلود شده
"UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);
۲.۵. جدول ارسال درخواست‌ها (JobApplications)
پیوند بین کارجو و آگهی کارفرما.

sql
CREATE TYPE application_status AS ENUM ('Pending', 'Viewed', 'Interview', 'Rejected');

CREATE TABLE "JobApplications" (
"Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
"JobId" UUID NOT NULL REFERENCES "Jobs"("Id") ON DELETE CASCADE,
"JobSeekerId" UUID NOT NULL REFERENCES "JobSeekers"("Id") ON DELETE CASCADE,
"Status" application_status NOT NULL DEFAULT 'Pending',
"MatchScore" INT DEFAULT 0, -- درصد تطابق رزومه با آگهی (مثلاً ۸۷٪)
"CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
CONSTRAINT "UQ_Job_JobSeeker" UNIQUE ("JobId", "JobSeekerId")
);
CREATE INDEX "IX_JobApplications_JobId" ON "JobApplications" ("JobId");
CREATE INDEX "IX_JobApplications_JobSeekerId" ON "JobApplications" ("JobSeekerId");
۲.۶. جدول مالی و تراکنش‌ها (Transactions)
مدیریت پرداخت‌های ۵۰ تومانی آگهی و ۲۵ تومانی رزومه از طریق زرین‌پال.

sql
CREATE TYPE payment_status AS ENUM ('Initiated', 'Completed', 'Failed');
CREATE TYPE payment_for AS ENUM ('JobPostFee', 'ResumeCreationFee', 'WalletTopUp');

CREATE TABLE "Transactions" (
"Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
"UserId" UUID NOT NULL REFERENCES "Users"("Id"),
"Amount" NUMERIC(12, 0) NOT NULL, -- ۵۰,۰۰۰ یا ۲۵,۰۰۰
"Status" payment_status NOT NULL DEFAULT 'Initiated',
"For" payment_for NOT NULL,
"TargetEntityId" UUID, -- شناسه آگهی یا رزومه مربوطه
"Authority" VARCHAR(100), -- کد تراکنش زرین‌پال
"RefId" VARCHAR(100), -- شماره پیگیری بعد از پرداخت
"CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);
۲.۷. جدول آگهی‌های نشان‌شده (BookmarkedJobs)
sql
CREATE TABLE "BookmarkedJobs" (
"Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
"JobSeekerId" UUID NOT NULL REFERENCES "JobSeekers"("Id") ON DELETE CASCADE,
"JobId" UUID NOT NULL REFERENCES "Jobs"("Id") ON DELETE CASCADE,
"CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
CONSTRAINT "UQ_Bookmark_User_Job" UNIQUE ("JobSeekerId", "JobId")
);

-- ۲.۸. جداول سیستم تیکتینگ و پیام‌ها (Ticketing System)

CREATE TYPE ticket_status AS ENUM ('Open', 'PendingUser', 'PendingAdmin', 'Closed');
CREATE TYPE ticket_priority AS ENUM ('Low', 'Medium', 'High', 'Critical');
CREATE TYPE ticket_department AS ENUM ('Technical', 'Financial', 'Verification', 'General');

CREATE TABLE "Tickets" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "TrackingCode" VARCHAR(12) NOT NULL UNIQUE, -- کد پیگیری تیکت (مثلاً TCK-140301-89)
    "Subject" VARCHAR(200) NOT NULL,
    "Department" ticket_department NOT NULL DEFAULT 'General',
    "Priority" ticket_priority NOT NULL DEFAULT 'Medium',
    "Status" ticket_status NOT NULL DEFAULT 'Open',
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);
CREATE INDEX "IX_Tickets_UserId" ON "Tickets" ("UserId");
CREATE INDEX "IX_Tickets_Status" ON "Tickets" ("Status");

CREATE TABLE "TicketMessages" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TicketId" UUID NOT NULL REFERENCES "Tickets"("Id") ON DELETE CASCADE,
    "SenderUserId" UUID NOT NULL REFERENCES "Users"("Id"), -- کاربر یا ادمین پاسخ‌دهنده
    "IsAdminReply" BOOLEAN NOT NULL DEFAULT FALSE,
    "Message" TEXT NOT NULL,
    "AttachmentUrls" TEXT[] DEFAULT '{}', -- پشتیبانی از آپلود اسکرین‌شات و فاکتور
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);
CREATE INDEX "IX_TicketMessages_TicketId" ON "TicketMessages" ("TicketId");



۳. مستندات قرارداد API (RESTful Endpoints)
۳.۱. ماژول احراز هویت و ثبت‌نام (/api/v1/auth)
POST /api/v1/auth/send-otp: ارسال کد پیامکی (ملی‌پیامک).
Body: { "phoneNumber": "09123456789" }
POST /api/v1/auth/verify-otp: تایید کد و صدور توکن.
Body: { "phoneNumber": "09123456789", "code": "12345", "role": "Employer" }
Response: { "token": "jwt_token", "user": { "id": "...", "role": "Employer" } }
۳.۲. ماژول صفحه اصلی و جستجو (/api/v1/public)
GET /api/v1/public/home: داده‌های صفحه لندینگ شامل آگهی‌های ویژه، کاروسل و شمارنده‌ها.
Response:
json
{
"platformStats": {
"activeJobs": 10500,
"activeCompanies": 2340,
"industrialZones": 85,
"jobSeekers": 28750
},
"featuredJobs": [ ... ],
"bannerAds": [ ... ],
"latestNews": [ ... ]
}

GET /api/v1/public/jobs: جستجوی چندفیلتره (Query Params: q, zone, category, page, pageSize).
۳.۳. ماژول کارفرما (/api/v1/employer) [Authorize(Roles = “Employer”)]
GET /api/v1/employer/dashboard: دریافت داده‌های کارت‌های آماری و گزارش‌ها.
Response:
json
{
"activeJobsCount": 12,
"totalViews": 8473,
"newApplicationsCount": 36,
"walletBalance": 2450000,
"recentApplicants": [
{ "id": "...", "fullName": "سعید محمدی", "jobTitle": "مهندس مکانیک", "matchScore": 87, "appliedAt": "2024-05-20T10:00:00Z" }
],
"activeJobs": [ ... ],
"viewsStatsChart": [ { "date": "1403/03/01", "views": 150 } ],
"jobsShareDonut": [ { "title": "تکنسین برق", "percentage": 30 } ]
}

POST /api/v1/employer/jobs: ثبت آگهی جدید (تولید فاکتور ۵۰ هزار تومانی یا کسر از کیف پول).
Body: { "title": "...", "category": "...", "zone": "صفادشت", "requiredCount": 2, "requirements": ["CNC"] }
۳.۴. ماژول کارجو (/api/v1/jobseeker) [Authorize(Roles = “JobSeeker”)]
GET /api/v1/jobseeker/dashboard:
Response:
json
{
"sentApplicationsCount": 12,
"reviewedByCompanyCount": 8,
"resumeViewsCount": 46,
"activeTicketsCount": 3,
"resumeCompletionPercentage": 85,
"hasPaidResume": true,
"recentApplications": [
{ "jobId": "...", "companyName": "صنایع غذایی بهروز", "zone": "خزر", "status": "Pending", "appliedAt": "1403/02/28" }
],
"suggestedJobs": [ ... ]
}

POST /api/v1/jobseeker/apply: ارسال رزومه به یک آگهی شغلی.
Body: { "jobId": "uuid" }
PUT /api/v1/jobseeker/resume: ویرایش اطلاعات رزومه آنلاین.
POST /api/v1/jobseeker/bookmarks/{jobId}: نشان کردن / حذف از نشان‌شده‌ها (Toggle).
۳.۵. ماژول پرداخت زرین‌پال (/api/v1/payments)
POST /api/v1/payments/request: ایجاد شناسه پرداخت (Authority) برای ۵۰ تومانی آگهی یا ۲۵ تومانی رزومه.
GET /api/v1/payments/verify: کال‌بک زرین‌پال برای تایید تراکنش و فعال‌سازی آگهی/رزومه.

### ۳.۶. ماژول تیکتینگ و ارتباط با پشتیبانی (`/api/v1/tickets`) [Authorize]

- `GET /api/v1/tickets`: دریافت لیست تیکت‌های کاربر جاری (با قابلیت فیلتر بر اساس `status` و صفحه‌بندی).
  - Response:
```json
{
"items": [
{
"id": "uuid",
"trackingCode": "TCK-8921",
"subject": "مشکل در تایید آگهی تراشکار CNC",
"department": "Verification",
"status": "PendingAdmin",
"priority": "High",
"lastReplyAt": "2024-05-20T12:30:00Z",
"createdAt": "2024-05-20T10:00:00Z"
}
],
"totalCount": 1
}

POST /api/v1/tickets: ارسال تیکت جدید به Owner/Admin.
Body:
json
{
"subject": "عدم فعال شدن رزومه پس از پرداخت زرین‌پال",
"department": "Financial",
"priority": "High",
"message": "مبلغ ۲۵ هزار تومان کسر شد ولی وضعیت رزومه تغییر نکرد. کد پیگیری پیوست شد.",
"attachmentUrls": ["https://cdn.khazar.ir/uploads/receipt-123.png"]
}

GET /api/v1/tickets/{ticketId}: مشاهده جزییات یک تیکت و تمام پیام‌ها/پاسخ‌های رد و بدل شده (Chat History).

POST /api/v1/tickets/{ticketId}/reply: ارسال پاسخ جدید برای تیکت باز.

Body:
json
{
"message": "تصویر رسید مجدداً ارسال شد.",
"attachmentUrls": []
}

PATCH /api/v1/tickets/{ticketId}/close: بستن تیکت توسط کاربر یا ادمین.
۳.۷. ماژول ادمین برای مدیریت تیکت‌ها (/api/v1/admin/tickets) [Authorize(Roles = “Admin”)]
GET /api/v1/admin/tickets: مشاهده تمام تیکت‌های پلتفرم با فیلتر نقش کاربر (Employer / JobSeeker)، اولویت و دپارتمان.
POST /api/v1/admin/tickets/{ticketId}/reply: ارسال پاسخ رسمی پشتیبانی توسط Owner/Admin.
PATCH /api/v1/admin/tickets/{ticketId}/status: تغییر دستی وضعیت تیکت.