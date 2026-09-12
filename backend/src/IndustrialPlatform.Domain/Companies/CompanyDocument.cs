using IndustrialPlatform.Shared.Entities;

namespace IndustrialPlatform.Domain.Companies;

/// <summary>
/// مدرک ارسالی شرکت برای احراز هویت (کارت ملی، آگهی تاسیس، پروانه بهره‌برداری و...).
/// Aggregate جداگانه از Company نگه داشته شده چون چرخه حیات مستقلی دارد (فقط افزوده می‌شود).
/// </summary>
public sealed class CompanyDocument : BaseEntity<Guid>
{
    public Guid CompanyId { get; private set; }
    public CompanyDocumentType DocumentType { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string FileUrl { get; private set; } = string.Empty;

    /// <summary>حجم فایل به بایت — طبق فاز بازطراحی پروفایل شرکت، برای نمایش «۲.۴ مگابایت» و مانند آن.
    /// رکوردهای قدیمی‌تر (قبل از این فاز) صفر خواهند بود؛ فرانت باید ۰ را «نامشخص» نمایش دهد نه یک عدد جعلی.</summary>
    public long FileSizeBytes { get; private set; }

    private CompanyDocument() { }

    private CompanyDocument(Guid id, Guid companyId, CompanyDocumentType documentType, string fileName, string fileUrl, long fileSizeBytes) : base(id)
    {
        CompanyId = companyId;
        DocumentType = documentType;
        FileName = fileName;
        FileUrl = fileUrl;
        FileSizeBytes = fileSizeBytes;
    }

    public static CompanyDocument Create(Guid companyId, CompanyDocumentType documentType, string fileName, string fileUrl, long fileSizeBytes = 0) =>
        new(Guid.NewGuid(), companyId, documentType, fileName, fileUrl, fileSizeBytes);
}
