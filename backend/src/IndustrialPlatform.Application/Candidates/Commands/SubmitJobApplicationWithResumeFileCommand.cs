using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.JobAds;
using IndustrialPlatform.Domain.Candidates;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Commands;

/// <summary>
/// مسیر «ارسال مستقیم» درخواست همکاری: به‌جای تکمیل کامل رزومه‌ساز، کارجو فقط نام و یک فایل رزومه
/// آپلود می‌کند. طبق تصمیم محصولی، این مسیر جایگزین SubmitJobApplicationCommand برای کارجویانی است
/// که هنوز پروفایل/رزومه‌ای در سیستم ندارند؛ در صورت وجود پروفایل قبلی، فقط فایل رزومه به آن الحاق می‌شود.
/// </summary>
public sealed record SubmitJobApplicationWithResumeFileCommand(
    Guid JobAdId, Stream FileContent, string FileName, string FullName) : IRequest<Result<JobApplicationDto>>;

public sealed class SubmitJobApplicationWithResumeFileCommandValidator : AbstractValidator<SubmitJobApplicationWithResumeFileCommand>
{
    public SubmitJobApplicationWithResumeFileCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("نام و نام خانوادگی الزامی است.");
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
    }
}

public sealed class SubmitJobApplicationWithResumeFileCommandHandler
    : IRequestHandler<SubmitJobApplicationWithResumeFileCommand, Result<JobApplicationDto>>
{
    private readonly IJobApplicationRepository _applicationRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly IJobAdRepository _jobAdRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ISmsService _smsService;

    public SubmitJobApplicationWithResumeFileCommandHandler(
        IJobApplicationRepository applicationRepository,
        ICandidateRepository candidateRepository,
        IJobAdRepository jobAdRepository,
        IFileStorageService fileStorage,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ISmsService smsService)
    {
        _applicationRepository = applicationRepository;
        _candidateRepository = candidateRepository;
        _jobAdRepository = jobAdRepository;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _smsService = smsService;
    }

    public async Task<Result<JobApplicationDto>> Handle(SubmitJobApplicationWithResumeFileCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<JobApplicationDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var jobAd = await _jobAdRepository.GetByIdAsync(request.JobAdId, cancellationToken);
        if (jobAd is null)
            return Result.Failure<JobApplicationDto>(Error.NotFound("JOB_AD_NOT_FOUND", "آگهی مورد نظر یافت نشد."));

        if (!jobAd.IsPublishable())
            return Result.Failure<JobApplicationDto>(Error.Conflict("JOB_AD_NOT_PUBLISHABLE", "این آگهی برای دریافت درخواست باز نیست."));

        var resumeFileUrl = await _fileStorage.SaveAsync(request.FileContent, request.FileName, "resumes", cancellationToken);

        var candidate = await _candidateRepository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (candidate is null)
        {
            var createResult = Candidate.CreateFromDirectUpload(
                _currentUser.UserId.Value, _currentUser.MobileNumber ?? string.Empty, request.FullName, resumeFileUrl);

            if (createResult.IsFailure)
                return Result.Failure<JobApplicationDto>(createResult.Error);

            candidate = createResult.Value;
            _candidateRepository.Add(candidate);
        }
        else
        {
            candidate.AttachResumeFile(resumeFileUrl);
        }

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
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(6);
        var chars = bytes.Select(b => alphabet[b % alphabet.Length]);
        return $"APP-{string.Concat(chars)}";
    }
}
