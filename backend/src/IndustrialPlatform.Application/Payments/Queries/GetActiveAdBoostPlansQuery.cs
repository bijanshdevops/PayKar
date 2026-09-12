using IndustrialPlatform.Domain.JobAds;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Payments.Queries;

/// <summary>
/// جایگزین GetActiveAdBoostPlansQuery حذف‌شده — مبلغ ثابت ثبت آگهی را برمی‌گرداند
/// تا فرانت‌اند بدون هاردکد کردن مبلغ، آن را از سرور بخواند.
/// </summary>
public sealed record GetJobAdListingFeeQuery : IRequest<Result<JobAdListingFeeDto>>;

public sealed class GetJobAdListingFeeQueryHandler : IRequestHandler<GetJobAdListingFeeQuery, Result<JobAdListingFeeDto>>
{
    public Task<Result<JobAdListingFeeDto>> Handle(GetJobAdListingFeeQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new JobAdListingFeeDto(JobAd.ListingFeeAmountInRials)));
}
