namespace IndustrialPlatform.Domain.Candidates;

/// <summary>طبق سند 02-Domain-Glossary.md بخش ۴.۲.</summary>
public enum JobApplicationStatus
{
    Submitted = 1,
    Reviewed = 2,
    InterviewScheduled = 3,
    Accepted = 4,
    Rejected = 5
}
