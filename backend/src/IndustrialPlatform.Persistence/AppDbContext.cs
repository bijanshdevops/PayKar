using System.Reflection;
using IndustrialPlatform.Domain.Candidates;
using IndustrialPlatform.Domain.Companies;
using IndustrialPlatform.Domain.Geography;
using IndustrialPlatform.Domain.JobAds;
using IndustrialPlatform.Domain.Ads;
using IndustrialPlatform.Domain.Payments;
using IndustrialPlatform.Domain.Support;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence;

/// <summary>
/// DbContext اصلی سیستم — پوشش‌دهنده Domain (Company, JobAd, Candidate, ...).
/// طبق ماتریس وابستگی سند 01-Architecture، این DbContext هرگز موجودیت‌های Identity
/// (User, Role, RefreshToken) را در بر نمی‌گیرد؛ آن‌ها در IdentityDbContext مجزا هستند.
/// یادداشت: DbSet مربوط به AdBoostPlan طبق تصمیم محصولی «جایگزینی کامل با هزینه ثابت» حذف شد.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<IndustrialZone> IndustrialZones => Set<IndustrialZone>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyDocument> CompanyDocuments => Set<CompanyDocument>();
    public DbSet<JobAd> JobAds => Set<JobAd>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<CandidateEducation> CandidateEducations => Set<CandidateEducation>();
    public DbSet<CandidateWorkExperience> CandidateWorkExperiences => Set<CandidateWorkExperience>();
    public DbSet<CandidateCertification> CandidateCertifications => Set<CandidateCertification>();
    public DbSet<CandidateSkill> CandidateSkills => Set<CandidateSkill>();
    public DbSet<CandidateLanguage> CandidateLanguages => Set<CandidateLanguage>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<BookmarkedJob> BookmarkedJobs => Set<BookmarkedJob>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();
    public DbSet<BannerAd> BannerAds => Set<BannerAd>();
    public DbSet<BannerSlot> BannerSlots => Set<BannerSlot>();
    public DbSet<BannerDailyStat> BannerDailyStats => Set<BannerDailyStat>();
    public DbSet<CompanyDailyViewStat> CompanyDailyViewStats => Set<CompanyDailyViewStat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
