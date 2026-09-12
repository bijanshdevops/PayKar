using IndustrialPlatform.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Identity.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Id).HasName("pk_roles");

        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(50).IsRequired();

        builder.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(r => r.CreatedBy).HasColumnName("created_by");
        builder.Property(r => r.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        builder.Property(r => r.LastModifiedBy).HasColumnName("last_modified_by");
        builder.Property(r => r.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(r => r.DeletedAtUtc).HasColumnName("deleted_at_utc");
        builder.Property(r => r.DeletedBy).HasColumnName("deleted_by");
        // یادداشت: UseXminAsConcurrencyToken() در نسخه‌های جدید Npgsql حذف شده؛ تنظیم دستی shadow property.
        builder.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();

        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.HasIndex(r => r.Name).HasDatabaseName("ux_roles_name").IsUnique();

        builder.HasData(
            new { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = Role.Admin, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsDeleted = false },
            new { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = Role.CompanyManager, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsDeleted = false },
            new { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = Role.Candidate, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsDeleted = false }
        );
    }
}
