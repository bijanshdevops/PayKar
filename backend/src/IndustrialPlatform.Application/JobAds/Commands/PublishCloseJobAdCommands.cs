using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.JobAds.Commands;

// یادداشت: PublishJobAdCommand حذف شد — طبق تصمیم محصولی جدید، انتشار آگهی مستقیماً توسط شرکت
// امکان‌پذیر نیست و صرفاً از طریق جریان «پرداخت هزینه ثابت → ارسال برای بررسی → تایید ادمین»
// انجام می‌شود. به IndustrialPlatform.Application.Payments.Commands.SubmitJobAdForReviewCommand
// و IndustrialPlatform.Application.JobAds.Commands.ApproveJobAdCommand مراجعه شود.

public sealed record CloseJobAdCommand(Guid JobAdId) : IRequest<Result<JobAdDto>>;

public sealed class CloseJobAdCommandHandler : IRequestHandler<CloseJobAdCommand, Result<JobAdDto>>
{
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CloseJobAdCommandHandler(
        IJobAdRepository jobAdRepository, ICompanyRepository companyRepository, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _jobAdRepository = jobAdRepository;
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<JobAdDto>> Handle(CloseJobAdCommand request, CancellationToken cancellationToken)
    {
        var jobAd = await _jobAdRepository.GetByIdAsync(request.JobAdId, cancellationToken);
        if (jobAd is null)
            return Result.Failure<JobAdDto>(Error.NotFound("JOB_AD_NOT_FOUND", "آگهی مورد نظر یافت نشد."));

        var ownershipCheck = await JobAdOwnershipGuard.EnsureOwnerAsync(jobAd, _companyRepository, _currentUser, cancellationToken);
        if (ownershipCheck.IsFailure)
            return Result.Failure<JobAdDto>(ownershipCheck.Error);

        var result = jobAd.Close();
        if (result.IsFailure)
            return Result.Failure<JobAdDto>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var company = await _companyRepository.GetByIdAsync(jobAd.CompanyId, cancellationToken);
        return Result.Success(JobAdMapper.ToDto(jobAd, company));
    }
}

/// <summary>
/// حذف منطقی یک آگهی — طبق فاز بازطراحی «آگهی‌های شرکت من» (اکشن «حذف» روی کارت آگهی).
/// طبق مانیفست معماری (بخش ۲.۳): حذف فیزیکی ممنوع است؛ از BaseEntity.MarkAsDeleted استفاده می‌شود
/// که رکورد را از فیلتر سراسری Soft Delete کنار می‌گذارد بدون حذف واقعی از دیتابیس.
/// دقیقاً هم‌الگو با DeleteCompanyDocumentCommand.
/// </summary>
public sealed record DeleteJobAdCommand(Guid JobAdId) : IRequest<Result>;

public sealed class DeleteJobAdCommandValidator : AbstractValidator<DeleteJobAdCommand>
{
    public DeleteJobAdCommandValidator()
    {
        RuleFor(x => x.JobAdId).NotEmpty();
    }
}

public sealed class DeleteJobAdCommandHandler : IRequestHandler<DeleteJobAdCommand, Result>
{
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteJobAdCommandHandler(
        IJobAdRepository jobAdRepository,
        ICompanyRepository companyRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _jobAdRepository = jobAdRepository;
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteJobAdCommand request, CancellationToken cancellationToken)
    {
        var jobAd = await _jobAdRepository.GetByIdAsync(request.JobAdId, cancellationToken);
        if (jobAd is null)
            return Result.Failure(Error.NotFound("JOB_AD_NOT_FOUND", "آگهی مورد نظر یافت نشد."));

        var ownershipCheck = await JobAdOwnershipGuard.EnsureOwnerAsync(jobAd, _companyRepository, _currentUser, cancellationToken);
        if (ownershipCheck.IsFailure)
            return Result.Failure(ownershipCheck.Error);

        var deletableCheck = jobAd.EnsureDeletable();
        if (deletableCheck.IsFailure)
            return Result.Failure(deletableCheck.Error);

        jobAd.MarkAsDeleted(_currentUser.UserId, _dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
