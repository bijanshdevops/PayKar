using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Domain.Ads;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Ads.Commands;

/// <summary>
/// ایجاد بنر تبلیغاتی پیش‌نویس توسط شرکت — طبق ADR-008.
/// گسترش‌یافته در فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۳): انتخاب جایگاه از کاتالوگ
/// BannerSlot و قفل‌کردن قیمت روزانه/مدت نمایش لحظه ایجاد (بدون داده فیک — قیمت مستقیماً از رکورد واقعی BannerSlot خوانده می‌شود).
/// </summary>
public sealed record CreateBannerAdCommand(
    string ImageUrl,
    string DestinationUrl,
    string Placement,
    int BannerSlotId,
    int? DurationDays = null) : IRequest<Result<BannerAdDto>>;

public sealed class CreateBannerAdCommandValidator : AbstractValidator<CreateBannerAdCommand>
{
    public CreateBannerAdCommandValidator()
    {
        RuleFor(x => x.ImageUrl).NotEmpty().WithMessage("تصویر بنر الزامی است.");
        RuleFor(x => x.DestinationUrl).NotEmpty().WithMessage("لینک مقصد بنر الزامی است.");
        RuleFor(x => x.Placement).IsEnumName(typeof(BannerPlacement)).WithMessage("محل نمایش بنر نامعتبر است.");
        RuleFor(x => x.BannerSlotId).GreaterThan(0).WithMessage("انتخاب جایگاه بنر الزامی است.");
        RuleFor(x => x.DurationDays).GreaterThan(0).When(x => x.DurationDays.HasValue)
            .WithMessage("مدت زمان نمایش باید بزرگ‌تر از صفر باشد.");
    }
}

public sealed class CreateBannerAdCommandHandler : IRequestHandler<CreateBannerAdCommand, Result<BannerAdDto>>
{
    private readonly IBannerAdRepository _bannerAdRepository;
    private readonly IBannerSlotRepository _bannerSlotRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateBannerAdCommandHandler(
        IBannerAdRepository bannerAdRepository,
        IBannerSlotRepository bannerSlotRepository,
        ICompanyRepository companyRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _bannerAdRepository = bannerAdRepository;
        _bannerSlotRepository = bannerSlotRepository;
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<BannerAdDto>> Handle(CreateBannerAdCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<BannerAdDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var company = await _companyRepository.GetByOwnerUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (company is null)
            return Result.Failure<BannerAdDto>(Error.NotFound("COMPANY_NOT_FOUND", "ابتدا باید پروفایل شرکت خود را تکمیل کنید."));

        if (!Enum.TryParse<BannerPlacement>(request.Placement, out var placement))
            return Result.Failure<BannerAdDto>(Error.Validation("VALIDATION_ERROR", "محل نمایش بنر نامعتبر است."));

        var slot = await _bannerSlotRepository.GetByIdAsync(request.BannerSlotId, cancellationToken);
        if (slot is null || !slot.IsActive)
            return Result.Failure<BannerAdDto>(Error.NotFound("BANNER_SLOT_NOT_FOUND", "جایگاه تبلیغاتی انتخاب‌شده یافت نشد یا غیرفعال است."));

        var createResult = BannerAd.Create(company.Id, request.ImageUrl, request.DestinationUrl, placement);
        if (createResult.IsFailure)
            return Result.Failure<BannerAdDto>(createResult.Error);

        var durationDays = request.DurationDays ?? BannerAd.DefaultDurationDays;
        var assignResult = createResult.Value.AssignSlotAndPricing(slot.Id, slot.DailyPrice, durationDays);
        if (assignResult.IsFailure)
            return Result.Failure<BannerAdDto>(assignResult.Error);

        _bannerAdRepository.Add(createResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(BannerAdMapper.ToDto(createResult.Value));
    }
}
