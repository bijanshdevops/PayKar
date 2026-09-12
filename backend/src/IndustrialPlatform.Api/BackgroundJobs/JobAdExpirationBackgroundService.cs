using IndustrialPlatform.Application.JobAds.Commands;
using MediatR;

namespace IndustrialPlatform.Api.BackgroundJobs;

/// <summary>
/// اجرای دوره‌ای ExpireJobAdsCommand — طبق docs/backend/Tasks.md فاز ۳
/// («Job زمان‌بندی‌شده برای انتقال خودکار آگهی‌های منقضی به وضعیت Expired»).
/// Stateless است و در محیط K8s با چندین Replica بدون تداخل قابل اجراست (عملیات Idempotent).
/// </summary>
public sealed class JobAdExpirationBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobAdExpirationBackgroundService> _logger;

    public JobAdExpirationBackgroundService(IServiceScopeFactory scopeFactory, ILogger<JobAdExpirationBackgroundService> logger)
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
                var result = await sender.Send(new ExpireJobAdsCommand(), stoppingToken);

                if (result.IsSuccess && result.Value > 0)
                {
                    _logger.LogInformation("{Count} آگهی به وضعیت منقضی تغییر یافت.", result.Value);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "اجرای Job انقضای آگهی‌ها با خطا مواجه شد.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
