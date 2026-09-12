using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Domain.JobAds;
using IndustrialPlatform.Shared.Results;

namespace IndustrialPlatform.Application.JobAds;

/// <summary>
/// بررسی مالکیت آگهی طبق سند 05-Security-Rules.md بخش ۳ (Resource Ownership):
/// فقط مالک شرکت مربوطه یا Admin اجازه تغییر آگهی را دارد.
/// </summary>
internal static class JobAdOwnershipGuard
{
    public static async Task<Result> EnsureOwnerAsync(
        JobAd jobAd, ICompanyRepository companyRepository, ICurrentUserService currentUser, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole("Admin"))
            return Result.Success();

        if (currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var company = await companyRepository.GetByOwnerUserIdAsync(currentUser.UserId.Value, cancellationToken);
        if (company is null || company.Id != jobAd.CompanyId)
            return Result.Failure(Error.Forbidden("FORBIDDEN", "شما اجازه مدیریت این آگهی را ندارید."));

        return Result.Success();
    }
}
