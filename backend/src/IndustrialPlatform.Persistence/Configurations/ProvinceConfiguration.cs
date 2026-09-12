using IndustrialPlatform.Domain.Geography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> builder)
    {
        builder.ToTable("provinces");

        builder.HasKey(p => p.Id).HasName("pk_provinces");

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(100).IsRequired();

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(p => p.Name)
            .HasDatabaseName("ux_provinces_name")
            .IsUnique()
            .HasFilter("is_deleted = false");
    }
}
