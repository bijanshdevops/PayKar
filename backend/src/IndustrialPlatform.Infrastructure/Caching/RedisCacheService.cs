using IndustrialPlatform.Application.Common.Interfaces;
using StackExchange.Redis;

namespace IndustrialPlatform.Infrastructure.Caching;

/// <summary>آداپتور کش ردیس — پیاده‌سازی پورت ICacheService (سند 01-Architecture، لایه Infrastructure).</summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    private IDatabase Database => _connectionMultiplexer.GetDatabase();

    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await Database.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public Task SetStringAsync(string key, string value, TimeSpan expiry, CancellationToken cancellationToken = default) =>
        Database.StringSetAsync(key, value, expiry);

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        Database.KeyDeleteAsync(key);

    public async Task<long> IncrementAsync(string key, TimeSpan expiryIfNew, CancellationToken cancellationToken = default)
    {
        var newValue = await Database.StringIncrementAsync(key);
        if (newValue == 1)
        {
            await Database.KeyExpireAsync(key, expiryIfNew);
        }

        return newValue;
    }
}
