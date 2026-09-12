using IndustrialPlatform.Domain.Candidates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.ToTable("candidates");
        builder.HasKey(c => c.Id).HasName("pk_candidates");

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(c => c.MobileNumber).HasColumnName("mobile_number").HasMaxLength(15).IsRequired();
        builder.Property(c => c.FullName).HasColumnName("full_name").HasMaxLength(150).IsRequired();
        builder.Property(c => c.MilitaryServiceStatus).HasColumnName("military_service_status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(c => c.EducationLevel).HasColumnName("education_level").HasMaxLength(100).IsRequired();
        builder.Property(c => c.WorkExperienceSummary).HasColumnName("work_experience_summary").HasColumnType("text");
        builder.Property(c => c.Skills).HasColumnName("skills").HasColumnType("text");
        builder.Property(c => c.Interests).HasColumnName("interests").HasColumnType("text");
        builder.Property(c => c.PsychologyAnswers).HasColumnName("psychology_answers").HasColumnType("text");
        builder.Property(c => c.ResumeFileUrl).HasColumnName("resume_file_url").HasMaxLength(500);
        builder.Property(c => c.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(500);
        // طبق فاز «مدیریت رزومه‌ها و متقاضیان» — فیلدهای اختیاری تماس/محل سکونت.
        builder.Property(c => c.Email).HasColumnName("email").HasMaxLength(255);
        builder.Property(c => c.City).HasColumnName("city").HasMaxLength(150);
        // طبق ADR-013: هزینه ساخت رزومه. مقدار پیش‌فرض ستون true است تا رزومه‌های موجودِ ساخته‌شده پیش از
        // این تصمیم محصولی، به‌صورت گذشته‌نگر «پرداخت‌شده» تلقی شوند؛ رکوردهای جدید همیشه مقدار صریح از Domain می‌گیرند.
        builder.Property(c => c.IsFeePaid).HasColumnName("is_fee_paid").IsRequired().HasDefaultValue(true);

        // طبق فاز «پروفایل و رزومه‌ساز کارجو».
        builder.Property(c => c.JobTitle).HasColumnName("job_title").HasMaxLength(150);
        builder.Property(c => c.ProfessionalSummary).HasColumnName("professional_summary").HasColumnType("text");
        builder.Property(c => c.LinkedInUrl).HasColumnName("linkedin_url").HasMaxLength(300);
        builder.Property(c => c.GitHubUrl).HasColumnName("github_url").HasMaxLength(300);
        builder.Property(c => c.PersonalWebsiteUrl).HasColumnName("personal_website_url").HasMaxLength(300);
        builder.Property(c => c.PreferredWorkType).HasColumnName("preferred_work_type").HasConversion<string>().HasMaxLength(30);
        // طبق دستور صریح این فاز: واحد تومان (نه ریال) — ر.ک. یادداشت طراحی در Candidate.MinRequestedSalaryInToman.
        builder.Property(c => c.MinRequestedSalaryInToman).HasColumnName("min_requested_salary_in_toman");
        builder.Property(c => c.MaxRequestedSalaryInToman).HasColumnName("max_requested_salary_in_toman");
        builder.Property(c => c.IsActivelyLookingForJob).HasColumnName("is_actively_looking_for_job").IsRequired().HasDefaultValue(true);

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(c => c.UserId).HasDatabaseName("ux_candidates_user_id").IsUnique().HasFilter("is_deleted = false");
    }
}
