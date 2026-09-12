namespace IndustrialPlatform.Application.Common.Interfaces;

/// <summary>پورت خروجی ذخیره‌سازی فایل — پیاده‌سازی واقعی (دیسک محلی) در IndustrialPlatform.Infrastructure.</summary>
public interface IFileStorageService
{
    /// <summary>محتوای فایل را ذخیره کرده و مسیر URL نسبی قابل‌دسترسی برای دانلود را برمی‌گرداند.</summary>
    Task<string> SaveAsync(Stream content, string originalFileName, string containerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// مسیر فیزیکی روی دیسک را از روی URL نسبی برگردانده‌شده توسط <see cref="SaveAsync"/> بازمی‌گرداند —
    /// برای اندپوینت‌های دانلود امن (احرازهویت‌شده) که نمی‌توانند از مسیر عمومی و بدون احراز /uploads/... استفاده کنند.
    /// </summary>
    string ResolvePhysicalPath(string relativeUrl);
}
