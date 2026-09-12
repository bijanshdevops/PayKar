namespace IndustrialPlatform.Application.Common.Interfaces;

/// <summary>پورت خروجی دسترسی به کاربر جاری درخواست — پیاده‌سازی در Api (از JWT Claims).</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    /// <summary>شماره موبایل کاربر جاری — از Claim توکن JWT («mobile_number») خوانده می‌شود.</summary>
    string? MobileNumber { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsInRole(string role);
}
