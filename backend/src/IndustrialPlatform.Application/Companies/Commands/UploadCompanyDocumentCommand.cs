using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Companies;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Companies.Commands;

/// <summary>
/// آپلود یک مدرک هویتی/ثبتی شرکت (کارت ملی، آگهی تاسیس، پروانه بهره‌برداری و...).
/// طبق تصمیم محصولی: شرکت باید حداقل یک مدرک ارسال کرده باشد تا بتواند درخواست احراز هویت بدهد
/// (به RequestCompanyVerificationCommand مراجعه شود).
/// </summary>
public sealed record UploadCompanyDocumentCommand(Stream FileContent, string FileName, string DocumentType, long FileSizeBytes = 0) : IRequest<Result<CompanyDocumentDto>>;

public sealed class UploadCompanyDocumentCommandValidator : AbstractValidator<UploadCompanyDocumentCommand>
{
    // طبق راهنمای متنی صفحه پروفایل شرکت (CompanyProfilePage.tsx): «فرمت PDF یا تصویر (JPG، PNG) و
    // حداکثر حجم ۵ مگابایت». پیش از این اصلاح، این Validator هیچ‌کدام از این دو قانون را واقعاً بررسی
    // نمی‌کرد — نه فرمت فایل و نه حجم آن — و FileSizeBytes صرفاً برای ثبت متادیتا استفاده می‌شد، نه
    // اعتبارسنجی. اکنون دقیقاً مطابق الگوی UploadCandidateAvatarCommand/CreateSupportTicketCommand اصلاح شد.
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // ۵ مگابایت

    public UploadCompanyDocumentCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.FileName)
            .Must(name => AllowedExtensions.Contains(Path.GetExtension(name).ToLowerInvariant()))
            .WithMessage("فرمت مدرک باید pdf، jpg، jpeg یا png باشد.");
        RuleFor(x => x.DocumentType).IsEnumName(typeof(CompanyDocumentType)).WithMessage("نوع مدرک نامعتبر است.");
        RuleFor(x => x.FileSizeBytes)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .When(x => x.FileSizeBytes > 0)
            .WithMessage("حجم مدرک نباید بیشتر از ۵ مگابایت باشد.");
    }
}

public sealed class UploadCompanyDocumentCommandHandler : IRequestHandler<UploadCompanyDocumentCommand, Result<CompanyDocumentDto>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanyDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UploadCompanyDocumentCommandHandler(
        ICompanyRepository companyRepository,
        ICompanyDocumentRepository documentRepository,
        IFileStorageService fileStorage,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _companyRepository = companyRepository;
        _documentRepository = documentRepository;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CompanyDocumentDto>> Handle(UploadCompanyDocumentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CompanyDocumentDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var company = await _companyRepository.GetByOwnerUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (company is null)
            return Result.Failure<CompanyDocumentDto>(Error.NotFound("COMPANY_NOT_FOUND", "ابتدا باید پروفایل شرکت خود را ثبت کنید."));

        // طبق CLAUDE.md («الگوی نتیجه») — همان اصلاح CreateSupportTicketCommand: به‌جای Enum.Parse
        // (throw-کننده، که کاملاً به اجرای موفق IsEnumName در Validator بالا متکی بود)، اینجا با
        // TryParse مستقل و ایمن بررسی می‌شود؛ در صورت شکست Result.Failure کنترل‌شده برمی‌گردد نه یک
        // ArgumentException مدیریت‌نشده که به خطای 500 می‌انجامید.
        if (!Enum.TryParse<CompanyDocumentType>(request.DocumentType, out var documentType))
            return Result.Failure<CompanyDocumentDto>(Error.Validation("DOCUMENT_TYPE_INVALID", "نوع مدرک نامعتبر است."));

        var fileUrl = await _fileStorage.SaveAsync(request.FileContent, request.FileName, "company-documents", cancellationToken);

        var document = CompanyDocument.Create(company.Id, documentType, request.FileName, fileUrl, request.FileSizeBytes);

        _documentRepository.Add(document);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CompanyDocumentDto(
            document.Id, document.CompanyId, document.DocumentType.ToString(), document.FileName, document.FileUrl, document.FileSizeBytes, document.CreatedAtUtc));
    }
}
