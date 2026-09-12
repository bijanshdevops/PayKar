using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Candidates;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Commands;

/// <summary>
/// افزودن یک ردیف سابقه تحصیلی به رزومه‌ساز — طبق فاز «مدیریت رزومه‌ها و متقاضیان» (چند ردیفی، مکمل
/// فیلد آزاد Candidate.EducationLevel، نه جایگزین آن). فقط برای کارجوی صاحب پروفایل جاری.
/// </summary>
public sealed record AddCandidateEducationCommand(
    string DegreeLevel,
    string FieldOfStudy,
    string InstitutionName,
    int? GraduationYear = null) : IRequest<Result<CandidateDto>>;

public sealed class AddCandidateEducationCommandValidator : AbstractValidator<AddCandidateEducationCommand>
{
    public AddCandidateEducationCommandValidator()
    {
        RuleFor(x => x.DegreeLevel).NotEmpty().WithMessage("مقطع تحصیلی الزامی است.").MaximumLength(100);
        RuleFor(x => x.FieldOfStudy).NotEmpty().WithMessage("رشته تحصیلی الزامی است.").MaximumLength(150);
        RuleFor(x => x.InstitutionName).NotEmpty().WithMessage("نام مؤسسه/دانشگاه الزامی است.").MaximumLength(200);
        RuleFor(x => x.GraduationYear).InclusiveBetween(1300, 1500)
            .WithMessage("سال فارغ‌التحصیلی نامعتبر است.")
            .When(x => x.GraduationYear.HasValue);
    }
}

public sealed class AddCandidateEducationCommandHandler : IRequestHandler<AddCandidateEducationCommand, Result<CandidateDto>>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICandidateEducationRepository _educationRepository;
    private readonly ICandidateWorkExperienceRepository _workExperienceRepository;
    private readonly ICandidateCertificationRepository _certificationRepository;
    private readonly ICandidateSkillRepository _skillRepository;
    private readonly ICandidateLanguageRepository _languageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AddCandidateEducationCommandHandler(
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

    public async Task<Result<CandidateDto>> Handle(AddCandidateEducationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CandidateDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var candidate = await _candidateRepository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (candidate is null)
            return Result.Failure<CandidateDto>(Error.NotFound("CANDIDATE_NOT_FOUND", "ابتدا باید پروفایل/رزومه خود را تکمیل کنید."));

        var education = CandidateEducation.Create(candidate.Id, request.DegreeLevel, request.FieldOfStudy, request.InstitutionName, request.GraduationYear);
        _educationRepository.Add(education);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var educations = await _educationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var workExperiences = await _workExperienceRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var certifications = await _certificationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var skills = await _skillRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var languages = await _languageRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);

        return Result.Success(CandidateMapper.ToDto(candidate, educations, workExperiences, certifications, skills, languages));
    }
}
