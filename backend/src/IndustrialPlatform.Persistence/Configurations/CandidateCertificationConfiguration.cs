using IndustrialPlatform.Domain.Candidates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class CandidateCertificationConfiguration : IEntityTypeConfiguration<CandidateCertification>
{
    public void Configure(EntityTypeBuilder<CandidateCertification> builder)
    {
        builder.ToTable("candidate_certifications");
        builder.HasKey(c => c.Id).HasName("pk_candidate_certifications");

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.CandidateId).HasColumnName("candidate_id").IsRequired();
        builder.Property(c => c.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(c => c.IssuingOrganization).HasColumnName("issuing_organization").HasMaxLength(200).IsRequired();
        builder.Property(c => c.YearObtained).HasColumnName("year_obtained").IsRequired();

        builder.HasOne(c => c.Candidate)
            .WithMany()
            .HasForeignKey(c => c.CandidateId)
            .HasConstraintName("fk_candidate_certifications_candidates_candidate_id")
            .OnDelete(DeleteBehavior.Cascade);

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(c => c.CandidateId).HasDatabaseName("ix_candidate_certifications_candidate_id");
    }
}
