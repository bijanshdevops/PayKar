using IndustrialPlatform.Domain.Geography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("cities");

        builder.HasKey(c => c.Id).HasName("pk_cities");

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.ProvinceId).HasColumnName("province_id").IsRequired();

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(c => c.ProvinceId).HasDatabaseName("ix_cities_province_id");
    }
}
