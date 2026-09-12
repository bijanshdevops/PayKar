using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Companies;
using IndustrialPlatform.Shared.Extensions;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Companies.Commands;

public sealed record CreateCompanyCommand(
    string Name,
    string NationalId,
    string RegistrationNumber,
    Guid IndustrialZoneId,
    string AddressDetail,
    string IndustryCategory,
    string ContactPhoneNumber,
    string? Website = null,
    string? Email = null,
    string? Description = null) : IRequest<Result<CompanyDto>>;

public sealed class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("نام شرکت الزامی است.").MaximumLength(200);
        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("شناسه ملی الزامی است.")
            .Must(n => n.IsValidNationalId()).WithMessage("شناسه ملی باید ۱۰ یا ۱۱ رقم باشد.");
        RuleFor(x => x.RegistrationNumber).NotEmpty().WithMessage("شماره ثبت الزامی است.");
        RuleFor(x => x.IndustrialZoneId).NotEmpty().WithMessage("انتخاب شهرک صنعتی الزامی است.");
        RuleFor(x => x.AddressDetail).NotEmpty().WithMessage("آدرس دقیق الزامی است.");
        RuleFor(x => x.IndustryCategory).NotEmpty().WithMessage("دسته‌بندی صنعتی الزامی است.");
        // طبق ADR-011 — این شماره در دکمه «تماس با کارفرما» صفحه جزئیات آگهی نمایش داده می‌شود.
        RuleFor(x => x.ContactPhoneNumber)
            .NotEmpty().WithMessage("شماره تماس شرکت الزامی است.")
            .Matches(@"^0\d{9,10}$").WithMessage("شماره تماس باید با صفر شروع شود و ۱۰ یا ۱۱ رقم باشد.");
        // فیلدهای اختیاری پروفایل — طبق فاز بازطراحی صفحه پروفایل شرکت.
        RuleFor(x => x.Website)
            .Matches(@"^(https?:\/\/)?[\w.-]+\.[a-zA-Z]{2,}([\/\w.-]*)*\/?$").WithMessage("آدرس وب‌سایت معتبر نیست.")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("ایمیل سازمانی معتبر نیست.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Description).MaximumLength(1000).WithMessage("معرفی شرکت نباید بیش از ۱۰۰۰ کاراکتر باشد.");
    }
}

public sealed class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, Result<CompanyDto>>
{
    private readonly ICompanyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateCompanyCommandHandler(ICompanyRepository repository, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CompanyDto>> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CompanyDto>(Error.Unauthorized("UNAUTHORIZED", "برای ثبت شرکت باید وارد شوید."));

        var normalizedNationalId = request.NationalId.NormalizePersian();

        if (await _repository.ExistsWithNationalIdAsync(normalizedNationalId, cancellationToken))
            return Result.Failure<CompanyDto>(Error.Conflict("DUPLICATE_NATIONAL_ID", "شرکتی با این شناسه ملی قبلاً ثبت شده است."));

        var existingCompany = await _repository.GetByOwnerUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (existingCompany is not null)
            return Result.Failure<CompanyDto>(Error.Conflict("COMPANY_ALREADY_EXISTS", "شما پیش‌تر یک شرکت ثبت کرده‌اید."));

        var company = Company.Create(
            request.Name.NormalizePersian(),
            normalizedNationalId,
            request.RegistrationNumber.NormalizePersian(),
            request.IndustrialZoneId,
            request.AddressDetail.NormalizePersian(),
            request.IndustryCategory.NormalizePersian(),
            _currentUser.UserId.Value,
            request.ContactPhoneNumber.NormalizePersian(),
            string.IsNullOrWhiteSpace(request.Website) ? null : request.Website.Trim(),
            string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.NormalizePersian());

        _repository.Add(company);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CompanyMapper.ToDto(company));
    }
}
