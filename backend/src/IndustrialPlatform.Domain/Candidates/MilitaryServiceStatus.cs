namespace IndustrialPlatform.Domain.Candidates;

/// <summary>وضعیت نظام وظیفه — طبق سند 02-Domain-Glossary.md بخش ۴.۱.</summary>
public enum MilitaryServiceStatus
{
    NotApplicable = 1,
    Completed = 2,
    Exempted = 3,
    InProgress = 4
}
