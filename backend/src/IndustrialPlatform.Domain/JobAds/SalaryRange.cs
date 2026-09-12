using IndustrialPlatform.Shared.Entities;
using IndustrialPlatform.Shared.Results;

namespace IndustrialPlatform.Domain.JobAds;

public enum SalaryRangeType
{
    MinistryOfLabor = 1,
    FixedAmount = 2,
    Range = 3,
    Agreement = 4
}

/// <summary>
/// شیء مقداری بازه حقوق — طبق سند 02-Domain-Glossary.md بخش ۳.۲.
/// به‌صورت Owned Entity توسط EF Core در همان جدول job_ads نگهداری می‌شود.
/// </summary>
public sealed class SalaryRange : ValueObject
{
    public SalaryRangeType Type { get; private set; }
    public decimal? FixedAmount { get; private set; }
    public decimal? MinAmount { get; private set; }
    public decimal? MaxAmount { get; private set; }

    private SalaryRange() { }

    private SalaryRange(SalaryRangeType type, decimal? fixedAmount, decimal? minAmount, decimal? maxAmount)
    {
        Type = type;
        FixedAmount = fixedAmount;
        MinAmount = minAmount;
        MaxAmount = maxAmount;
    }

    public static Result<SalaryRange> MinistryOfLabor() => Result.Success(new SalaryRange(SalaryRangeType.MinistryOfLabor, null, null, null));

    public static Result<SalaryRange> Agreement() => Result.Success(new SalaryRange(SalaryRangeType.Agreement, null, null, null));

    public static Result<SalaryRange> Fixed(decimal amount)
    {
        if (amount <= 0)
            return Result.Failure<SalaryRange>(Error.Validation("INVALID_SALARY", "مبلغ حقوق باید بزرگ‌تر از صفر باشد."));

        return Result.Success(new SalaryRange(SalaryRangeType.FixedAmount, amount, null, null));
    }

    public static Result<SalaryRange> Range(decimal min, decimal max)
    {
        if (min <= 0 || max <= 0 || min > max)
            return Result.Failure<SalaryRange>(Error.Validation("INVALID_SALARY_RANGE", "بازه حقوق نامعتبر است."));

        return Result.Success(new SalaryRange(SalaryRangeType.Range, null, min, max));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return FixedAmount;
        yield return MinAmount;
        yield return MaxAmount;
    }
}
