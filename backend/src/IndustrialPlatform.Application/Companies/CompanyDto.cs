namespace IndustrialPlatform.Application.Companies;

public sealed record CompanyDto(
    Guid Id,
    string Name,
    string NationalId,
    string RegistrationNumber,
    Guid IndustrialZoneId,
    string AddressDetail,
    string IndustryCategory,
    string? LogoUrl,
    string VerificationStatus,
    string? ContactPhoneNumber,
    string? BannerUrl,
    string? Website,
    string? Email,
    string? Description);
