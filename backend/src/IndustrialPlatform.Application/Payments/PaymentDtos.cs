namespace IndustrialPlatform.Application.Payments;

public sealed record SubmitJobAdForReviewResponse(string PaymentRedirectUrl, long AmountInRials);

/// <summary>طبق ADR-013 — پاسخ شروع پرداخت هزینه رزومه‌ساز (مسیر مشابه SubmitJobAdForReviewResponse).</summary>
public sealed record SubmitResumeFeePaymentResponse(string PaymentRedirectUrl, long AmountInRials);

public sealed record PaymentCallbackResult(bool Success, string? RefId, string? Message, Guid? JobAdId, Guid? BannerAdId = null, Guid? CandidateId = null);

public sealed record JobAdListingFeeDto(long AmountInRials);

/// <summary>
/// یک ردیف در «امور مالی و تراکنش‌ها» شرکت — طبق تصمیم صریح محصولی این فاز، شامل فقط تراکنش‌هایی
/// که CompanyId دارند (هزینه ثبت آگهی / هزینه بنر تبلیغاتی)؛ هزینه رزومه‌ساز کارجو (CandidateId) از
/// این لیست مستثنی است چون به شرکت مربوط نیست. RelatedTitle عنوان آگهی یا برچسب ثابت بنر تبلیغاتی است.
/// </summary>
public sealed record CompanyTransactionDto(
    Guid Id,
    string Purpose,
    long AmountInRials,
    string Status,
    string Authority,
    string? RefId,
    string RelatedTitle,
    DateTime CreatedAtUtc);

/// <summary>
/// خلاصهٔ آماری صفحهٔ «امور مالی و تراکنش‌ها» (کارت‌های بالای صفحه) — طبق موکاپ جدید این صفحه.
/// TotalPaidAmountInRials/SuccessfulTransactionsCount/LastTransactionAtUtc از کل تراکنش‌های شرکت
/// محاسبه می‌شوند (نه فقط صفحهٔ جاری جدول)، تا با فیلتر/صفحه‌بندی جدول ناسازگار نشوند.
/// </summary>
public sealed record CompanyTransactionSummaryDto(
    long WalletBalanceInRials,
    long TotalPaidAmountInRials,
    int SuccessfulTransactionsCount,
    int TotalTransactionsCount,
    DateTime? LastTransactionAtUtc);
