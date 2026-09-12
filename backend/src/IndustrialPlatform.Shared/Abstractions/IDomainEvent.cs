namespace IndustrialPlatform.Shared.Abstractions;

/// <summary>مارکر رویدادهای دامنه طبق سند 01-Architecture.md.</summary>
public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
