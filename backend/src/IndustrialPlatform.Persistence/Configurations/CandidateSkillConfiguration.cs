using IndustrialPlatform.Domain.Candidates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class CandidateSkillConfiguration : IEntityTypeConfiguration<CandidateSkill>
{
    public void Configure(EntityTypeBuilder<CandidateSkill> builder)
    {
        builder.ToTable("candidate_skills");
        builder.HasKey(s => s.Id).HasName("pk_candidate_skills");

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.CandidateId).HasColumnName("candidate_id").IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        // Category عمداً متن آزاد است — ر.ک. یادداشت طراحی در CandidateSkill.cs.
        builder.Property(s => s.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
        builder.Property(s => s.Level).HasColumnName("level").IsRequired();

        builder.HasOne(s => s.Candidate)
            .WithMany()
            .HasForeignKey(s => s.CandidateId)
            .HasConstraintName("fk_candidate_skills_candidates_candidate_id")
            .OnDelete(DeleteBehavior.Cascade);

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(s => s.CandidateId).HasDatabaseName("ix_candidate_skills_candidate_id");
    }
}
