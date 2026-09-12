using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Dashboard.Queries;

/// <summary>طبق تسک #74 — آمار اختصاصی پنل شرکت برای کاربر واردشده (بر اساس شرکت متعلق به او).</summary>
public sealed record GetCompanyDashboardQuery : IRequest<Result<CompanyDashboardDto>>;

public sealed class GetCompanyDashboardQueryHandler : IRequestHandler<GetCompanyDashboardQuery, Result<CompanyDashboardDto>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanyDashboardQueryService _dashboardQueryService;
    private readonly ICurrentUserService _currentUser;

    public GetCompanyDashboardQueryHandler(
        ICompanyRepository companyRepository,
        ICompanyDashboardQueryService dashboardQueryService,
        ICurrentUserService currentUser)
    {
        _companyRepository = companyRepository;
        _dashboardQueryService = dashboardQueryService;
        _currentUser = currentUser;
    }

    public async Task<Result<CompanyDashboardDto>> Handle(GetCompanyDashboardQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CompanyDashboardDto>(Error.Unauthorized("UNAUTHORIZED", "برای مشاهده آمار باید وارد شوید."));

        var company = await _companyRepository.GetByOwnerUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (company is null)
            return Result.Failure<CompanyDashboardDto>(Error.NotFound("COMPANY_NOT_FOUND", "شما هنوز شرکتی ثبت نکرده‌اید."));

        var dashboard = await _dashboardQueryService.GetDashboardAsync(company.Id, cancellationToken);
        return Result.Success(dashboard);
    }
}
