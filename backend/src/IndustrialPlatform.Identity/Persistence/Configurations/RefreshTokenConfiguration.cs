using IndustrialPlatform.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Identity.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(rt => rt.Id).HasName("pk_refresh_tokens");

        builder.Property(rt => rt.Id).HasColumnName("id");
        builder.Property(rt => rt.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(rt => rt.TokenHash).HasColumnName("token_hash").HasMaxLength(512).IsRequired();
        builder.Property(rt => rt.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
        builder.Property(rt => rt.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.Property(rt => rt.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash").HasMaxLength(512);

        builder.Property(rt => rt.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(rt => rt.CreatedBy).HasColumnName("created_by");
        builder.Property(rt => rt.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        builder.Property(rt => rt.LastModifiedBy).HasColumnName("last_modified_by");
        builder.Property(rt => rt.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(rt => rt.DeletedAtUtc).HasColumnName("deleted_at_utc");
        builder.Property(rt => rt.DeletedBy).HasColumnName("deleted_by");
        // یادداشت: UseXminAsConcurrencyToken() در نسخه‌های جدید Npgsql حذف شده؛ تنظیم دستی shadow property.
        builder.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();

        builder.HasQueryFilter(rt => !rt.IsDeleted);

        builder.HasIndex(rt => rt.UserId).HasDatabaseName("ix_refresh_tokens_user_id");
        builder.HasIndex(rt => rt.TokenHash).HasDatabaseName("ux_refresh_tokens_token_hash").IsUnique();
    }
}
