using IndustrialPlatform.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("payment_transactions");
        builder.HasKey(t => t.Id).HasName("pk_payment_transactions");

        builder.Property(t => t.Id).HasColumnName("id");
        // طبق ADR-013: CompanyId نال‌پذیر شد چون پرداخت‌کننده می‌تواند کارجو (CandidateId) هم باشد.
        builder.Property(t => t.CompanyId).HasColumnName("company_id");
        builder.Property(t => t.CandidateId).HasColumnName("candidate_id");
        // طبق ADR-008/ADR-013: JobAdId/BannerAdId هر دو Nullable هستند و بسته به Purpose دقیقاً یکی مقدار دارد.
        builder.Property(t => t.JobAdId).HasColumnName("job_ad_id");
        builder.Property(t => t.BannerAdId).HasColumnName("banner_ad_id");
        builder.Property(t => t.Purpose).HasColumnName("purpose").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.AmountInRials).HasColumnName("amount_in_rials").IsRequired();
        builder.Property(t => t.Authority).HasColumnName("authority").HasMaxLength(100).IsRequired();
        builder.Property(t => t.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.RefId).HasColumnName("ref_id").HasMaxLength(100);
        // طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۵) — شرح آزاد تراکنش‌های کسر کیف‌پول (مثل تمدید بنر).
        builder.Property(t => t.Description).HasColumnName("description").HasMaxLength(500);

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(t => t.Authority).HasDatabaseName("ux_payment_transactions_authority").IsUnique();
        builder.HasIndex(t => t.CompanyId).HasDatabaseName("ix_payment_transactions_company_id");
        builder.HasIndex(t => t.CandidateId).HasDatabaseName("ix_payment_transactions_candidate_id");
        builder.HasIndex(t => t.JobAdId).HasDatabaseName("ix_payment_transactions_job_ad_id");
        builder.HasIndex(t => t.BannerAdId).HasDatabaseName("ix_payment_transactions_banner_ad_id");
    }
}
