using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.JobAds;
using IndustrialPlatform.Domain.Candidates;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Commands;

/// <summary>
/// ثبت درخواست همکاری کارجو برای یک آگهی — طبق سند 04-Api-Contract.md بخش ۵.۵.
/// خطای JOB_AD_NOT_PUBLISHABLE برای آگهی‌های غیرفعال (سند 04، بخش ۴).
/// </summary>
public sealed record SubmitJobApplicationCommand(Guid JobAdId) : IRequest<Result<JobApplicationDto>>;

public sealed class SubmitJobApplicationCommandHandler : IRequestHandler<SubmitJobApplicationCommand, Result<JobApplicationDto>>
{
    private readonly IJobApplicationRepository _applicationRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly IJobAdRepository _jobAdRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ISmsService _smsService;

    public SubmitJobApplicationCommandHandler(
        IJobApplicationRepository applicationRepository,
        ICandidateRepository candidateRepository,
        IJobAdRepository jobAdRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ISmsService smsService)
    {
        _applicationRepository = applicationRepository;
        _candidateRepository = candidateRepository;
        _jobAdRepository = jobAdRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _smsService = smsService;
    }

    public async Task<Result<JobApplicationDto>> Handle(SubmitJobApplicationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<JobApplicationDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var candidate = await _candidateRepository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (candidate is null)
            return Result.Failure<JobApplicationDto>(Error.NotFound("CANDIDATE_NOT_FOUND", "ابتدا باید پروفایل/رزومه خود را تکمیل کنید."));

        var jobAd = await _jobAdRepository.GetByIdAsync(request.JobAdId, cancellationToken);
        if (jobAd is null)
            return Result.Failure<JobApplicationDto>(Error.NotFound("JOB_AD_NOT_FOUND", "آگهی مورد نظر یافت نشد."));

        if (!jobAd.IsPublishable())
            return Result.Failure<JobApplicationDto>(Error.Conflict("JOB_AD_NOT_PUBLISHABLE", "این آگهی برای دریافت درخواست باز نیست."));

        if (await _applicationRepository.ExistsForCandidateAndJobAdAsync(candidate.Id, jobAd.Id, cancellationToken))
            return Result.Failure<JobApplicationDto>(Error.Conflict("APPLICATION_ALREADY_SUBMITTED", "شما قبلاً برای این آگهی درخواست ارسال کرده‌اید."));

        var trackingToken = GenerateTrackingToken();
        var matchScorePercent = ResumeMatchScoreCalculator.Calculate(candidate.Skills, jobAd.RequiredSkills);
        var application = JobApplication.Create(jobAd.Id, candidate.Id, trackingToken, matchScorePercent);

        _applicationRepository.Add(application);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(candidate.MobileNumber))
            await _smsService.SendTextAsync(candidate.MobileNumber, "رزومه شما دریافت شد.", cancellationToken);

        return Result.Success(CandidateMapper.ToDto(application));
    }

    private static string GenerateTrackingToken()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // بدون کاراکترهای مشابه (O/0, I/1)
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(6);
        var chars = bytes.Select(b => alphabet[b % alphabet.Length]);
        return $"APP-{string.Concat(chars)}";
    }
}
