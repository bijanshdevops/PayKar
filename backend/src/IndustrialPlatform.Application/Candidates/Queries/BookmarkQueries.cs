using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Application.JobAds;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Queries;

/// <summary>
/// شناسهٔ تمام آگهی‌های نشان‌شدهٔ کارجوی واردشده — سبک و بدون جزئیات، صرفاً برای تعیین وضعیت
/// (نشان‌شده/نشان‌نشده) آیکون بوک‌مارک روی کارت‌های آگهی در هر صفحه‌ای که نمایش داده می‌شوند.
/// </summary>
public sealed record GetMyBookmarkedJobIdsQuery : IRequest<Result<IReadOnlyList<Guid>>>;

public sealed class GetMyBookmarkedJobIdsQueryHandler : IRequestHandler<GetMyBookmarkedJobIdsQuery, Result<IReadOnlyList<Guid>>>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IBookmarkedJobRepository _bookmarkRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyBookmarkedJobIdsQueryHandler(
        ICandidateRepository candidateRepository, IBookmarkedJobRepository bookmarkRepository, ICurrentUserService currentUser)
    {
        _candidateRepository = candidateRepository;
        _bookmarkRepository = bookmarkRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<Guid>>> Handle(GetMyBookmarkedJobIdsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Success<IReadOnlyList<Guid>>(Array.Empty<Guid>());

        var candidate = await _candidateRepository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (candidate is null)
            return Result.Success<IReadOnlyList<Guid>>(Array.Empty<Guid>());

        var bookmarks = await _bookmarkRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        return Result.Success<IReadOnlyList<Guid>>(bookmarks.Select(b => b.JobAdId).ToList());
    }
}

/// <summary>صفحهٔ «آگهی‌های نشان‌شده» — جزئیات کامل هر آگهی، جدیدترین نشان‌شده ابتدا.</summary>
public sealed record GetMyBookmarkedJobsQuery : IRequest<Result<IReadOnlyList<JobAdDto>>>;

public sealed class GetMyBookmarkedJobsQueryHandler : IRequestHandler<GetMyBookmarkedJobsQuery, Result<IReadOnlyList<JobAdDto>>>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IBookmarkedJobRepository _bookmarkRepository;
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyBookmarkedJobsQueryHandler(
        ICandidateRepository candidateRepository, IBookmarkedJobRepository bookmarkRepository,
        IJobAdRepository jobAdRepository, ICompanyRepository companyRepository, ICurrentUserService currentUser)
    {
        _candidateRepository = candidateRepository;
        _bookmarkRepository = bookmarkRepository;
        _jobAdRepository = jobAdRepository;
        _companyRepository = companyRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<JobAdDto>>> Handle(GetMyBookmarkedJobsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<IReadOnlyList<JobAdDto>>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var candidate = await _candidateRepository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (candidate is null)
            return Result.Success<IReadOnlyList<JobAdDto>>(Array.Empty<JobAdDto>());

        var bookmarks = await _bookmarkRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);

        var result = new List<JobAdDto>(bookmarks.Count);
        foreach (var bookmark in bookmarks)
        {
            var jobAd = await _jobAdRepository.GetByIdAsync(bookmark.JobAdId, cancellationToken);
            if (jobAd is null)
                continue; // آگهی حذف‌شده — نادیده گرفته می‌شود.

            var company = await _companyRepository.GetByIdAsync(jobAd.CompanyId, cancellationToken);
            result.Add(JobAdMapper.ToDto(jobAd, company));
        }

        return Result.Success<IReadOnlyList<JobAdDto>>(result);
    }
}
