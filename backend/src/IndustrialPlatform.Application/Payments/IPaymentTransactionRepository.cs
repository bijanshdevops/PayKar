using IndustrialPlatform.Domain.Payments;
using IndustrialPlatform.Shared.Api;

namespace IndustrialPlatform.Application.Payments;

/// <summary>خلاصهٔ آماری تراکنش‌های یک شرکت — برای کارت‌های صفحهٔ «امور مالی و تراکنش‌ها».</summary>
public sealed record CompanyTransactionAggregate(
    long TotalPaidAmountInRials,
    int SuccessfulTransactionsCount,
    int TotalTransactionsCount,
    DateTime? LastTransactionAtUtc);

public interface IPaymentTransactionRepository
{
    Task<PaymentTransaction?> GetByAuthorityAsync(string authority, CancellationToken cancellationToken = default);

    /// <summary>
    /// طبق «امور مالی و تراکنش‌ها» — تاریخچه صفحه‌بندی‌شده تراکنش‌های یک شرکت، جدیدترین ابتدا،
    /// با فیلترهای اختیاری وضعیت/هدف/بازه تاریخ/جستجو (شماره پیگیری، کد رهگیری یا عنوان آگهی مرتبط).
    /// </summary>
    Task<PagedResult<PaymentTransaction>> GetPagedByCompanyIdAsync(
        Guid companyId,
        int page,
        int pageSize,
        PaymentTransactionStatus? status = null,
        PaymentPurpose? purpose = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    /// <summary>خلاصهٔ آماری تمام تراکنش‌های شرکت (بدون فیلتر/صفحه‌بندی) — برای کارت‌های بالای صفحه.</summary>
    Task<CompanyTransactionAggregate> GetCompanyAggregateAsync(Guid companyId, CancellationToken cancellationToken = default);

    void Add(PaymentTransaction transaction);
}
