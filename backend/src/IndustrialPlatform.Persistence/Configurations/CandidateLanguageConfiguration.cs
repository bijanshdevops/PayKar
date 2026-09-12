using IndustrialPlatform.Domain.Candidates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class CandidateLanguageConfiguration : IEntityTypeConfiguration<CandidateLanguage>
{
    public void Configure(EntityTypeBuilder<CandidateLanguage> builder)
    {
        builder.ToTable("candidate_languages");
        builder.HasKey(l => l.Id).HasName("pk_candidate_languages");

        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.CandidateId).HasColumnName("candidate_id").IsRequired();
        builder.Property(l => l.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        builder.Property(l => l.ProficiencyLevel).HasColumnName("proficiency_level").HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasOne(l => l.Candidate)
            .WithMany()
            .HasForeignKey(l => l.CandidateId)
            .HasConstraintName("fk_candidate_languages_candidates_candidate_id")
            .OnDelete(DeleteBehavior.Cascade);

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(l => l.CandidateId).HasDatabaseName("ix_candidate_languages_candidate_id");

        // جلوگیری از ثبت یک زبان تکراری برای یک کارجو.
        builder.HasIndex(l => new { l.CandidateId, l.Name })
            .HasDatabaseName("ux_candidate_languages_candidate_id_name")
            .IsUnique()
            .HasFilter("is_deleted = false");
    }
}
