using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Commands;

/// <summary>
/// آپلود/جایگزینی تصویر آواتار کارجو — طبق تصمیم صریح محصولی این فاز: هنگام Apply روی یک آگهی مشخص،
/// این آواتار به همراه سایر مشخصات هویتی در اختیار کارفرمای همان آگهی قرار می‌گیرد (نمای «آخرین
/// رزومه‌های دریافتی» داشبورد شرکت). الگوی این Command دقیقاً مطابق UploadCompanyDocumentCommand است.
/// </summary>
public sealed record UploadCandidateAvatarCommand(Stream FileContent, string FileName, long FileSizeBytes = 0) : IRequest<Result<CandidateDto>>;

public sealed class UploadCandidateAvatarCommandValidator : AbstractValidator<UploadCandidateAvatarCommand>
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // ۵ مگابایت

    public UploadCandidateAvatarCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.FileName)
            .Must(name => AllowedExtensions.Contains(Path.GetExtension(name).ToLowerInvariant()))
            .WithMessage("فرمت تصویر باید jpg، jpeg، png یا webp باشد.");
        // FileSizeBytes پیش‌فرض صفر دارد (برای فراخوانی‌های مستقیم بدون اطلاع از حجم)؛ اعتبارسنجی فقط
        // وقتی اندپوینت مقدار واقعی IFormFile.Length را ارسال کرده باشد اعمال می‌شود.
        RuleFor(x => x.FileSizeBytes)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .When(x => x.FileSizeBytes > 0)
            .WithMessage("حجم تصویر نباید بیشتر از ۵ مگابایت باشد.");
    }
}

public sealed class UploadCandidateAvatarCommandHandler : IRequestHandler<UploadCandidateAvatarCommand, Result<CandidateDto>>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICandidateEducationRepository _educationRepository;
    private readonly ICandidateWorkExperienceRepository _workExperienceRepository;
    private readonly ICandidateCertificationRepository _certificationRepository;
    private readonly ICandidateSkillRepository _skillRepository;
    private readonly ICandidateLanguageRepository _languageRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UploadCandidateAvatarCommandHandler(
        ICandidateRepository candidateRepository,
        ICandidateEducationRepository educationRepository,
        ICandidateWorkExperienceRepository workExperienceRepository,
        ICandidateCertificationRepository certificationRepository,
        ICandidateSkillRepository skillRepository,
        ICandidateLanguageRepository languageRepository,
        IFileStorageService fileStorage,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _candidateRepository = candidateRepository;
        _educationRepository = educationRepository;
        _workExperienceRepository = workExperienceRepository;
        _certificationRepository = certificationRepository;
        _skillRepository = skillRepository;
        _languageRepository = languageRepository;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CandidateDto>> Handle(UploadCandidateAvatarCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CandidateDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var candidate = await _candidateRepository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (candidate is null)
            return Result.Failure<CandidateDto>(Error.NotFound("CANDIDATE_NOT_FOUND", "ابتدا باید پروفایل/رزومه خود را تکمیل کنید."));

        var avatarUrl = await _fileStorage.SaveAsync(request.FileContent, request.FileName, "candidate-avatars", cancellationToken);
        candidate.SetAvatarUrl(avatarUrl);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var educations = await _educationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var workExperiences = await _workExperienceRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var certifications = await _certificationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var skills = await _skillRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var languages = await _languageRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);

        return Result.Success(CandidateMapper.ToDto(candidate, educations, workExperiences, certifications, skills, languages));
    }
}
