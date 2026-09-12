using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Companies;
using IndustrialPlatform.Shared.Api;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Companies.Queries;

public sealed record GetMyCompanyQuery : IRequest<Result<CompanyDto>>;

public sealed class GetMyCompanyQueryHandler : IRequestHandler<GetMyCompanyQuery, Result<CompanyDto>>
{
    private readonly ICompanyRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetMyCompanyQueryHandler(ICompanyRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<CompanyDto>> Handle(GetMyCompanyQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CompanyDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var company = await _repository.GetByOwnerUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        return company is null
            ? Result.Failure<CompanyDto>(Error.NotFound("COMPANY_NOT_FOUND", "شما هنوز شرکتی ثبت نکرده‌اید."))
            : Result.Success(CompanyMapper.ToDto(company));
    }
}

public sealed record GetCompanyByIdQuery(Guid CompanyId) : IRequest<Result<CompanyDto>>;

public sealed class GetCompanyByIdQueryHandler : IRequestHandler<GetCompanyByIdQuery, Result<CompanyDto>>
{
    private readonly ICompanyRepository _repository;

    public GetCompanyByIdQueryHandler(ICompanyRepository repository) => _repository = repository;

    public async Task<Result<CompanyDto>> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        var company = await _repository.GetByIdAsync(request.CompanyId, cancellationToken);
        return company is null
            ? Result.Failure<CompanyDto>(Error.NotFound("COMPANY_NOT_FOUND", "شرکت مورد نظر یافت نشد."))
            : Result.Success(CompanyMapper.ToDto(company));
    }
}

/// <summary>لیست مدارک ارسالی یک شرکت — قابل استفاده هم توسط خود شرکت و هم ادمین در حین بررسی.</summary>
public sealed record GetCompanyDocumentsQuery(Guid CompanyId) : IRequest<Result<IReadOnlyList<CompanyDocumentDto>>>;

public sealed class GetCompanyDocumentsQueryHandler : IRequestHandler<GetCompanyDocumentsQuery, Result<IReadOnlyList<CompanyDocumentDto>>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanyDocumentRepository _documentRepository;
    private readonly ICurrentUserService _currentUser;

    public GetCompanyDocumentsQueryHandler(
        ICompanyRepository companyRepository, ICompanyDocumentRepository documentRepository, ICurrentUserService currentUser)
    {
        _companyRepository = companyRepository;
        _documentRepository = documentRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<CompanyDocumentDto>>> Handle(GetCompanyDocumentsQuery request, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(request.CompanyId, cancellationToken);
        if (company is null)
            return Result.Failure<IReadOnlyList<CompanyDocumentDto>>(Error.NotFound("COMPANY_NOT_FOUND", "شرکت مورد نظر یافت نشد."));

        var isOwner = _currentUser.UserId.HasValue && company.IsOwnedBy(_currentUser.UserId.Value);
        if (!isOwner && !_currentUser.IsInRole("Admin"))
            return Result.Failure<IReadOnlyList<CompanyDocumentDto>>(Error.Forbidden("FORBIDDEN", "شما اجازه مشاهده مدارک این شرکت را ندارید."));

        var documents = await _documentRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);
        var dtoItems = documents
            .Select(d => new CompanyDocumentDto(d.Id, d.CompanyId, d.DocumentType.ToString(), d.FileName, d.FileUrl, d.FileSizeBytes, d.CreatedAtUtc))
            .ToList();

        return Result.Success<IReadOnlyList<CompanyDocumentDto>>(dtoItems);
    }
}

public sealed record GetPendingCompaniesQuery(int Page, int PageSize) : IRequest<Result<PagedResult<CompanyDto>>>;

public sealed class GetPendingCompaniesQueryHandler : IRequestHandler<GetPendingCompaniesQuery, Result<PagedResult<CompanyDto>>>
{
    private readonly ICompanyRepository _repository;

    public GetPendingCompaniesQueryHandler(ICompanyRepository repository) => _repository = repository;

    public async Task<Result<PagedResult<CompanyDto>>> Handle(GetPendingCompaniesQuery request, CancellationToken cancellationToken)
    {
        var paged = await _repository.GetByVerificationStatusAsync(
            VerificationStatus.PendingVerification, request.Page, request.PageSize, cancellationToken);

        var dtoItems = paged.Items.Select(CompanyMapper.ToDto).ToList();
        return Result.Success(PagedResult<CompanyDto>.Create(dtoItems, paged.TotalCount, paged.Page, paged.PageSize));
    }
}
