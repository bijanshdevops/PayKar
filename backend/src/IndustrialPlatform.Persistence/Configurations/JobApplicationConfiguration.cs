using IndustrialPlatform.Domain.Candidates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("job_applications");
        builder.HasKey(a => a.Id).HasName("pk_job_applications");

        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.JobAdId).HasColumnName("job_ad_id").IsRequired();
        builder.Property(a => a.CandidateId).HasColumnName("candidate_id").IsRequired();
        builder.Property(a => a.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(a => a.TrackingToken).HasColumnName("tracking_token").HasMaxLength(20).IsRequired();
        builder.Property(a => a.MatchScorePercent).HasColumnName("match_score_percent");
        builder.Property(a => a.CompanyNotes).HasColumnName("company_notes");
        builder.Property(a => a.InterviewDateTimeUtc).HasColumnName("interview_date_time_utc");

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(a => a.JobAdId).HasDatabaseName("ix_job_applications_job_ad_id");
        builder.HasIndex(a => a.CandidateId).HasDatabaseName("ix_job_applications_candidate_id");
        builder.HasIndex(a => a.TrackingToken).HasDatabaseName("ux_job_applications_tracking_token").IsUnique();
        builder.HasIndex(a => new { a.CandidateId, a.JobAdId })
            .HasDatabaseName("ux_job_applications_candidate_id_job_ad_id")
            .IsUnique()
            .HasFilter("is_deleted = false");
    }
}
