namespace IndustrialPlatform.Domain.JobAds;

/// <summary>نوع قرارداد استخدامی — فیلد الزامی فرم ثبت آگهی.</summary>
public enum ContractType
{
    Permanent = 1,
    FixedTerm = 2,
    PartTime = 3,
    ProjectBased = 4,
    Internship = 5
}
