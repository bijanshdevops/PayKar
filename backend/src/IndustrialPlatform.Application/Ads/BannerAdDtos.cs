namespace IndustrialPlatform.Application.Ads;

public sealed record BannerAdDto(
    Guid Id,
    Guid CompanyId,
    string ImageUrl,
    string DestinationUrl,
    string Placement,
    string Status,
    bool IsFeePaid,
    string? RejectionReason,
    DateTime? ActivatedAtUtc,
    DateTime? ExpiresAtUtc,
    DateTime CreatedAtUtc,
    int? BannerSlotId,
    string? SlotTitle,
    string? SlotDimensions,
    int DurationDays,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal TotalAmount,
    long ImpressionsCount,
    long ClicksCount);
