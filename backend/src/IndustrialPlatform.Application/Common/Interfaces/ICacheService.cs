namespace IndustrialPlatform.Application.Common.Interfaces;

/// <summary>پورت خروجی کش — پیاده‌سازی واقعی (Redis) در IndustrialPlatform.Infrastructure.</summary>
public interface ICacheService
{
    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);
    Task SetStringAsync(string key, string value, TimeSpan expiry, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<long> IncrementAsync(string key, TimeSpan expiryIfNew, CancellationToken cancellationToken = default);
}
