using IndustrialPlatform.Domain.Candidates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class CandidateWorkExperienceConfiguration : IEntityTypeConfiguration<CandidateWorkExperience>
{
    public void Configure(EntityTypeBuilder<CandidateWorkExperience> builder)
    {
        builder.ToTable("candidate_work_experiences");
        builder.HasKey(w => w.Id).HasName("pk_candidate_work_experiences");

        builder.Property(w => w.Id).HasColumnName("id");
        builder.Property(w => w.CandidateId).HasColumnName("candidate_id").IsRequired();
        builder.Property(w => w.JobTitle).HasColumnName("job_title").HasMaxLength(150).IsRequired();
        builder.Property(w => w.CompanyName).HasColumnName("company_name").HasMaxLength(200).IsRequired();
        builder.Property(w => w.StartYear).HasColumnName("start_year").IsRequired();
        builder.Property(w => w.EndYear).HasColumnName("end_year");
        builder.Property(w => w.Description).HasColumnName("description").HasColumnType("text");

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(w => w.CandidateId).HasDatabaseName("ix_candidate_work_experiences_candidate_id");
    }
}
