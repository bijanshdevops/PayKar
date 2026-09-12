using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Domain.Ads;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Ads.Commands;

/// <summary>ویرایش بنر پیش‌نویس/ردشده پیش از ارسال مجدد برای بررسی — طبق ADR-008.</summary>
public sealed record UpdateBannerAdCommand(Guid BannerAdId, string ImageUrl, string DestinationUrl, string Placement) : IRequest<Result<BannerAdDto>>;

public sealed class UpdateBannerAdCommandHandler : IRequestHandler<UpdateBannerAdCommand, Result<BannerAdDto>>
{
    private readonly IBannerAdRepository _bannerAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateBannerAdCommandHandler(
        IBannerAdRepository bannerAdRepository, ICompanyRepository companyRepository, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _bannerAdRepository = bannerAdRepository;
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<BannerAdDto>> Handle(UpdateBannerAdCommand request, CancellationToken cancellationToken)
    {
        var bannerAd = await _bannerAdRepository.GetByIdAsync(request.BannerAdId, cancellationToken);
        if (bannerAd is null)
            return Result.Failure<BannerAdDto>(Error.NotFound("BANNER_AD_NOT_FOUND", "بنر مورد نظر یافت نشد."));

        var ownershipCheck = await BannerAdOwnershipGuard.EnsureOwnerAsync(bannerAd, _companyRepository, _currentUser, cancellationToken);
        if (ownershipCheck.IsFailure)
            return Result.Failure<BannerAdDto>(ownershipCheck.Error);

        if (!Enum.TryParse<BannerPlacement>(request.Placement, out var placement))
            return Result.Failure<BannerAdDto>(Error.Validation("VALIDATION_ERROR", "محل نمایش بنر نامعتبر است."));

        var updateResult = bannerAd.Update(request.ImageUrl, request.DestinationUrl, placement);
        if (updateResult.IsFailure)
            return Result.Failure<BannerAdDto>(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(BannerAdMapper.ToDto(bannerAd));
    }
}
