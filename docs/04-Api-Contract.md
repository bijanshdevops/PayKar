# 04. قرارداد و استانداردهای API (API Contract & Standards)

| مشخصه | مقدار |
| :--- | :--- |
| **سند** | قرارداد API پلتفرم آگهی‌های صنعتی (`IndustrialPlatform`) |
| **ورژن** | 2.0.0 (بازنویسی کامل برای هم‌راستایی با دامنه واقعی پروژه) |
| **پروتکل** | RESTful HTTP/S — JSON |
| **مسیر پایه** | `/api/v1` |

---

## ۱. اصول کلی

- تمام درخواست‌ها و پاسخ‌ها با فرمت `application/json` (UTF-8) هستند.
- نسخه‌بندی API از طریق مسیر (`/api/v1`, `/api/v2`) انجام می‌شود، نه Header.
- تاریخ/زمان همواره در پاسخ‌ها به فرمت ISO-8601 و UTC برگردانده می‌شود (`2026-08-29T10:15:00Z`).
- شناسه‌های موجودیت‌ها از نوع `Guid` (رشته UUID) هستند.
- احراز هویت با هدر `Authorization: Bearer {accessToken}`.

---

## ۲. پاکت پاسخ استاندارد (Standard Response Envelope)

طبق مانیفست معماری (`CLAUDE.md`)، تمام پاسخ‌های API — چه موفق و چه ناموفق — در قالب `ApiResponse<T>` زیر بازگردانده می‌شوند:

```json
{
  "success": true,
  "statusCode": 200,
  "message": "عملیات با موفقیت انجام شد.",
  "data": { },
  "errors": null
}
```

### حالت خطا:
```json
{
  "success": false,
  "statusCode": 404,
  "message": "شرکت مورد نظر یافت نشد.",
  "data": null,
  "errors": {
    "code": ["COMPANY_NOT_FOUND"]
  }
}
```

| فیلد | نوع | توضیح |
| :--- | :--- | :--- |
| `success` | boolean | نتیجه کلی عملیات |
| `statusCode` | int | معادل HTTP Status Code |
| `message` | string | پیام قابل‌نمایش به کاربر (فارسی) |
| `data` | object/array/null | محتوای اصلی پاسخ در حالت موفق |
| `errors` | object/null | نگاشت خطاهای ولیدیشن/بیزینسی به‌ازای هر فیلد یا کد خطا |

> این ساختار مستقیماً محصول `Result<T>` لایه Application است که در لایه Api به `ApiResponse<T>` نگاشت می‌شود؛ کنترلرها هرگز نباید Exception پرتاب کنند تا این پاکت شکسته شود.

---

## ۳. صفحه‌بندی، فیلتر و مرتب‌سازی (Pagination, Filtering, Sorting)

اندپوینت‌های لیستی (مثل جست‌وجوی آگهی‌ها) از پارامترهای Query زیر پیروی می‌کنند:

| پارامتر | نوع | پیش‌فرض | توضیح |
| :--- | :--- | :--- | :--- |
| `page` | int | 1 | شماره صفحه |
| `pageSize` | int | 20 (حداکثر 100) | تعداد آیتم در هر صفحه |
| `sortBy` | string | `createdAtUtc` | فیلد مرتب‌سازی |
| `sortDir` | string | `desc` | `asc` یا `desc` |

پاسخ‌های صفحه‌بندی‌شده در `data` ساختار زیر را دارند:
```json
{
  "items": [ ],
  "totalCount": 134,
  "page": 1,
  "pageSize": 20,
  "totalPages": 7
}
```

---

## ۴. کدهای خطای استاندارد (Error Code Registry)

| errors.code | HTTP Status | معنی |
| :--- | :--- | :--- |
| `VALIDATION_ERROR` | 400 | ورودی نامعتبر (FluentValidation) |
| `UNAUTHORIZED` | 401 | توکن نامعتبر یا منقضی |
| `FORBIDDEN` | 403 | عدم دسترسی کافی (نقش/مالکیت) |
| `COMPANY_NOT_FOUND` | 404 | شرکت یافت نشد |
| `JOB_AD_NOT_FOUND` | 404 | آگهی یافت نشد |
| `CANDIDATE_NOT_FOUND` | 404 | کارجو یافت نشد |
| `DUPLICATE_NATIONAL_ID` | 409 | شناسه ملی شرکت تکراری |
| `JOB_AD_NOT_PUBLISHABLE` | 409 | آگهی در وضعیتی نیست که قابل انتشار باشد |
| `OTP_INVALID_OR_EXPIRED` | 400 | کد یکبارمصرف نادرست یا منقضی |
| `RATE_LIMIT_EXCEEDED` | 429 | عبور از سقف مجاز درخواست |
| `INTERNAL_ERROR` | 500 | خطای پیش‌بینی‌نشده سیستمی |

---

## ۵. نمونه اندپوینت‌ها

### ۵.۱. احراز هویت — درخواست OTP
`POST /api/v1/auth/otp/request`
```json
{ "mobileNumber": "09121234567" }
```
پاسخ موفق (200):
```json
{
  "success": true,
  "statusCode": 200,
  "message": "کد تایید ارسال شد.",
  "data": { "expiresInSeconds": 120 },
  "errors": null
}
```

### ۵.۲. احراز هویت — تایید OTP و صدور توکن
`POST /api/v1/auth/otp/verify`
```json
{ "mobileNumber": "09121234567", "otpCode": "482913" }
```
پاسخ موفق (200):
```json
{
  "success": true,
  "statusCode": 200,
  "message": "ورود موفقیت‌آمیز بود.",
  "data": {
    "accessToken": "eyJhbGciOi...",
    "refreshToken": "8f14e45f...",
    "expiresInSeconds": 900,
    "user": { "id": "9b1d...", "mobileNumber": "09121234567", "roles": ["CompanyManager"] }
  },
  "errors": null
}
```

### ۵.۳. ایجاد پروفایل شرکت
`POST /api/v1/companies`
```json
{
  "name": "صنایع فلزی البرز",
  "nationalId": "10101234567",
  "registrationNumber": "45872",
  "industrialZoneId": "3a1e2c9e-....",
  "addressDetail": "بلوار اصلی، فاز ۲، قطعه ۱۴",
  "industryCategory": "ریخته‌گری"
}
```
پاسخ موفق (201):
```json
{
  "success": true,
  "statusCode": 201,
  "message": "شرکت با موفقیت ثبت شد و در انتظار احراز هویت است.",
  "data": {
    "id": "761d7632-475b-4c12-881a-96696b01050a",
    "name": "صنایع فلزی البرز",
    "verificationStatus": "PendingVerification"
  },
  "errors": null
}
```

### ۵.۴. جست‌وجوی آگهی‌های شغلی با فیلترهای صنعتی
`GET /api/v1/job-ads?industrialZoneId={id}&workShift=Rotational2Shift&hasCommuteService=true&page=1&pageSize=20`

پاسخ موفق (200):
```json
{
  "success": true,
  "statusCode": 200,
  "message": null,
  "data": {
    "items": [
      {
        "id": "b12c...",
        "title": "اپراتور CNC",
        "companyName": "صنایع فلزی البرز",
        "industrialZoneName": "شهرک صنعتی اشتهارد",
        "workShift": "Rotational2Shift",
        "hasCommuteService": true,
        "salaryRangeType": "Range",
        "status": "Published",
        "publishedAtUtc": "2026-08-20T08:00:00Z"
      }
    ],
    "totalCount": 42,
    "page": 1,
    "pageSize": 20,
    "totalPages": 3
  },
  "errors": null
}
```

### ۵.۵. ارسال رزومه برای یک آگهی
`POST /api/v1/job-ads/{jobAdId}/applications`
```json
{ "resumeId": "d4e5f6..." }
```
پاسخ موفق (201):
```json
{
  "success": true,
  "statusCode": 201,
  "message": "درخواست شما ثبت شد.",
  "data": {
    "id": "f9a1...",
    "status": "Submitted",
    "trackingToken": "APP-9F2K7Q"
  },
  "errors": null
}
```

### ۵.۶. خطای نمونه — ولیدیشن ناموفق
`POST /api/v1/companies` با شناسه ملی نامعتبر:
```json
{
  "success": false,
  "statusCode": 400,
  "message": "اطلاعات ورودی نامعتبر است.",
  "data": null,
  "errors": {
    "nationalId": ["شناسه ملی باید ۱۰ یا ۱۱ رقم باشد."]
  }
}
```

---

## ۶. هدرهای اجباری

| هدر | اجباری | توضیح |
| :--- | :--- | :--- |
| `Authorization` | برای اندپوینت‌های محافظت‌شده | `Bearer {accessToken}` |
| `Accept-Language` | خیر (پیش‌فرض `fa-IR`) | برای پیام‌های چندزبانه در آینده |
| `Idempotency-Key` | برای عملیات پرداخت (ZarinPal) | جلوگیری از ثبت تکراری تراکنش |

---

## ۷. Health Check Endpoints

طبق سند ۰۱-معماری، این دو اندپوینت خارج از پاکت استاندارد و بدون احراز هویت پاسخ می‌دهند (مصرف‌کننده: Kubernetes Probes):

- `GET /health/live` → `200 OK` در صورت زنده بودن پروسه.
- `GET /health/ready` → `200 OK` در صورت برقراری اتصال به PostgreSQL و Redis، در غیر این صورت `503`.
