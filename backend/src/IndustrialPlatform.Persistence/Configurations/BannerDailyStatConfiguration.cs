using IndustrialPlatform.Domain.Ads;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class BannerDailyStatConfiguration : IEntityTypeConfiguration<BannerDailyStat>
{
    public void Configure(EntityTypeBuilder<BannerDailyStat> builder)
    {
        builder.ToTable("banner_daily_stats");
        builder.HasKey(s => s.Id).HasName("pk_banner_daily_stats");

        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(s => s.BannerAdId).HasColumnName("banner_ad_id").IsRequired();
        builder.Property(s => s.Date).HasColumnName("date").HasColumnType("date").IsRequired();
        builder.Property(s => s.ImpressionsCount).HasColumnName("impressions_count").HasDefaultValue(0L).IsRequired();
        builder.Property(s => s.ClicksCount).HasColumnName("clicks_count").HasDefaultValue(0L).IsRequired();

        builder.HasOne(s => s.BannerAd)
            .WithMany()
            .HasForeignKey(s => s.BannerAdId)
            .HasConstraintName("fk_banner_daily_stats_banner_ads_banner_ad_id")
            .OnDelete(DeleteBehavior.Cascade);

        PersistenceHelpers.ConfigureAuditColumns(builder);

        // طبق تسک ۶ — جلوگیری از رکورد تکراری برای یک بنر در یک روز.
        builder.HasIndex(s => new { s.BannerAdId, s.Date })
            .HasDatabaseName("ux_banner_daily_stats_banner_ad_id_date")
            .IsUnique()
            .HasFilter("is_deleted = false");
    }
}
