using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.JobAds.Commands;

/// <summary>تایید آگهی توسط ادمین (پنل Owner) — از PendingReview به Published.</summary>
public sealed record ApproveJobAdCommand(Guid JobAdId) : IRequest<Result<JobAdDto>>;

public sealed class ApproveJobAdCommandHandler : IRequestHandler<ApproveJobAdCommand, Result<JobAdDto>>
{
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ApproveJobAdCommandHandler(
        IJobAdRepository jobAdRepository, ICompanyRepository companyRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _jobAdRepository = jobAdRepository;
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<JobAdDto>> Handle(ApproveJobAdCommand request, CancellationToken cancellationToken)
    {
        var jobAd = await _jobAdRepository.GetByIdAsync(request.JobAdId, cancellationToken);
        if (jobAd is null)
            return Result.Failure<JobAdDto>(Error.NotFound("JOB_AD_NOT_FOUND", "آگهی مورد نظر یافت نشد."));

        var result = jobAd.Approve(_dateTimeProvider.UtcNow);
        if (result.IsFailure)
            return Result.Failure<JobAdDto>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var company = await _companyRepository.GetByIdAsync(jobAd.CompanyId, cancellationToken);
        return Result.Success(JobAdMapper.ToDto(jobAd, company));
    }
}

/// <summary>رد آگهی توسط ادمین (پنل Owner) با ذکر دلیل — از PendingReview به Rejected.</summary>
public sealed record RejectJobAdCommand(Guid JobAdId, string Reason) : IRequest<Result<JobAdDto>>;

public sealed class RejectJobAdCommandValidator : AbstractValidator<RejectJobAdCommand>
{
    public RejectJobAdCommandValidator()
    {
        RuleFor(x => x.JobAdId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().WithMessage("ذکر دلیل رد آگهی الزامی است.").MaximumLength(1000);
    }
}

public sealed class RejectJobAdCommandHandler : IRequestHandler<RejectJobAdCommand, Result<JobAdDto>>
{
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RejectJobAdCommandHandler(IJobAdRepository jobAdRepository, ICompanyRepository companyRepository, IUnitOfWork unitOfWork)
    {
        _jobAdRepository = jobAdRepository;
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<JobAdDto>> Handle(RejectJobAdCommand request, CancellationToken cancellationToken)
    {
        var jobAd = await _jobAdRepository.GetByIdAsync(request.JobAdId, cancellationToken);
        if (jobAd is null)
            return Result.Failure<JobAdDto>(Error.NotFound("JOB_AD_NOT_FOUND", "آگهی مورد نظر یافت نشد."));

        var result = jobAd.Reject(request.Reason);
        if (result.IsFailure)
            return Result.Failure<JobAdDto>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var company = await _companyRepository.GetByIdAsync(jobAd.CompanyId, cancellationToken);
        return Result.Success(JobAdMapper.ToDto(jobAd, company));
    }
}
