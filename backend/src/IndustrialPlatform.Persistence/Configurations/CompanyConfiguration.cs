using IndustrialPlatform.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");
        builder.HasKey(c => c.Id).HasName("pk_companies");

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(c => c.NationalId).HasColumnName("national_id").HasMaxLength(11).IsRequired();
        builder.Property(c => c.RegistrationNumber).HasColumnName("registration_number").HasMaxLength(50).IsRequired();
        builder.Property(c => c.IndustrialZoneId).HasColumnName("industrial_zone_id").IsRequired();
        builder.Property(c => c.AddressDetail).HasColumnName("address_detail").HasColumnType("text").IsRequired();
        builder.Property(c => c.IndustryCategory).HasColumnName("industry_category").HasMaxLength(150).IsRequired();
        builder.Property(c => c.LogoUrl).HasColumnName("logo_url").HasMaxLength(500);
        // طبق ADR-011 — شماره تماس عمومی برای دکمه «تماس با کارفرما» در صفحه جزئیات آگهی.
        builder.Property(c => c.ContactPhoneNumber).HasColumnName("contact_phone_number").HasMaxLength(20);
        // طبق فاز بازطراحی صفحه پروفایل شرکت — فیلدهای اختیاری نمایشی.
        builder.Property(c => c.BannerUrl).HasColumnName("banner_url").HasMaxLength(500);
        builder.Property(c => c.Website).HasColumnName("website").HasMaxLength(300);
        builder.Property(c => c.Email).HasColumnName("email").HasMaxLength(200);
        builder.Property(c => c.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(c => c.VerificationStatus).HasColumnName("verification_status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(c => c.OwnerUserId).HasColumnName("owner_user_id").IsRequired();
        // فیلد اختیاری/صفر، بدون وابستگی الزامی — طبق تصمیم صریح محصولی (مدل پرداخت مستقیم زرین‌پال برای MVP).
        builder.Property(c => c.WalletBalanceInRials).HasColumnName("wallet_balance_in_rials").IsRequired().HasDefaultValue(0L);

        PersistenceHelpers.ConfigureAuditColumns(builder);

        // یکتایی شناسه ملی فقط روی رکوردهای فعال (سند 03-Database-Standards.md بخش ۴.۳)
        builder.HasIndex(c => c.NationalId)
            .HasDatabaseName("ux_companies_national_id")
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(c => c.OwnerUserId).HasDatabaseName("ix_companies_owner_user_id");
        builder.HasIndex(c => c.IndustrialZoneId).HasDatabaseName("ix_companies_industrial_zone_id");
        builder.HasIndex(c => c.VerificationStatus).HasDatabaseName("ix_companies_verification_status");
    }
}
