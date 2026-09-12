using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Application.JobAds;
using IndustrialPlatform.Domain.Payments;
using IndustrialPlatform.Shared.Api;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Payments.Queries;

/// <summary>
/// صفحهٔ «امور مالی و تراکنش‌ها» در داشبورد کارفرما — طبق 04_company_dashboard_spec.md بخش ۱ و ۳
/// (آیتم منوی «امور مالی» و لینک «تراکنش‌ها» روی کارت مانده کیف پول). تاریخچهٔ صفحه‌بندی‌شدهٔ تمام
/// تراکنش‌های پرداخت‌شده (یا در انتظار/ناموفق) مربوط به شرکت کاربر واردشده، جدیدترین ابتدا.
/// فیلترهای Status/Purpose/FromUtc/ToUtc/Search همگی اختیاری‌اند؛ مقدار نامعتبر Status/Purpose نادیده
/// گرفته می‌شود (نه خطا) چون این فیلترها از Query String می‌آیند و فقط برای نوار فیلتر UI هستند.
/// </summary>
public sealed record GetMyCompanyTransactionsQuery(
    int Page,
    int PageSize,
    string? Status = null,
    string? Purpose = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? Search = null) : IRequest<Result<PagedResult<CompanyTransactionDto>>>;

public sealed class GetMyCompanyTransactionsQueryHandler : IRequestHandler<GetMyCompanyTransactionsQuery, Result<PagedResult<CompanyTransactionDto>>>
{
    private readonly IPaymentTransactionRepository _transactionRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyCompanyTransactionsQueryHandler(
        IPaymentTransactionRepository transactionRepository,
        ICompanyRepository companyRepository,
        IJobAdRepository jobAdRepository,
        ICurrentUserService currentUser)
    {
        _transactionRepository = transactionRepository;
        _companyRepository = companyRepository;
        _jobAdRepository = jobAdRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<CompanyTransactionDto>>> Handle(GetMyCompanyTransactionsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<PagedResult<CompanyTransactionDto>>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var company = await _companyRepository.GetByOwnerUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (company is null)
            return Result.Success(PagedResult<CompanyTransactionDto>.Create(Array.Empty<CompanyTransactionDto>(), 0, request.Page, request.PageSize));

        PaymentTransactionStatus? status = Enum.TryParse<PaymentTransactionStatus>(request.Status, out var parsedStatus) ? parsedStatus : null;
        PaymentPurpose? purpose = Enum.TryParse<PaymentPurpose>(request.Purpose, out var parsedPurpose) ? parsedPurpose : null;

        var paged = await _transactionRepository.GetPagedByCompanyIdAsync(
            company.Id, request.Page, request.PageSize, status, purpose, request.FromUtc, request.ToUtc, request.Search, cancellationToken);

        var items = new List<CompanyTransactionDto>(paged.Items.Count);
        foreach (var transaction in paged.Items)
            items.Add(await ToDtoAsync(transaction, cancellationToken));

        return Result.Success(PagedResult<CompanyTransactionDto>.Create(items, paged.TotalCount, paged.Page, paged.PageSize));
    }

    /// <summary>عنوان مرتبط با هر تراکنش — عنوان واقعی آگهی برای هزینه ثبت آگهی، برچسب ثابت برای بنر (بدون فیلد عنوان در Domain).</summary>
    private async Task<CompanyTransactionDto> ToDtoAsync(PaymentTransaction transaction, CancellationToken cancellationToken)
    {
        var relatedTitle = transaction.Purpose switch
        {
            PaymentPurpose.JobAdListingFee when transaction.JobAdId is not null =>
                (await _jobAdRepository.GetByIdAsync(transaction.JobAdId.Value, cancellationToken))?.Title ?? "آگهی حذف‌شده",
            PaymentPurpose.BannerAdFee => "بنر تبلیغاتی",
            _ => "—"
        };

        return new CompanyTransactionDto(
            transaction.Id,
            transaction.Purpose.ToString(),
            transaction.AmountInRials,
            transaction.Status.ToString(),
            transaction.Authority,
            transaction.RefId,
            relatedTitle,
            transaction.CreatedAtUtc);
    }
}

/// <summary>خلاصهٔ آماری کارت‌های بالای صفحهٔ «امور مالی و تراکنش‌ها» — مانده کیف پول + تجمیع کل تراکنش‌های شرکت.</summary>
public sealed record GetMyCompanyTransactionSummaryQuery : IRequest<Result<CompanyTransactionSummaryDto>>;

public sealed class GetMyCompanyTransactionSummaryQueryHandler : IRequestHandler<GetMyCompanyTransactionSummaryQuery, Result<CompanyTransactionSummaryDto>>
{
    private readonly IPaymentTransactionRepository _transactionRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyCompanyTransactionSummaryQueryHandler(
        IPaymentTransactionRepository transactionRepository, ICompanyRepository companyRepository, ICurrentUserService currentUser)
    {
        _transactionRepository = transactionRepository;
        _companyRepository = companyRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<CompanyTransactionSummaryDto>> Handle(GetMyCompanyTransactionSummaryQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CompanyTransactionSummaryDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var company = await _companyRepository.GetByOwnerUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (company is null)
            return Result.Success(new CompanyTransactionSummaryDto(0, 0, 0, 0, null));

        var aggregate = await _transactionRepository.GetCompanyAggregateAsync(company.Id, cancellationToken);

        return Result.Success(new CompanyTransactionSummaryDto(
            company.WalletBalanceInRials,
            aggregate.TotalPaidAmountInRials,
            aggregate.SuccessfulTransactionsCount,
            aggregate.TotalTransactionsCount,
            aggregate.LastTransactionAtUtc));
    }
}
