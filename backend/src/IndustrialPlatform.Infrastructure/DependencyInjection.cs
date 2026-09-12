using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Infrastructure.Caching;
using IndustrialPlatform.Infrastructure.Payments;
using IndustrialPlatform.Infrastructure.Sms;
using IndustrialPlatform.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace IndustrialPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' not configured.");

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddScoped<ICacheService, RedisCacheService>();

        services.Configure<MeliPayamakSettings>(configuration.GetSection(MeliPayamakSettings.SectionName));
        services.AddHttpClient<ISmsService, MeliPayamakSmsService>((sp, client) =>
        {
            var settings = configuration.GetSection(MeliPayamakSettings.SectionName).Get<MeliPayamakSettings>()
                ?? new MeliPayamakSettings();
            client.BaseAddress = new Uri(settings.ApiBaseUrl);
        });

        services.Configure<ZarinPalSettings>(configuration.GetSection(ZarinPalSettings.SectionName));
        services.AddHttpClient<IPaymentGateway, ZarinPalPaymentGateway>();

        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}
