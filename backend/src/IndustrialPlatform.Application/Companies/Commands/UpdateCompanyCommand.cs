using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Shared.Extensions;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Companies.Commands;

public sealed record UpdateCompanyCommand(
    Guid CompanyId,
    string Name,
    string AddressDetail,
    string IndustryCategory,
    Guid IndustrialZoneId,
    string? ContactPhoneNumber = null,
    string? Website = null,
    string? Email = null,
    string? Description = null) : IRequest<Result<CompanyDto>>;

public sealed class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AddressDetail).NotEmpty();
        RuleFor(x => x.IndustryCategory).NotEmpty();
        RuleFor(x => x.IndustrialZoneId).NotEmpty();
        // طبق ADR-011 — اختیاری در ویرایش (برای شرکت‌های موجود که هنوز شماره ثبت نکرده‌اند)، اما در صورت ارسال باید معتبر باشد.
        RuleFor(x => x.ContactPhoneNumber)
            .Matches(@"^0\d{9,10}$").WithMessage("شماره تماس باید با صفر شروع شود و ۱۰ یا ۱۱ رقم باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactPhoneNumber));
        RuleFor(x => x.Website)
            .Matches(@"^(https?:\/\/)?[\w.-]+\.[a-zA-Z]{2,}([\/\w.-]*)*\/?$").WithMessage("آدرس وب‌سایت معتبر نیست.")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("ایمیل سازمانی معتبر نیست.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Description).MaximumLength(1000).WithMessage("معرفی شرکت نباید بیش از ۱۰۰۰ کاراکتر باشد.");
    }
}

public sealed class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand, Result<CompanyDto>>
{
    private readonly ICompanyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateCompanyCommandHandler(ICompanyRepository repository, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CompanyDto>> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await _repository.GetByIdAsync(request.CompanyId, cancellationToken);
        if (company is null)
            return Result.Failure<CompanyDto>(Error.NotFound("COMPANY_NOT_FOUND", "شرکت مورد نظر یافت نشد."));

        if (_currentUser.UserId is null || (!company.IsOwnedBy(_currentUser.UserId.Value) && !_currentUser.IsInRole("Admin")))
            return Result.Failure<CompanyDto>(Error.Forbidden("FORBIDDEN", "شما اجازه ویرایش این شرکت را ندارید."));

        company.Update(
            request.Name.NormalizePersian(),
            request.AddressDetail.NormalizePersian(),
            request.IndustryCategory.NormalizePersian(),
            request.IndustrialZoneId,
            string.IsNullOrWhiteSpace(request.ContactPhoneNumber) ? null : request.ContactPhoneNumber.NormalizePersian(),
            request.Website is null ? null : (string.IsNullOrWhiteSpace(request.Website) ? string.Empty : request.Website.Trim()),
            request.Email is null ? null : (string.IsNullOrWhiteSpace(request.Email) ? string.Empty : request.Email.Trim()),
            request.Description is null ? null : (string.IsNullOrWhiteSpace(request.Description) ? string.Empty : request.Description.NormalizePersian()));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CompanyMapper.ToDto(company));
    }
}
