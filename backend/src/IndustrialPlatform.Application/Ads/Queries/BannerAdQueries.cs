using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Domain.Ads;
using IndustrialPlatform.Shared.Api;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Ads.Queries;

/// <summary>لیست بنرهای شرکت جاری — طبق ADR-008.</summary>
public sealed record GetMyCompanyBannerAdsQuery(int Page, int PageSize) : IRequest<Result<PagedResult<BannerAdDto>>>;

public sealed class GetMyCompanyBannerAdsQueryHandler : IRequestHandler<GetMyCompanyBannerAdsQuery, Result<PagedResult<BannerAdDto>>>
{
    private readonly IBannerAdRepository _bannerAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyCompanyBannerAdsQueryHandler(IBannerAdRepository bannerAdRepository, ICompanyRepository companyRepository, ICurrentUserService currentUser)
    {
        _bannerAdRepository = bannerAdRepository;
        _companyRepository = companyRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<BannerAdDto>>> Handle(GetMyCompanyBannerAdsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<PagedResult<BannerAdDto>>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var company = await _companyRepository.GetByOwnerUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (company is null)
            return Result.Failure<PagedResult<BannerAdDto>>(Error.NotFound("COMPANY_NOT_FOUND", "ابتدا باید پروفایل شرکت خود را تکمیل کنید."));

        var paged = await _bannerAdRepository.GetByCompanyIdAsync(company.Id, request.Page, request.PageSize, cancellationToken);
        var dtoItems = paged.Items.Select(BannerAdMapper.ToDto).ToList();

        return Result.Success(PagedResult<BannerAdDto>.Create(dtoItems, paged.TotalCount, paged.Page, paged.PageSize));
    }
}

/// <summary>صف بررسی ادمین/پنل Owner — طبق ADR-008.</summary>
public sealed record GetPendingReviewBannerAdsQuery(int Page, int PageSize) : IRequest<Result<PagedResult<BannerAdDto>>>;

public sealed class GetPendingReviewBannerAdsQueryHandler : IRequestHandler<GetPendingReviewBannerAdsQuery, Result<PagedResult<BannerAdDto>>>
{
    private readonly IBannerAdRepository _bannerAdRepository;

    public GetPendingReviewBannerAdsQueryHandler(IBannerAdRepository bannerAdRepository) => _bannerAdRepository = bannerAdRepository;

    public async Task<Result<PagedResult<BannerAdDto>>> Handle(GetPendingReviewBannerAdsQuery request, CancellationToken cancellationToken)
    {
        var paged = await _bannerAdRepository.GetPendingReviewAsync(request.Page, request.PageSize, cancellationToken);
        var dtoItems = paged.Items.Select(BannerAdMapper.ToDto).ToList();

        return Result.Success(PagedResult<BannerAdDto>.Create(dtoItems, paged.TotalCount, paged.Page, paged.PageSize));
    }
}

/// <summary>بنرهای فعال قابل نمایش عمومی بر اساس محل نمایش — بدون نیاز به احراز هویت (طبق ADR-008).</summary>
public sealed record GetActiveBannerAdsQuery(string Placement) : IRequest<Result<IReadOnlyList<BannerAdDto>>>;

public sealed class GetActiveBannerAdsQueryHandler : IRequestHandler<GetActiveBannerAdsQuery, Result<IReadOnlyList<BannerAdDto>>>
{
    private readonly IBannerAdRepository _bannerAdRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetActiveBannerAdsQueryHandler(IBannerAdRepository bannerAdRepository, IDateTimeProvider dateTimeProvider)
    {
        _bannerAdRepository = bannerAdRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<IReadOnlyList<BannerAdDto>>> Handle(GetActiveBannerAdsQuery request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<BannerPlacement>(request.Placement, out var placement))
            return Result.Failure<IReadOnlyList<BannerAdDto>>(Error.Validation("VALIDATION_ERROR", "محل نمایش بنر نامعتبر است."));

        var banners = await _bannerAdRepository.GetActiveByPlacementAsync(placement, _dateTimeProvider.UtcNow, cancellationToken);
        return Result.Success<IReadOnlyList<BannerAdDto>>(banners.Select(BannerAdMapper.ToDto).ToList());
    }
}
