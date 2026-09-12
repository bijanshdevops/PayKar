using IndustrialPlatform.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class CompanyDailyViewStatConfiguration : IEntityTypeConfiguration<CompanyDailyViewStat>
{
    public void Configure(EntityTypeBuilder<CompanyDailyViewStat> builder)
    {
        builder.ToTable("company_daily_view_stats");
        builder.HasKey(s => s.Id).HasName("pk_company_daily_view_stats");

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(s => s.StatDateUtc).HasColumnName("stat_date_utc").HasColumnType("date").IsRequired();
        builder.Property(s => s.ViewsCount).HasColumnName("views_count").IsRequired();

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(s => new { s.CompanyId, s.StatDateUtc })
            .HasDatabaseName("ux_company_daily_view_stats_company_id_stat_date_utc")
            .IsUnique()
            .HasFilter("is_deleted = false");
    }
}
