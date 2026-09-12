using IndustrialPlatform.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;

namespace IndustrialPlatform.Infrastructure.Storage;

/// <summary>
/// پیاده‌سازی ساده دیسک محلی برای ذخیره فایل‌های آپلودی (مدارک شرکت، رزومه و...).
/// فایل‌ها در {ContentRoot}/uploads/{containerName}/ ذخیره شده و از طریق Static Files
/// روی مسیر عمومی /uploads/... در Program.cs قابل‌دسترسی هستند.
/// یادداشت: برای استقرار در مقیاس بزرگ‌تر باید با یک پیاده‌سازی Object Storage
/// (مثل S3/Arvan Object Storage) جایگزین شود؛ چون این پورت (IFileStorageService)
/// در لایه Application تعریف شده، جایگزینی بدون تغییر کد بیزینسی ممکن است.
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _contentRootPath;
    private readonly string _uploadsRootPath;

    public LocalFileStorageService(IHostEnvironment hostEnvironment)
    {
        _contentRootPath = hostEnvironment.ContentRootPath;
        _uploadsRootPath = Path.Combine(hostEnvironment.ContentRootPath, "uploads");
    }

    public async Task<string> SaveAsync(Stream content, string originalFileName, string containerName, CancellationToken cancellationToken = default)
    {
        var containerPath = Path.Combine(_uploadsRootPath, containerName);
        Directory.CreateDirectory(containerPath);

        var safeExtension = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{safeExtension}";
        var fullPath = Path.Combine(containerPath, storedFileName);

        await using (var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        return $"/uploads/{containerName}/{storedFileName}";
    }

    public string ResolvePhysicalPath(string relativeUrl)
    {
        var normalized = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_contentRootPath, normalized);
    }
}
