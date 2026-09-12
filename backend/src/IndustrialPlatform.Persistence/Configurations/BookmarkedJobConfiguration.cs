using IndustrialPlatform.Domain.Candidates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class BookmarkedJobConfiguration : IEntityTypeConfiguration<BookmarkedJob>
{
    public void Configure(EntityTypeBuilder<BookmarkedJob> builder)
    {
        builder.ToTable("bookmarked_jobs");
        builder.HasKey(b => b.Id).HasName("pk_bookmarked_jobs");

        builder.Property(b => b.Id).HasColumnName("id");
        builder.Property(b => b.CandidateId).HasColumnName("candidate_id").IsRequired();
        builder.Property(b => b.JobAdId).HasColumnName("job_ad_id").IsRequired();

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(b => b.CandidateId).HasDatabaseName("ix_bookmarked_jobs_candidate_id");
        builder.HasIndex(b => b.JobAdId).HasDatabaseName("ix_bookmarked_jobs_job_ad_id");
        builder.HasIndex(b => new { b.CandidateId, b.JobAdId })
            .HasDatabaseName("ux_bookmarked_jobs_candidate_id_job_ad_id")
            .IsUnique()
            .HasFilter("is_deleted = false");
    }
}
