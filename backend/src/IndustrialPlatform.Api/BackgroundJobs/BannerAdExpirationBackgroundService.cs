using IndustrialPlatform.Application.Ads.Commands;
using MediatR;

namespace IndustrialPlatform.Api.BackgroundJobs;

/// <summary>
/// اجرای دوره‌ای ExpireBannerAdsCommand — طبق ADR-008 (عیناً الگوی JobAdExpirationBackgroundService).
/// Stateless و Idempotent، برای اجرا در محیط K8s با چندین Replica بدون تداخل مناسب است.
/// </summary>
public sealed class BannerAdExpirationBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BannerAdExpirationBackgroundService> _logger;

    public BannerAdExpirationBackgroundService(IServiceScopeFactory scopeFactory, ILogger<BannerAdExpirationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var result = await sender.Send(new ExpireBannerAdsCommand(), stoppingToken);

                if (result.IsSuccess && result.Value > 0)
                {
                    _logger.LogInformation("{Count} بنر تبلیغاتی به وضعیت منقضی تغییر یافت.", result.Value);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "اجرای Job انقضای بنرهای تبلیغاتی با خطا مواجه شد.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
