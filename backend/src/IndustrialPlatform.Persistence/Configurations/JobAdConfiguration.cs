using IndustrialPlatform.Domain.JobAds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class JobAdConfiguration : IEntityTypeConfiguration<JobAd>
{
    public void Configure(EntityTypeBuilder<JobAd> builder)
    {
        builder.ToTable("job_ads");
        builder.HasKey(j => j.Id).HasName("pk_job_ads");

        builder.Property(j => j.Id).HasColumnName("id");
        builder.Property(j => j.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(j => j.IndustrialZoneId).HasColumnName("industrial_zone_id").IsRequired();
        builder.Property(j => j.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(j => j.Description).HasColumnName("description").HasColumnType("text").IsRequired();
        builder.Property(j => j.WorkShift).HasColumnName("work_shift").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(j => j.HasCommuteService).HasColumnName("has_commute_service").IsRequired();
        builder.Property(j => j.CommuteServiceRoutes).HasColumnName("commute_service_routes").HasColumnType("text");
        builder.Property(j => j.MealPlan).HasColumnName("meal_plan").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(j => j.InsuranceTypes).HasColumnName("insurance_types").HasConversion<int>().IsRequired();

        // ---------- فیلدهای تکمیلی استخدام ----------
        builder.Property(j => j.ContractType).HasColumnName("contract_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(j => j.GenderPreference).HasColumnName("gender_preference").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(j => j.MinAge).HasColumnName("min_age");
        builder.Property(j => j.MaxAge).HasColumnName("max_age");
        builder.Property(j => j.MinEducationLevel).HasColumnName("min_education_level").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(j => j.MinExperienceYears).HasColumnName("min_experience_years");
        builder.Property(j => j.MilitaryServiceStatus).HasColumnName("military_service_status").HasConversion<string>().HasMaxLength(50);
        builder.Property(j => j.HeadcountNeeded).HasColumnName("headcount_needed");
        builder.Property(j => j.ApplicationDeadlineUtc).HasColumnName("application_deadline_utc");
        builder.Property(j => j.RequiredSkills).HasColumnName("required_skills").HasColumnType("text");
        builder.Property(j => j.AdditionalBenefits).HasColumnName("additional_benefits").HasColumnType("text");

        // ---------- جریان تایید ادمین و پرداخت ----------
        builder.Property(j => j.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(j => j.IsFeePaid).HasColumnName("is_fee_paid").HasDefaultValue(false).IsRequired();
        builder.Property(j => j.RejectionReason).HasColumnName("rejection_reason").HasColumnType("text");
        builder.Property(j => j.SubmittedForReviewAtUtc).HasColumnName("submitted_for_review_at_utc");
        builder.Property(j => j.PublishedAtUtc).HasColumnName("published_at_utc");
        builder.Property(j => j.ExpiresAtUtc).HasColumnName("expires_at_utc");

        // ---------- بازدید واقعی و فلگ ویژهٔ ادمین (بدون پلن پرداختی) ----------
        builder.Property(j => j.ViewsCount).HasColumnName("views_count").IsRequired().HasDefaultValue(0);
        builder.Property(j => j.IsFeatured).HasColumnName("is_featured").IsRequired().HasDefaultValue(false);

        builder.OwnsOne(j => j.SalaryRange, salary =>
        {
            salary.Property(s => s.Type).HasColumnName("salary_type").HasConversion<string>().HasMaxLength(50).IsRequired();
            salary.Property(s => s.FixedAmount).HasColumnName("salary_fixed_amount").HasColumnType("numeric(18,2)");
            salary.Property(s => s.MinAmount).HasColumnName("salary_min_amount").HasColumnType("numeric(18,2)");
            salary.Property(s => s.MaxAmount).HasColumnName("salary_max_amount").HasColumnType("numeric(18,2)");
        });
        builder.Navigation(j => j.SalaryRange).IsRequired();

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(j => j.CompanyId).HasDatabaseName("ix_job_ads_company_id");
        builder.HasIndex(j => j.IndustrialZoneId).HasDatabaseName("ix_job_ads_industrial_zone_id");
        builder.HasIndex(j => j.Status).HasDatabaseName("ix_job_ads_status");
        builder.HasIndex(j => j.IsFeatured).HasDatabaseName("ix_job_ads_is_featured");

        // ایندکس فیلترشده برای آگهی‌های فعال (سند 03-Database-Standards.md بخش ۵)
        builder.HasIndex(j => j.CreatedAtUtc)
            .HasDatabaseName("ix_job_ads_active")
            .HasFilter("is_deleted = false AND status = 'Published'");

        // یادداشت: ایندکس GIN/pg_trgm روی title برای جست‌وجوی فازی فارسی (ADR-004) در Migration
        // اختصاصی با SQL خام اضافه می‌شود.
    }
}
