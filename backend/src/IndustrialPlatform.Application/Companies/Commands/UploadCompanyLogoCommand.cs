using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Companies.Commands;

/// <summary>
/// آپلود/جایگزینی لوگوی شرکت — طبق فاز بازطراحی صفحه پروفایل شرکت. الگوی این Command دقیقاً
/// مطابق UploadCandidateAvatarCommand است (Handler شرکت کاربر جاری را از روی مالکیت پیدا می‌کند).
/// </summary>
public sealed record UploadCompanyLogoCommand(Stream FileContent, string FileName, long FileSizeBytes = 0) : IRequest<Result<CompanyDto>>;

public sealed class UploadCompanyLogoCommandValidator : AbstractValidator<UploadCompanyLogoCommand>
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // ۵ مگابایت — مطابق UploadCandidateAvatarCommand

    public UploadCompanyLogoCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.FileName)
            .Must(name => AllowedExtensions.Contains(Path.GetExtension(name).ToLowerInvariant()))
            .WithMessage("فرمت تصویر باید jpg، jpeg، png یا webp باشد.");
        RuleFor(x => x.FileSizeBytes)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .When(x => x.FileSizeBytes > 0)
            .WithMessage("حجم تصویر نباید بیشتر از ۵ مگابایت باشد.");
    }
}

public sealed class UploadCompanyLogoCommandHandler : IRequestHandler<UploadCompanyLogoCommand, Result<CompanyDto>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UploadCompanyLogoCommandHandler(
        ICompanyRepository companyRepository,
        IFileStorageService fileStorage,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _companyRepository = companyRepository;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CompanyDto>> Handle(UploadCompanyLogoCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CompanyDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var company = await _companyRepository.GetByOwnerUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (company is null)
            return Result.Failure<CompanyDto>(Error.NotFound("COMPANY_NOT_FOUND", "ابتدا باید پروفایل شرکت خود را ثبت کنید."));

        var logoUrl = await _fileStorage.SaveAsync(request.FileContent, request.FileName, "company-logos", cancellationToken);
        company.SetLogo(logoUrl);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CompanyMapper.ToDto(company));
    }
}
