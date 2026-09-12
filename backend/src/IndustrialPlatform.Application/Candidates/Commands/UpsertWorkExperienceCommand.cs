using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Candidates;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Commands;

/// <summary>
/// افزودن یا ویرایش اتمیک یک ردیف سابقه شغلی — طبق فاز «پروفایل و رزومه‌ساز کارجو». Id=null یعنی
/// ایجاد ردیف جدید (هم‌معنا با AddCandidateWorkExperienceCommand که به‌دلیل سازگاری با فرانت‌اند
/// فعلی دست‌نخورده باقی مانده)؛ Id غیرخالی یعنی ویرایش درجای ردیف موجود (قابلیت جدیدی که تا پیش از
/// این فاز روی CandidateWorkExperience وجود نداشت — ر.ک. متد جدید CandidateWorkExperience.Update).
/// «تاریخ‌های شمسی/میلادی»: این پروژه در سراسر خود سال شمسی عددی (Jalali Year) ذخیره می‌کند و هیچ
/// کتابخانه تقویم شمسی/تبدیل تاریخ کامل ندارد؛ این Command همان دانه‌بندی سال (StartYear/EndYear:int)
/// موجود و از قبل به فرانت‌اند متصل‌شده را می‌پذیرد، نه یک تاریخ کامل روز/ماه/سال میلادی یا شمسی —
/// ارتقا به دانه‌بندی روز/ماه یک تغییر Breaking روی ستون‌های موجود دیتابیس/فرانت‌اند است که در این
/// فاز (که صراحتاً فقط لایه Application/Api را هدف قرار داده) انجام نشده.
/// </summary>
public sealed record UpsertWorkExperienceCommand(
    Guid? Id,
    string JobTitle,
    string CompanyName,
    int StartYear,
    int? EndYear = null,
    string? Description = null) : IRequest<Result<CandidateDto>>;

public sealed class UpsertWorkExperienceCommandValidator : AbstractValidator<UpsertWorkExperienceCommand>
{
    public UpsertWorkExperienceCommandValidator()
    {
        RuleFor(x => x.JobTitle).NotEmpty().WithMessage("عنوان شغلی الزامی است.").MaximumLength(150);
        RuleFor(x => x.CompanyName).NotEmpty().WithMessage("نام شرکت/کارفرما الزامی است.").MaximumLength(200);
        RuleFor(x => x.StartYear).InclusiveBetween(1300, 1500).WithMessage("سال شروع نامعتبر است.");
        RuleFor(x => x.EndYear).InclusiveBetween(1300, 1500).WithMessage("سال پایان نامعتبر است.").When(x => x.EndYear.HasValue);
        RuleFor(x => x)
            .Must(x => !x.EndYear.HasValue || x.EndYear.Value >= x.StartYear)
            .WithMessage("سال پایان نمی‌تواند قبل از سال شروع باشد.")
            .WithName("EndYear");
    }
}

public sealed class UpsertWorkExperienceCommandHandler : IRequestHandler<UpsertWorkExperienceCommand, Result<CandidateDto>>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICandidateEducationRepository _educationRepository;
    private readonly ICandidateWorkExperienceRepository _workExperienceRepository;
    private readonly ICandidateCertificationRepository _certificationRepository;
    private readonly ICandidateSkillRepository _skillRepository;
    private readonly ICandidateLanguageRepository _languageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpsertWorkExperienceCommandHandler(
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

    public async Task<Result<CandidateDto>> Handle(UpsertWorkExperienceCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CandidateDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var candidate = await _candidateRepository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (candidate is null)
            return Result.Failure<CandidateDto>(Error.NotFound("CANDIDATE_NOT_FOUND", "ابتدا باید پروفایل/رزومه خود را تکمیل کنید."));

        if (request.Id is null)
        {
            var created = CandidateWorkExperience.Create(
                candidate.Id, request.JobTitle, request.CompanyName, request.StartYear, request.EndYear, request.Description);
            _workExperienceRepository.Add(created);
        }
        else
        {
            var existing = await _workExperienceRepository.GetByIdAsync(request.Id.Value, cancellationToken);
            if (existing is null)
                return Result.Failure<CandidateDto>(Error.NotFound("WORK_EXPERIENCE_NOT_FOUND", "سابقه شغلی مورد نظر یافت نشد."));

            if (existing.CandidateId != candidate.Id)
                return Result.Failure<CandidateDto>(Error.Forbidden("FORBIDDEN", "شما اجازه ویرایش این سابقه شغلی را ندارید."));

            existing.Update(request.JobTitle, request.CompanyName, request.StartYear, request.EndYear, request.Description);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var educations = await _educationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var workExperiences = await _workExperienceRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var certifications = await _certificationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var skills = await _skillRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var languages = await _languageRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);

        return Result.Success(CandidateMapper.ToDto(candidate, educations, workExperiences, certifications, skills, languages));
    }
}
