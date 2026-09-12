using IndustrialPlatform.Domain.Companies;

namespace IndustrialPlatform.Application.Companies;

internal static class CompanyMapper
{
    public static CompanyDto ToDto(Company company) => new(
        company.Id,
        company.Name,
        company.NationalId,
        company.RegistrationNumber,
        company.IndustrialZoneId,
        company.AddressDetail,
        company.IndustryCategory,
        company.LogoUrl,
        company.VerificationStatus.ToString(),
        company.ContactPhoneNumber,
        company.BannerUrl,
        company.Website,
        company.Email,
        company.Description);
}
