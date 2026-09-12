-- =============================================================================
-- seed-test-job-applications.sql
-- اسکریپت تست/توسعه — یک آگهی منتشرشده برای شرکت «شرکت صنعتی فانوس» (حساب اصلی/فعال کاربر —
-- owner_user_id = b1075215-fd92-4929-9499-a750e1a2a564 / username: shindevops) و سه درخواست
-- همکاری با وضعیت‌های متفاوت (Submitted / Reviewed / InterviewScheduled) می‌سازد تا صفحه
-- «مدیریت رزومه‌ها و متقاضیان» (/company/job-ads/:jobAdId/applications) قابل تست باشد.
--
-- طبق فاز «مدیریت رزومه‌ها و متقاضیان» بخش دوم (گسترش دامنه): برای کارجوی تستی سوم (کاملاً synthetic —
-- «علی رضایی»، نه یک حساب واقعی کاربر)، ایمیل/شهر + دو ردیف سابقه تحصیلی و دو ردیف سابقه شغلی
-- ساختاریافته نیز درج می‌شود تا بخش‌های جدید «تحصیلات» و Timeline «سوابق شغلی» صفحه قابل تست باشند.
-- عمداً به کاندیدهای موجود واقعی (candidate1/candidate2) داده جعلی افزوده نمی‌شود.
--
-- اجرا:
--   psql -h localhost -p 5433 -U industrial_platform -d industrial_platform -f seed-test-job-applications.sql
--
-- ایمن برای اجرای مکرر است — شماره موبایل کارجوی سوم هر بار تصادفی تولید می‌شود تا با
-- محدودیت یکتایی ux_users_mobile_number تداخل نکند (هر بار یک آگهی و رکوردهای جدید می‌سازد).
-- فقط برای محیط توسعه/تست.
-- =============================================================================

DO $$
DECLARE
    v_job_ad_id        uuid := gen_random_uuid();
    v_new_user_id      uuid := gen_random_uuid();
    v_new_candidate_id uuid := gen_random_uuid();
    v_new_mobile       text := '0935' || (1000 + floor(random() * 9000))::int::text;
    v_candidate1_id    uuid := '6b2d2287-739f-4aa9-bfb7-f59e2669db4b'; -- شاهین خلیلی (کاندیدای موجود ۱)
    v_candidate2_id    uuid := 'aa071768-2f72-4eef-a8bc-d6c26613e29c'; -- شاهین خلیلی (کاندیدای موجود ۲)
    v_company_id       uuid := '879245cc-2bc1-489a-89a0-74a2e25e220d'; -- شرکت صنعتی فانوس (حساب اصلی کاربر)
    v_zone_id          uuid := '22578e26-df01-401a-972d-4883b8e17b27'; -- شهرک صنعتی شهید سلیمی
    v_now              timestamptz := now();
BEGIN
    -- ۱) یک آگهی منتشرشده تستی برای شرکت اول
    INSERT INTO job_ads (
        id, company_id, industrial_zone_id, title, description, work_shift, has_commute_service,
        meal_plan, insurance_types, contract_type, gender_preference, min_education_level, status,
        is_fee_paid, submitted_for_review_at_utc, published_at_utc, expires_at_utc,
        salary_type, views_count, is_featured, created_at_utc, is_deleted
    ) VALUES (
        v_job_ad_id, v_company_id, v_zone_id, 'اپراتور خط تولید (آگهی تستی)',
        'این آگهی صرفاً برای تست صفحه مدیریت رزومه‌ها و متقاضیان ایجاد شده است.',
        'Rotational2Shift', false, 'LunchOnly', 1, 'Permanent', 'Any', 'Diploma', 'Published',
        true, v_now - interval '3 days', v_now - interval '2 days', v_now + interval '27 days',
        'Agreement', 0, false, v_now - interval '3 days', false
    );

    -- ۲) یک کاربر و کارجوی جدید (سومین متقاضی) برای تنوع بیشتر در وضعیت‌ها
    INSERT INTO users (id, mobile_number, is_active, is_mobile_verified, created_at_utc, is_deleted)
    VALUES (v_new_user_id, v_new_mobile, true, true, v_now, false);

    INSERT INTO candidates (
        id, user_id, mobile_number, full_name, military_service_status, education_level,
        work_experience_summary, skills, is_fee_paid, email, city, created_at_utc, is_deleted
    ) VALUES (
        v_new_candidate_id, v_new_user_id, v_new_mobile, 'علی رضایی', 'Completed', 'کاردانی',
        'سه سال سابقه کار در خط تولید کارخانه فولاد به‌عنوان اپراتور CNC.',
        'اپراتوری CNC, جوشکاری, ایمنی صنعتی', true, 'ali.rezaei.test@example.com', 'تهران', v_now, false
    );

    -- ۲.۱) سوابق تحصیلی ساختاریافته (چند ردیفی) کارجوی تستی سوم — برای تست بخش «تحصیلات» صفحه.
    INSERT INTO candidate_educations (id, candidate_id, degree_level, field_of_study, institution_name, graduation_year, created_at_utc, is_deleted)
    VALUES
        (gen_random_uuid(), v_new_candidate_id, 'کاردانی', 'مکانیک ماشین‌افزار', 'دانشگاه فنی و حرفه‌ای', 1398, v_now, false),
        (gen_random_uuid(), v_new_candidate_id, 'دیپلم', 'ریاضی و فیزیک', 'هنرستان شهید بهشتی', 1394, v_now, false);

    -- ۲.۲) سوابق شغلی ساختاریافته (چند ردیفی، با بازه زمانی) — برای تست Timeline «سوابق شغلی» صفحه.
    INSERT INTO candidate_work_experiences (id, candidate_id, job_title, company_name, start_year, end_year, description, created_at_utc, is_deleted)
    VALUES
        (gen_random_uuid(), v_new_candidate_id, 'اپراتور CNC', 'گروه صنعتی فولاد البرز', 1401, NULL, 'برنامه‌نویسی و راه‌اندازی دستگاه‌های CNC در خط تولید.', v_now, false),
        (gen_random_uuid(), v_new_candidate_id, 'کارگر خط تولید', 'کارخانه قطعات خودرو پارس', 1398, 1401, 'مونتاژ قطعات و کنترل کیفیت اولیه.', v_now, false);

    -- ۳) سه درخواست همکاری با وضعیت‌های مختلف برای همان آگهی
    INSERT INTO job_applications (id, job_ad_id, candidate_id, status, tracking_token, match_score_percent, created_at_utc, is_deleted)
    VALUES
        (gen_random_uuid(), v_job_ad_id, v_candidate1_id, 'Submitted', 'TRK' || substr(md5(random()::text), 1, 8), 72, v_now - interval '2 days', false),
        (gen_random_uuid(), v_job_ad_id, v_candidate2_id, 'Reviewed', 'TRK' || substr(md5(random()::text), 1, 8), 85, v_now - interval '1 days', false),
        (gen_random_uuid(), v_job_ad_id, v_new_candidate_id, 'InterviewScheduled', 'TRK' || substr(md5(random()::text), 1, 8), 90, v_now - interval '5 hours', false);

    RAISE NOTICE 'Seeded job_ad id: %', v_job_ad_id;
END $$;
