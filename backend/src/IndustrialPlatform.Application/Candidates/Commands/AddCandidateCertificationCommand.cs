using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Candidates;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Commands;

/// <summary>
/// افزودن یک ردیف مدرک/گواهینامه به رزومه‌ساز — طبق فاز «پروفایل و رزومه‌ساز کارجو»، هم‌راستا با
/// AddCandidateEducationCommand. فقط برای کارجوی صاحب پروفایل جاری.
/// </summary>
public sealed record AddCandidateCertificationCommand(
    string Title,
    string IssuingOrganization,
    int YearObtained) : IRequest<Result<CandidateDto>>;

public sealed class AddCandidateCertificationCommandValidator : AbstractValidator<AddCandidateCertificationCommand>
{
    public AddCandidateCertificationCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان مدرک الزامی است.").MaximumLength(150);
        RuleFor(x => x.IssuingOrganization).NotEmpty().WithMessage("نام موسسه صادرکننده الزامی است.").MaximumLength(200);
        RuleFor(x => x.YearObtained).InclusiveBetween(1300, 1500).WithMessage("سال اخذ مدرک نامعتبر است.");
    }
}

public sealed class AddCandidateCertificationCommandHandler : IRequestHandler<AddCandidateCertificationCommand, Result<CandidateDto>>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICandidateEducationRepository _educationRepository;
    private readonly ICandidateWorkExperienceRepository _workExperienceRepository;
    private readonly ICandidateCertificationRepository _certificationRepository;
    private readonly ICandidateSkillRepository _skillRepository;
    private readonly ICandidateLanguageRepository _languageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AddCandidateCertificationCommandHandler(
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

    public async Task<Result<CandidateDto>> Handle(AddCandidateCertificationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CandidateDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var candidate = await _candidateRepository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (candidate is null)
            return Result.Failure<CandidateDto>(Error.NotFound("CANDIDATE_NOT_FOUND", "ابتدا باید پروفایل/رزومه خود را تکمیل کنید."));

        var certification = CandidateCertification.Create(candidate.Id, request.Title, request.IssuingOrganization, request.YearObtained);
        _certificationRepository.Add(certification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var educations = await _educationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var workExperiences = await _workExperienceRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var certifications = await _certificationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var skills = await _skillRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var languages = await _languageRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);

        return Result.Success(CandidateMapper.ToDto(candidate, educations, workExperiences, certifications, skills, languages));
    }
}
