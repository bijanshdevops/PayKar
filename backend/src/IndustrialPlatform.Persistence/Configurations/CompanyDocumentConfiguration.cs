using IndustrialPlatform.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class CompanyDocumentConfiguration : IEntityTypeConfiguration<CompanyDocument>
{
    public void Configure(EntityTypeBuilder<CompanyDocument> builder)
    {
        builder.ToTable("company_documents");
        builder.HasKey(d => d.Id).HasName("pk_company_documents");

        builder.Property(d => d.Id).HasColumnName("id");
        builder.Property(d => d.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(d => d.DocumentType).HasColumnName("document_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(d => d.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
        builder.Property(d => d.FileUrl).HasColumnName("file_url").HasMaxLength(500).IsRequired();
        // طبق فاز بازطراحی صفحه پروفایل شرکت — برای نمایش حجم فایل روی کارت مدرک؛ رکوردهای قدیمی صفر می‌مانند.
        builder.Property(d => d.FileSizeBytes).HasColumnName("file_size_bytes").IsRequired().HasDefaultValue(0L);

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(d => d.CompanyId).HasDatabaseName("ix_company_documents_company_id");
    }
}
