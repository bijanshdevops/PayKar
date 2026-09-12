using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IndustrialPlatform.Api.Endpoints;

/// <summary>
/// اندپوینت‌های سلامت طبق سند 01-Architecture بخش ۵: مصرف‌کننده Kubernetes Liveness/Readiness Probes.
/// این اندپوینت‌ها عمداً خارج از پاکت استاندارد ApiResponse قرار دارند (سند 04، بخش ۷).
/// </summary>
public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // Liveness: فقط بررسی زنده بودن پروسه، بدون چک وابستگی خارجی
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });

        // Readiness: بررسی اتصال واقعی به PostgreSQL و Redis
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        return app;
    }
}
