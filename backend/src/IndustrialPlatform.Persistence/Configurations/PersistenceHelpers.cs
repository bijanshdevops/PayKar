using IndustrialPlatform.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

/// <summary>
/// ستون‌های حسابرسی/حذف منطقی مشترک طبق سند 03-Database-Standards.md بخش ۳.
/// روی هر Entity که IAuditableEntity/ISoftDeletable را پیاده‌سازی کند فراخوانی می‌شود.
/// </summary>
internal static class PersistenceHelpers
{
    public static void ConfigureAuditColumns<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditableEntity, ISoftDeletable
    {
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        builder.Property(e => e.LastModifiedBy).HasColumnName("last_modified_by");

        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by");

        // مدیریت همروندی خوش‌بینانه با ستون سیستمی xmin پستگرس‌کیو‌ال (سند 03، بخش ۳)
        // یادداشت: متد کمکی UseXminAsConcurrencyToken() در نسخه‌های جدید Npgsql.EntityFrameworkCore.PostgreSQL
        // حذف شده؛ طبق مستندات رسمی باید shadow property «xmin» را دستی به‌عنوان RowVersion تنظیم کرد.
        builder.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();

        // فیلتر سراسری Soft Delete طبق سند 03، بخش ۴.۲
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
