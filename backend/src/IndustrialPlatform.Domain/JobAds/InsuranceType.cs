namespace IndustrialPlatform.Domain.JobAds;

/// <summary>
/// طبق سند 02-Domain-Glossary.md بخش ۳.۲ — یک آگهی می‌تواند هم‌زمان چند نوع بیمه داشته باشد.
/// </summary>
[Flags]
public enum InsuranceType
{
    None = 0,
    SocialSecurity = 1,
    Supplementary = 2,
    AccidentInsurance = 4
}
