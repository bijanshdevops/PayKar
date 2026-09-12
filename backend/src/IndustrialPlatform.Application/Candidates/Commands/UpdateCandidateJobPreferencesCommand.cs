using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Candidates;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Commands;

/// <summary>
/// به‌روزرسانی ترجیحات شغلی («نوع همکاری»/«محدوده حقوق درخواستی»/«وضعیت جستجوی کار») — طبق فاز
/// «پروفایل و رزومه‌ساز کارجو» (PUT /preferences)، مستقل از SaveCandidateProfileCommand.
/// </summary>
public sealed record UpdateCandidateJobPreferencesCommand(
    string? PreferredWorkType,
    long? MinRequestedSalaryInToman,
    long? MaxRequestedSalaryInToman,
    bool IsActivelyLookingForJob) : IRequest<Result<CandidateDto>>;

public sealed class UpdateCandidateJobPreferencesCommandValidator : AbstractValidator<UpdateCandidateJobPreferencesCommand>
{
    public UpdateCandidateJobPreferencesCommandValidator()
    {
        RuleFor(x => x.PreferredWorkType)
            .IsEnumName(typeof(Domain.Candidates.PreferredWorkType))
            .When(x => !string.IsNullOrWhiteSpace(x.PreferredWorkType));
        RuleFor(x => x.MinRequestedSalaryInToman).GreaterThanOrEqualTo(0).When(x => x.MinRequestedSalaryInToman.HasValue);
        RuleFor(x => x.MaxRequestedSalaryInToman).GreaterThanOrEqualTo(0).When(x => x.MaxRequestedSalaryInToman.HasValue);
        RuleFor(x => x)
            .Must(x => !x.MinRequestedSalaryInToman.HasValue || !x.MaxRequestedSalaryInToman.HasValue || x.MinRequestedSalaryInToman <= x.MaxRequestedSalaryInToman)
            .WithMessage("حداقل حقوق درخواستی نمی‌تواند بیشتر از حداکثر آن باشد.")
            .WithName("MaxRequestedSalaryInToman");
    }
}

public sealed class UpdateCandidateJobPreferencesCommandHandler : IRequestHandler<UpdateCandidateJobPreferencesCommand, Result<CandidateDto>>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICandidateEducationRepository _educationRepository;
    private readonly ICandidateWorkExperienceRepository _workExperienceRepository;
    private readonly ICandidateCertificationRepository _certificationRepository;
    private readonly ICandidateSkillRepository _skillRepository;
    private readonly ICandidateLanguageRepository _languageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateCandidateJobPreferencesCommandHandler(
        ICandidateRepository candidateRepository,
        ICandidateEducationRepository educationRepository,
        ICandidateWorkExperienceRepository workExperienceRepository,
        ICandidateCertificationRepository certificationRepository,
        ICandidateSkillRepository skillRepository,
        ICandidateLanguageRepository languageRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _candidateRepository = candidateRepository;
        _educationRepository = educationRepository;
        _workExperienceRepository = workExperienceRepository;
        _certificationRepository = certificationRepository;
        _skillRepository = skillRepository;
        _languageRepository = languageRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CandidateDto>> Handle(UpdateCandidateJobPreferencesCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CandidateDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var candidate = await _candidateRepository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (candidate is null)
            return Result.Failure<CandidateDto>(Error.NotFound("CANDIDATE_NOT_FOUND", "ابتدا باید پروفایل/رزومه خود را تکمیل کنید."));

        var preferredWorkType = string.IsNullOrWhiteSpace(request.PreferredWorkType)
            ? (Domain.Candidates.PreferredWorkType?)null
            : Enum.Parse<Domain.Candidates.PreferredWorkType>(request.PreferredWorkType);

        var updateResult = candidate.UpdateJobPreferences(
            preferredWorkType, request.MinRequestedSalaryInToman, request.MaxRequestedSalaryInToman, request.IsActivelyLookingForJob);
        if (updateResult.IsFailure)
            return Result.Failure<CandidateDto>(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var educations = await _educationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var workExperiences = await _workExperienceRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var certifications = await _certificationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var skills = await _skillRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var languages = await _languageRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);

        return Result.Success(CandidateMapper.ToDto(candidate, educations, workExperiences, certifications, skills, languages));
    }
}
