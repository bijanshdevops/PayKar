using IndustrialPlatform.Application.Candidates;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Application.Geography;
using IndustrialPlatform.Application.JobAds;
using IndustrialPlatform.Application.Ads;
using IndustrialPlatform.Application.Dashboard;
using IndustrialPlatform.Application.Payments;
using IndustrialPlatform.Application.Support;
using IndustrialPlatform.Persistence.Common;
using IndustrialPlatform.Persistence.Interceptors;
using IndustrialPlatform.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrialPlatform.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' not configured.");

        // یادداشت: Scoped (نه Singleton) چون به ICurrentUserService (Scoped، وابسته به HttpContext)
        // نیاز دارد — تزریق سرویس Scoped داخل Singleton توسط اعتبارسنجی DI رد می‌شود.
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            options.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ICompanyDocumentRepository, CompanyDocumentRepository>();
        services.AddScoped<IGeographyQueryService, GeographyQueryService>();
        services.AddScoped<IJobAdRepository, JobAdRepository>();
        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped<ICandidateEducationRepository, CandidateEducationRepository>();
        services.AddScoped<ICandidateWorkExperienceRepository, CandidateWorkExperienceRepository>();
        services.AddScoped<ICandidateCertificationRepository, CandidateCertificationRepository>();
        services.AddScoped<ICandidateSkillRepository, CandidateSkillRepository>();
        services.AddScoped<ICandidateLanguageRepository, CandidateLanguageRepository>();
        services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
        services.AddScoped<IBookmarkedJobRepository, BookmarkedJobRepository>();
        services.AddScoped<ICompanyDailyViewStatRepository, CompanyDailyViewStatRepository>();
        // یادداشت: IAdBoostPlanRepository/AdBoostPlanRepository به همراه سیستم ارتقاء/Boost بازنشسته شدند.
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
        services.AddScoped<ISupportMessageRepository, SupportMessageRepository>();
        services.AddScoped<IBannerAdRepository, BannerAdRepository>();
        services.AddScoped<IBannerSlotRepository, BannerSlotRepository>();
        services.AddScoped<IBannerDailyStatRepository, BannerDailyStatRepository>();
        services.AddScoped<IOwnerDashboardQueryService, OwnerDashboardQueryService>();
        services.AddScoped<ICompanyDashboardQueryService, CompanyDashboardQueryService>();
        services.AddScoped<IPublicStatsQueryService, PublicStatsQueryService>();

        return services;
    }
}
