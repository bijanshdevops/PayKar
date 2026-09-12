using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Application.JobAds;
using IndustrialPlatform.Domain.Candidates;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Commands;

/// <summary>
/// تغییر وضعیت درخواست توسط شرکت — فقط برای آگهی‌های متعلق به همان شرکت (سند 04، بخش QA سناریو R-05).
/// طبق فاز «مدیریت رزومه‌ها و متقاضیان»: علاوه بر تغییر وضعیت، امکان ثبت یادداشت داخلی/زمان مصاحبه
/// و کنترل ارسال پیامک اطلاع‌رسانی به کارجو نیز فراهم شده. اگر NewStatus برابر وضعیت فعلی باشد،
/// به‌عنوان «فقط به‌روزرسانی یادداشت» تلقی می‌شود (بدون خطای انتقال نامعتبر و بدون ارسال پیامک تکراری).
/// خروجی EmployerApplicantDto است (نه JobApplicationDto عمومی) تا یادداشت داخلی هرگز به کارجو افشا نشود.
/// </summary>
public sealed record UpdateApplicationStatusCommand(
    Guid ApplicationId,
    string NewStatus,
    string? CompanyNotes = null,
    DateTime? InterviewDateTimeUtc = null,
    bool NotifyCandidate = true) : IRequest<Result<EmployerApplicantDto>>;

public sealed class UpdateApplicationStatusCommandValidator : AbstractValidator<UpdateApplicationStatusCommand>
{
    public UpdateApplicationStatusCommandValidator()
    {
        RuleFor(x => x.NewStatus).IsEnumName(typeof(JobApplicationStatus)).WithMessage("وضعیت درخواستی نامعتبر است.");
    }
}

public sealed class UpdateApplicationStatusCommandHandler : IRequestHandler<UpdateApplicationStatusCommand, Result<EmployerApplicantDto>>
{
    private static readonly Dictionary<JobApplicationStatus, string> StatusNotificationMessages = new()
    {
        [JobApplicationStatus.Reviewed] = "رزومه شما در حال بررسی است.",
        [JobApplicationStatus.InterviewScheduled] = "در انتظار تماس کارشناس باشید.",
        [JobApplicationStatus.Accepted] = "تبریک! شما برای این موقعیت شغلی پذیرفته شدید.",
        [JobApplicationStatus.Rejected] = "متاسفانه رزومه شما برای این موقعیت رد شد."
    };

    private readonly IJobApplicationRepository _applicationRepository;
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICandidateEducationRepository _educationRepository;
    private readonly ICandidateWorkExperienceRepository _workExperienceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ISmsService _smsService;

    public UpdateApplicationStatusCommandHandler(
        IJobApplicationRepository applicationRepository,
        IJobAdRepository jobAdRepository,
        ICompanyRepository companyRepository,
        ICandidateRepository candidateRepository,
        ICandidateEducationRepository educationRepository,
        ICandidateWorkExperienceRepository workExperienceRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ISmsService smsService)
    {
        _applicationRepository = applicationRepository;
        _jobAdRepository = jobAdRepository;
        _companyRepository = companyRepository;
        _candidateRepository = candidateRepository;
        _educationRepository = educationRepository;
        _workExperienceRepository = workExperienceRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _smsService = smsService;
    }

    public async Task<Result<EmployerApplicantDto>> Handle(UpdateApplicationStatusCommand request, CancellationToken cancellationToken)
    {
        var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null)
            return Result.Failure<EmployerApplicantDto>(Error.NotFound("APPLICATION_NOT_FOUND", "درخواست مورد نظر یافت نشد."));

        var jobAd = await _jobAdRepository.GetByIdAsync(application.JobAdId, cancellationToken);
        if (jobAd is null)
            return Result.Failure<EmployerApplicantDto>(Error.NotFound("JOB_AD_NOT_FOUND", "آگهی مرتبط یافت نشد."));

        var ownershipCheck = await JobAdOwnershipGuard.EnsureOwnerAsync(jobAd, _companyRepository, _currentUser, cancellationToken);
        if (ownershipCheck.IsFailure)
            return Result.Failure<EmployerApplicantDto>(ownershipCheck.Error);

        if (!Enum.TryParse<JobApplicationStatus>(request.NewStatus, out var newStatus))
            return Result.Failure<EmployerApplicantDto>(Error.Validation("VALIDATION_ERROR", "وضعیت درخواستی نامعتبر است."));

        // اگر وضعیت درخواستی همان وضعیت فعلی باشد، صرفاً یادداشت/زمان مصاحبه به‌روزرسانی می‌شود
        // (بدون فراخوانی TransitionTo — که در غیر این صورت آن را «انتقال نامعتبر» تلقی می‌کرد).
        var statusChanged = application.Status != newStatus;
        if (statusChanged)
        {
            var transitionResult = application.TransitionTo(newStatus);
            if (transitionResult.IsFailure)
                return Result.Failure<EmployerApplicantDto>(transitionResult.Error);
        }

        application.UpdateEmployerNotes(request.CompanyNotes, request.InterviewDateTimeUtc);

        var candidate = await _candidateRepository.GetByIdAsync(application.CandidateId, cancellationToken);
        if (candidate is null)
            return Result.Failure<EmployerApplicantDto>(Error.NotFound("CANDIDATE_NOT_FOUND", "کارجوی مرتبط با این درخواست یافت نشد."));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (statusChanged && request.NotifyCandidate &&
            StatusNotificationMessages.TryGetValue(newStatus, out var message) &&
            !string.IsNullOrWhiteSpace(candidate.MobileNumber))
        {
            var fullMessage = $"کارجوی گرامی، وضعیت درخواست شما برای موقعیت شغلی «{jobAd.Title}» به‌روزرسانی شد: {message}";
            await _smsService.SendTextAsync(candidate.MobileNumber, fullMessage, cancellationToken);
        }

        var educations = await _educationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var workExperiences = await _workExperienceRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);

        return Result.Success(CandidateMapper.ToEmployerDto(application, candidate, jobAd.Title, educations, workExperiences));
    }
}
