using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Shared.Api;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.JobAds.Queries;

/// <summary>جست‌وجوی پیشرفته آگهی — بر اساس شهرک، شهر، شیفت، سرویس، نوع قرارداد و حداقل حقوق.</summary>
public sealed record SearchJobAdsQuery(
    Guid? IndustrialZoneId,
    Guid? CityId,
    string? WorkShift,
    bool? HasCommuteService,
    string? ContractType,
    decimal? MinSalaryAmount,
    string? Keyword,
    int Page,
    int PageSize,
    string SortBy,
    string SortDir) : IRequest<Result<PagedResult<JobAdDto>>>;

public sealed class SearchJobAdsQueryHandler : IRequestHandler<SearchJobAdsQuery, Result<PagedResult<JobAdDto>>>
{
    private readonly IJobAdRepository _repository;
    private readonly ICompanyRepository _companyRepository;

    public SearchJobAdsQueryHandler(IJobAdRepository repository, ICompanyRepository companyRepository)
    {
        _repository = repository;
        _companyRepository = companyRepository;
    }

    public async Task<Result<PagedResult<JobAdDto>>> Handle(SearchJobAdsQuery request, CancellationToken cancellationToken)
    {
        Domain.JobAds.WorkShift? workShift = null;
        if (!string.IsNullOrWhiteSpace(request.WorkShift) && Enum.TryParse<Domain.JobAds.WorkShift>(request.WorkShift, out var parsedShift))
        {
            workShift = parsedShift;
        }

        Domain.JobAds.ContractType? contractType = null;
        if (!string.IsNullOrWhiteSpace(request.ContractType) && Enum.TryParse<Domain.JobAds.ContractType>(request.ContractType, out var parsedContract))
        {
            contractType = parsedContract;
        }

        var filter = new JobAdSearchFilter(
            request.IndustrialZoneId, request.CityId, workShift, request.HasCommuteService,
            contractType, request.MinSalaryAmount, request.Keyword);

        var paged = await _repository.SearchPublishedAsync(
            filter, request.Page, request.PageSize, request.SortBy, request.SortDir, cancellationToken);

        var companiesById = await LoadCompaniesAsync(paged.Items.Select(a => a.CompanyId), _companyRepository, cancellationToken);
        var dtoItems = paged.Items.Select(a => JobAdMapper.ToDto(a, companiesById.GetValueOrDefault(a.CompanyId))).ToList();
        return Result.Success(PagedResult<JobAdDto>.Create(dtoItems, paged.TotalCount, paged.Page, paged.PageSize));
    }

    /// <summary>طبق ADR-011 — بارگذاری دسته‌ای شرکت‌های متمایز یک صفحه نتیجه، برای پرهیز از N+1 غیرضروری.</summary>
    internal static async Task<Dictionary<Guid, Domain.Companies.Company>> LoadCompaniesAsync(
        IEnumerable<Guid> companyIds, ICompanyRepository companyRepository, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, Domain.Companies.Company>();
        foreach (var companyId in companyIds.Distinct())
        {
            var company = await companyRepository.GetByIdAsync(companyId, cancellationToken);
            if (company is not null)
                result[companyId] = company;
        }

        return result;
    }
}

public sealed record GetJobAdByIdQuery(Guid JobAdId) : IRequest<Result<JobAdDto>>;

public sealed class GetJobAdByIdQueryHandler : IRequestHandler<GetJobAdByIdQuery, Result<JobAdDto>>
{
    private readonly IJobAdRepository _repository;
    private readonly ICompanyRepository _companyRepository;

    public GetJobAdByIdQueryHandler(IJobAdRepository repository, ICompanyRepository companyRepository)
    {
        _repository = repository;
        _companyRepository = companyRepository;
    }

    public async Task<Result<JobAdDto>> Handle(GetJobAdByIdQuery request, CancellationToken cancellationToken)
    {
        var jobAd = await _repository.GetByIdAsync(request.JobAdId, cancellationToken);
        if (jobAd is null)
            return Result.Failure<JobAdDto>(Error.NotFound("JOB_AD_NOT_FOUND", "آگهی مورد نظر یافت نشد."));

        var company = await _companyRepository.GetByIdAsync(jobAd.CompanyId, cancellationToken);
        return Result.Success(JobAdMapper.ToDto(jobAd, company));
    }
}

public sealed record GetMyCompanyJobAdsQuery(int Page, int PageSize) : IRequest<Result<PagedResult<JobAdDto>>>;

public sealed class GetMyCompanyJobAdsQueryHandler : IRequestHandler<GetMyCompanyJobAdsQuery, Result<PagedResult<JobAdDto>>>
{
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyCompanyJobAdsQueryHandler(IJobAdRepository jobAdRepository, ICompanyRepository companyRepository, ICurrentUserService currentUser)
    {
        _jobAdRepository = jobAdRepository;
        _companyRepository = companyRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<JobAdDto>>> Handle(GetMyCompanyJobAdsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<PagedResult<JobAdDto>>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var company = await _companyRepository.GetByOwnerUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (company is null)
            return Result.Failure<PagedResult<JobAdDto>>(Error.NotFound("COMPANY_NOT_FOUND", "شما هنوز شرکتی ثبت نکرده‌اید."));

        var paged = await _jobAdRepository.GetByCompanyIdAsync(company.Id, request.Page, request.PageSize, cancellationToken);
        var dtoItems = paged.Items.Select(a => JobAdMapper.ToDto(a, company)).ToList();

        return Result.Success(PagedResult<JobAdDto>.Create(dtoItems, paged.TotalCount, paged.Page, paged.PageSize));
    }
}

/// <summary>لیست آگهی‌های در انتظار بررسی — برای پنل Owner.</summary>
public sealed record GetPendingReviewJobAdsQuery(int Page, int PageSize) : IRequest<Result<PagedResult<JobAdDto>>>;

public sealed class GetPendingReviewJobAdsQueryHandler : IRequestHandler<GetPendingReviewJobAdsQuery, Result<PagedResult<JobAdDto>>>
{
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICompanyRepository _companyRepository;

    public GetPendingReviewJobAdsQueryHandler(IJobAdRepository jobAdRepository, ICompanyRepository companyRepository)
    {
        _jobAdRepository = jobAdRepository;
        _companyRepository = companyRepository;
    }

    public async Task<Result<PagedResult<JobAdDto>>> Handle(GetPendingReviewJobAdsQuery request, CancellationToken cancellationToken)
    {
        var paged = await _jobAdRepository.GetPendingReviewAsync(request.Page, request.PageSize, cancellationToken);
        var companiesById = await SearchJobAdsQueryHandler.LoadCompaniesAsync(paged.Items.Select(a => a.CompanyId), _companyRepository, cancellationToken);
        var dtoItems = paged.Items.Select(a => JobAdMapper.ToDto(a, companiesById.GetValueOrDefault(a.CompanyId))).ToList();
        return Result.Success(PagedResult<JobAdDto>.Create(dtoItems, paged.TotalCount, paged.Page, paged.PageSize));
    }
}
