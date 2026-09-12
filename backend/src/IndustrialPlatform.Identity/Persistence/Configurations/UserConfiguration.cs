using IndustrialPlatform.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Identity.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id).HasName("pk_users");

        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.MobileNumber).HasColumnName("mobile_number").HasMaxLength(15).IsRequired();
        builder.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(u => u.Username).HasColumnName("username").HasMaxLength(50);
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(200);
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(200);
        builder.Property(u => u.IsMobileVerified).HasColumnName("is_mobile_verified").HasDefaultValue(false).IsRequired();

        builder.Property(u => u.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(u => u.CreatedBy).HasColumnName("created_by");
        builder.Property(u => u.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        builder.Property(u => u.LastModifiedBy).HasColumnName("last_modified_by");
        builder.Property(u => u.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(u => u.DeletedAtUtc).HasColumnName("deleted_at_utc");
        builder.Property(u => u.DeletedBy).HasColumnName("deleted_by");
        // یادداشت: UseXminAsConcurrencyToken() در نسخه‌های جدید Npgsql حذف شده؛ تنظیم دستی shadow property.
        builder.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();

        builder.HasQueryFilter(u => !u.IsDeleted);

        builder.HasIndex(u => u.MobileNumber)
            .HasDatabaseName("ux_users_mobile_number")
            .IsUnique()
            .HasFilter("is_deleted = false");

        // یکتایی نام‌کاربری فقط روی رکوردهای فعال و غیر‌Null (طبق ADR-005).
        builder.HasIndex(u => u.Username)
            .HasDatabaseName("ux_users_username")
            .IsUnique()
            .HasFilter("is_deleted = false AND username IS NOT NULL");

        // یکتایی ایمیل فقط روی رکوردهای فعال و غیر‌Null (طبق ADR-006).
        builder.HasIndex(u => u.Email)
            .HasDatabaseName("ux_users_email")
            .IsUnique()
            .HasFilter("is_deleted = false AND email IS NOT NULL");

        builder.Metadata.FindNavigation(nameof(User.UserRoles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
