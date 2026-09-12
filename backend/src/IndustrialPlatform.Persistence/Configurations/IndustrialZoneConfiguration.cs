using IndustrialPlatform.Domain.Geography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class IndustrialZoneConfiguration : IEntityTypeConfiguration<IndustrialZone>
{
    public void Configure(EntityTypeBuilder<IndustrialZone> builder)
    {
        builder.ToTable("industrial_zones");

        builder.HasKey(z => z.Id).HasName("pk_industrial_zones");

        builder.Property(z => z.Id).HasColumnName("id");
        builder.Property(z => z.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(z => z.CityId).HasColumnName("city_id").IsRequired();
        builder.Property(z => z.ZoneType).HasColumnName("zone_type").HasConversion<string>().HasMaxLength(50).IsRequired();

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(z => z.CityId).HasDatabaseName("ix_industrial_zones_city_id");
        builder.HasIndex(z => z.Name).HasDatabaseName("ix_industrial_zones_name");

        // یادداشت: ایندکس GIN/pg_trgm برای جست‌وجوی فازی فارسی (ADR-004) در Migration اختصاصی
        // با SQL خام (CREATE EXTENSION pg_trgm; CREATE INDEX ... USING gin (name gin_trgm_ops);)
        // اضافه می‌شود، نه از طریق Fluent API — چون EF Core به‌صورت بومی از GIN/pg_trgm پشتیبانی نمی‌کند.
    }
}
