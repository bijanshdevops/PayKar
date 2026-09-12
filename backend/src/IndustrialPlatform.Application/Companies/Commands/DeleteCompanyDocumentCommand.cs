using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Companies.Commands;

/// <summary>
/// حذف منطقی یک مدرک ارسالی شرکت — طبق فاز بازطراحی صفحه پروفایل (اکشن «حذف» روی کارت مدرک).
/// طبق مانیفست معماری (بخش ۲.۳): حذف فیزیکی ممنوع است؛ از BaseEntity.MarkAsDeleted استفاده می‌شود
/// که رکورد را از فیلتر سراسری Soft Delete کنار می‌گذارد بدون حذف واقعی از دیتابیس.
/// </summary>
public sealed record DeleteCompanyDocumentCommand(Guid DocumentId) : IRequest<Result>;

public sealed class DeleteCompanyDocumentCommandValidator : AbstractValidator<DeleteCompanyDocumentCommand>
{
    public DeleteCompanyDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
    }
}

public sealed class DeleteCompanyDocumentCommandHandler : IRequestHandler<DeleteCompanyDocumentCommand, Result>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanyDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteCompanyDocumentCommandHandler(
        ICompanyRepository companyRepository,
        ICompanyDocumentRepository documentRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _companyRepository = companyRepository;
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteCompanyDocumentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure(Error.NotFound("DOCUMENT_NOT_FOUND", "مدرک مورد نظر یافت نشد."));

        var company = await _companyRepository.GetByIdAsync(document.CompanyId, cancellationToken);
        if (company is null || (!company.IsOwnedBy(_currentUser.UserId.Value) && !_currentUser.IsInRole("Admin")))
            return Result.Failure(Error.Forbidden("FORBIDDEN", "شما اجازه حذف این مدرک را ندارید."));

        document.MarkAsDeleted(_currentUser.UserId, _dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
