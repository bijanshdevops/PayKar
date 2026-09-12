namespace IndustrialPlatform.Application.Companies;

public sealed record CompanyDocumentDto(
    Guid Id,
    Guid CompanyId,
    string DocumentType,
    string FileName,
    string FileUrl,
    long FileSizeBytes,
    DateTime UploadedAtUtc);
