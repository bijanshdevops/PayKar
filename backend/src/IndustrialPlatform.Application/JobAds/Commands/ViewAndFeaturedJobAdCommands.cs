using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Domain.Companies;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.JobAds.Commands;

/// <summary>
/// ثبت یک بازدید واقعی از صفحه جزئیات آگهی — طبق تصمیم صریح محصولی این فاز («فیلد ViewsCount اضافه شود»).
/// عمداً به‌صورت یک Command مجزا (نه side-effect داخل GetJobAdByIdQuery) پیاده‌سازی شده تا اصل CQRS
/// این پروژه (Query خالص/بدون تغییر وضعیت) حفظ شود؛ فرانت‌اند این Command را یک‌بار در mount شدن
/// صفحه جزئیات آگهی فراخوانی می‌کند. بدون احراز هویت (هر بازدیدکننده، حتی مهمان).
/// </summary>
public sealed record RecordJobAdViewCommand(Guid JobAdId) : IRequest<Result>;

public sealed class RecordJobAdViewCommandHandler : IRequestHandler<RecordJobAdViewCommand, Result>
{
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICompanyDailyViewStatRepository _dailyViewStatRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RecordJobAdViewCommandHandler(
        IJobAdRepository jobAdRepository,
        ICompanyDailyViewStatRepository dailyViewStatRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _jobAdRepository = jobAdRepository;
        _dailyViewStatRepository = dailyViewStatRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RecordJobAdViewCommand request, CancellationToken cancellationToken)
    {
        var jobAd = await _jobAdRepository.GetByIdAsync(request.JobAdId, cancellationToken);
        if (jobAd is null)
            return Result.Failure(Error.NotFound("JOB_AD_NOT_FOUND", "آگهی مورد نظر یافت نشد."));

        jobAd.IncrementViewsCount();

        // طبق فاز بازطراحی داشبورد کارفرما — تجمیع بازدید روزانه شرکت برای اسپارک‌لاین/نمودار روند ۳۰ روزه.
        var today = _dateTimeProvider.UtcNow.Date;
        var dailyStat = await _dailyViewStatRepository.GetByCompanyAndDateAsync(jobAd.CompanyId, today, cancellationToken);
        if (dailyStat is null)
        {
            dailyStat = CompanyDailyViewStat.Create(jobAd.CompanyId, today);
            _dailyViewStatRepository.Add(dailyStat);
        }
        dailyStat.IncrementViews();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>
/// تغییر فلگ «ویژه» یک آگهی توسط ادمین — طبق تصمیم صریح محصولی این فاز: «IsFeatured فقط به‌عنوان
/// فلگ ساده برای نمایش در کاروسل ویژه صفحه اصلی، بدون نیاز به پلن پیچیده AdBoost». جایگزین کامل و
/// عمدی سیستم AdBoostPlan حذف‌شده در تسک #38 — هیچ تراکنش مالی/پرداختی به این عملیات متصل نیست.
/// </summary>
public sealed record SetJobAdFeaturedCommand(Guid JobAdId, bool IsFeatured) : IRequest<Result<JobAdDto>>;

public sealed class SetJobAdFeaturedCommandHandler : IRequestHandler<SetJobAdFeaturedCommand, Result<JobAdDto>>
{
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetJobAdFeaturedCommandHandler(IJobAdRepository jobAdRepository, ICompanyRepository companyRepository, IUnitOfWork unitOfWork)
    {
        _jobAdRepository = jobAdRepository;
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<JobAdDto>> Handle(SetJobAdFeaturedCommand request, CancellationToken cancellationToken)
    {
        var jobAd = await _jobAdRepository.GetByIdAsync(request.JobAdId, cancellationToken);
        if (jobAd is null)
            return Result.Failure<JobAdDto>(Error.NotFound("JOB_AD_NOT_FOUND", "آگهی مورد نظر یافت نشد."));

        jobAd.SetFeatured(request.IsFeatured);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var company = await _companyRepository.GetByIdAsync(jobAd.CompanyId, cancellationToken);
        return Result.Success(JobAdMapper.ToDto(jobAd, company));
    }
}
