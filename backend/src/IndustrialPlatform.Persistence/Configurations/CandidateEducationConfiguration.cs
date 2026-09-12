using IndustrialPlatform.Domain.Candidates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class CandidateEducationConfiguration : IEntityTypeConfiguration<CandidateEducation>
{
    public void Configure(EntityTypeBuilder<CandidateEducation> builder)
    {
        builder.ToTable("candidate_educations");
        builder.HasKey(e => e.Id).HasName("pk_candidate_educations");

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.CandidateId).HasColumnName("candidate_id").IsRequired();
        builder.Property(e => e.DegreeLevel).HasColumnName("degree_level").HasMaxLength(100).IsRequired();
        builder.Property(e => e.FieldOfStudy).HasColumnName("field_of_study").HasMaxLength(150).IsRequired();
        builder.Property(e => e.InstitutionName).HasColumnName("institution_name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.GraduationYear).HasColumnName("graduation_year");

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(e => e.CandidateId).HasDatabaseName("ix_candidate_educations_candidate_id");
    }
}
