using IndustrialPlatform.Domain.Ads;

namespace IndustrialPlatform.Application.Ads;

internal static class BannerAdMapper
{
    public static BannerAdDto ToDto(BannerAd banner) => new(
        banner.Id,
        banner.CompanyId,
        banner.ImageUrl,
        banner.DestinationUrl,
        banner.Placement.ToString(),
        banner.Status.ToString(),
        banner.IsFeePaid,
        banner.RejectionReason,
        banner.ActivatedAtUtc,
        banner.ExpiresAtUtc,
        banner.CreatedAtUtc,
        banner.BannerSlotId,
        banner.BannerSlot?.Title,
        banner.BannerSlot?.Dimensions,
        banner.DurationDays,
        banner.StartDate,
        banner.EndDate,
        banner.TotalAmount,
        banner.ImpressionsCount,
        banner.ClicksCount);
}
