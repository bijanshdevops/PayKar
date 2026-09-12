using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Companies.Commands;

public sealed record RequestCompanyVerificationCommand(Guid CompanyId) : IRequest<Result<CompanyDto>>;
public sealed record ApproveCompanyCommand(Guid CompanyId) : IRequest<Result<CompanyDto>>;
public sealed record RejectCompanyCommand(Guid CompanyId) : IRequest<Result<CompanyDto>>;

public sealed class RequestCompanyVerificationCommandHandler
    : IRequestHandler<RequestCompanyVerificationCommand, Result<CompanyDto>>
{
    private readonly ICompanyRepository _repository;
    private readonly ICompanyDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RequestCompanyVerificationCommandHandler(
        ICompanyRepository repository, ICompanyDocumentRepository documentRepository, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _repository = repository;
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CompanyDto>> Handle(RequestCompanyVerificationCommand request, CancellationToken cancellationToken)
    {
        var company = await _repository.GetByIdAsync(request.CompanyId, cancellationToken);
        if (company is null)
            return Result.Failure<CompanyDto>(Error.NotFound("COMPANY_NOT_FOUND", "شرکت مورد نظر یافت نشد."));

        if (_currentUser.UserId is null || !company.IsOwnedBy(_currentUser.UserId.Value))
            return Result.Failure<CompanyDto>(Error.Forbidden("FORBIDDEN", "شما اجازه این عملیات را ندارید."));

        // طبق تصمیم محصولی: بدون ارسال حداقل یک مدرک هویتی/ثبتی، درخواست احراز هویت پذیرفته نمی‌شود.
        var documentCount = await _documentRepository.CountByCompanyIdAsync(company.Id, cancellationToken);
        if (documentCount == 0)
            return Result.Failure<CompanyDto>(Error.Validation("DOCUMENTS_REQUIRED", "ابتدا باید حداقل یک مدرک هویتی/ثبتی شرکت را آپلود کنید."));

        var result = company.RequestVerification();
        if (result.IsFailure)
            return Result.Failure<CompanyDto>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(CompanyMapper.ToDto(company));
    }
}

/// <summary>یادداشت: Endpoint این Command با Policy "AdminOnly" محافظت می‌شود (سند 05، بخش ۳).</summary>
public sealed class ApproveCompanyCommandHandler : IRequestHandler<ApproveCompanyCommand, Result<CompanyDto>>
{
    private readonly ICompanyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveCompanyCommandHandler(ICompanyRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CompanyDto>> Handle(ApproveCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await _repository.GetByIdAsync(request.CompanyId, cancellationToken);
        if (company is null)
            return Result.Failure<CompanyDto>(Error.NotFound("COMPANY_NOT_FOUND", "شرکت مورد نظر یافت نشد."));

        company.Approve();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(CompanyMapper.ToDto(company));
    }
}

/// <summary>یادداشت: Endpoint این Command نیز فقط برای نقش Admin مجاز است.</summary>
public sealed class RejectCompanyCommandHandler : IRequestHandler<RejectCompanyCommand, Result<CompanyDto>>
{
    private readonly ICompanyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RejectCompanyCommandHandler(ICompanyRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CompanyDto>> Handle(RejectCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await _repository.GetByIdAsync(request.CompanyId, cancellationToken);
        if (company is null)
            return Result.Failure<CompanyDto>(Error.NotFound("COMPANY_NOT_FOUND", "شرکت مورد نظر یافت نشد."));

        company.Reject();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(CompanyMapper.ToDto(company));
    }
}
