using IndustrialPlatform.Domain.Ads;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class BannerAdConfiguration : IEntityTypeConfiguration<BannerAd>
{
    public void Configure(EntityTypeBuilder<BannerAd> builder)
    {
        builder.ToTable("banner_ads");
        builder.HasKey(b => b.Id).HasName("pk_banner_ads");

        builder.Property(b => b.Id).HasColumnName("id");
        builder.Property(b => b.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(b => b.ImageUrl).HasColumnName("image_url").HasMaxLength(500).IsRequired();
        builder.Property(b => b.DestinationUrl).HasColumnName("destination_url").HasMaxLength(500).IsRequired();
        builder.Property(b => b.Placement).HasColumnName("placement").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(b => b.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(b => b.IsFeePaid).HasColumnName("is_fee_paid").IsRequired();
        builder.Property(b => b.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(1000);
        builder.Property(b => b.SubmittedForReviewAtUtc).HasColumnName("submitted_for_review_at_utc");
        builder.Property(b => b.ActivatedAtUtc).HasColumnName("activated_at_utc");
        builder.Property(b => b.ExpiresAtUtc).HasColumnName("expires_at_utc");

        // طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۲) — کاتالوگ جایگاه + قیمت‌گذاری روزانه + آمار اولیه.
        builder.Property(b => b.BannerSlotId).HasColumnName("banner_slot_id");
        builder.Property(b => b.DurationDays).HasColumnName("duration_days").HasDefaultValue(30).IsRequired();
        builder.Property(b => b.StartDate).HasColumnName("start_date");
        builder.Property(b => b.EndDate).HasColumnName("end_date");
        builder.Property(b => b.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        builder.Property(b => b.ImpressionsCount).HasColumnName("impressions_count").HasDefaultValue(0L).IsRequired();
        builder.Property(b => b.ClicksCount).HasColumnName("clicks_count").HasDefaultValue(0L).IsRequired();

        builder.HasOne(b => b.BannerSlot)
            .WithMany()
            .HasForeignKey(b => b.BannerSlotId)
            .HasConstraintName("fk_banner_ads_banner_slots_banner_slot_id")
            .OnDelete(DeleteBehavior.Restrict);

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(b => b.CompanyId).HasDatabaseName("ix_banner_ads_company_id");
        builder.HasIndex(b => b.Status).HasDatabaseName("ix_banner_ads_status");
        builder.HasIndex(b => b.BannerSlotId).HasDatabaseName("ix_banner_ads_banner_slot_id");
    }
}
