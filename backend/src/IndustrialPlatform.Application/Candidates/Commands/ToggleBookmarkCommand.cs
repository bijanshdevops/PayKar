using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.JobAds;
using IndustrialPlatform.Domain.Candidates;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Commands;

/// <summary>
/// نشان‌کردن/لغو نشان یک آگهی توسط کارجو — طبق 02_home_page_spec.md بخش ۲.۴ («آیکون بوک‌مارک») و
/// 03_jobseeker_dashboard_spec.md بخش ۱ («آگهی‌های نشان‌شده»). به‌صورت Toggle پیاده‌سازی شده تا فرانت‌اند
/// فقط به یک دکمه/یک Mutation نیاز داشته باشد؛ نتیجه true یعنی اکنون نشان‌شده است، false یعنی لغو شد.
/// </summary>
public sealed record ToggleBookmarkCommand(Guid JobAdId) : IRequest<Result<bool>>;

public sealed class ToggleBookmarkCommandHandler : IRequestHandler<ToggleBookmarkCommand, Result<bool>>
{
    private readonly IBookmarkedJobRepository _bookmarkRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly IJobAdRepository _jobAdRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ToggleBookmarkCommandHandler(
        IBookmarkedJobRepository bookmarkRepository,
        ICandidateRepository candidateRepository,
        IJobAdRepository jobAdRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _bookmarkRepository = bookmarkRepository;
        _candidateRepository = candidateRepository;
        _jobAdRepository = jobAdRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(ToggleBookmarkCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<bool>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var candidate = await _candidateRepository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (candidate is null)
            return Result.Failure<bool>(Error.NotFound("CANDIDATE_NOT_FOUND", "ابتدا باید پروفایل/رزومه خود را تکمیل کنید."));

        var existing = await _bookmarkRepository.GetByCandidateAndJobAdAsync(candidate.Id, request.JobAdId, cancellationToken);
        if (existing is not null)
        {
            _bookmarkRepository.Remove(existing);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(false);
        }

        var jobAd = await _jobAdRepository.GetByIdAsync(request.JobAdId, cancellationToken);
        if (jobAd is null)
            return Result.Failure<bool>(Error.NotFound("JOB_AD_NOT_FOUND", "آگهی مورد نظر یافت نشد."));

        var bookmark = BookmarkedJob.Create(candidate.Id, jobAd.Id);
        _bookmarkRepository.Add(bookmark);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}
