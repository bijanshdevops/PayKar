using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Domain.Ads;
using IndustrialPlatform.Shared.Results;

namespace IndustrialPlatform.Application.Ads;

/// <summary>بررسی مالکیت بنر — عیناً الگوی JobAdOwnershipGuard (طبق ADR-008).</summary>
internal static class BannerAdOwnershipGuard
{
    public static async Task<Result> EnsureOwnerAsync(
        BannerAd bannerAd, ICompanyRepository companyRepository, ICurrentUserService currentUser, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole("Admin"))
            return Result.Success();

        if (currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var company = await companyRepository.GetByOwnerUserIdAsync(currentUser.UserId.Value, cancellationToken);
        if (company is null || company.Id != bannerAd.CompanyId)
            return Result.Failure(Error.Forbidden("FORBIDDEN", "شما اجازه مدیریت این بنر را ندارید."));

        return Result.Success();
    }
}
