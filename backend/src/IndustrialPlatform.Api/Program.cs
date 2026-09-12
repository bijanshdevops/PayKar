using System.Text;
using System.Threading.RateLimiting;
using Microsoft.Extensions.FileProviders;
using IndustrialPlatform.Api.BackgroundJobs;
using IndustrialPlatform.Api.Common;
using IndustrialPlatform.Api.Endpoints;
using IndustrialPlatform.Api.Middleware;
using IndustrialPlatform.Api.Services;
using IndustrialPlatform.Application;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Identity;
using IndustrialPlatform.Identity.Settings;
using IndustrialPlatform.Infrastructure;
using IndustrialPlatform.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuration-bound settings ----------
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

// ---------- لایه‌ها طبق سند 01-Architecture.md ----------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPaymentCallbackUrlProvider, PaymentCallbackUrlProvider>();

builder.Services.AddApplication(typeof(IndustrialPlatform.Identity.DependencyInjection).Assembly);
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// ---------- احراز هویت JWT (سند 05-Security-Rules.md) ----------
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CompanyManagerOnly", policy => policy.RequireRole("CompanyManager"));
    options.AddPolicy("CandidateOnly", policy => policy.RequireRole("Candidate"));
});

// ---------- CORS (سند 05، بخش ۴) ----------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ---------- Health Checks (سند 01، بخش ۵) ----------
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!, tags: new[] { "ready" })
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, tags: new[] { "ready" });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<JobAdExpirationBackgroundService>();
builder.Services.AddHostedService<BannerAdExpirationBackgroundService>();

// ---------- Rate Limiting سراسری (سند 05-Security-Rules.md بخش ۴: «سراسری: سقف درخواست
// به‌ازای هر IP روی Gateway/Middleware») — محدودیت‌های نقطه‌ای OTP در OtpService/Redis جدا
// و علاوه بر این محدودیت عمومی باقی می‌مانند.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

var app = builder.Build();

// ---------- Middleware Pipeline ----------
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

// ---------- فایل‌های آپلودی (مدارک شرکت و...) — سرو شده از {ContentRoot}/uploads روی مسیر /uploads ----------
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseCors("Default");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// یادداشت معماری آگاهانه — عدم ثبت AddAntiforgery()/UseAntiforgery() در این‌جا (تصمیم عمدی):
// ASP.NET Core 8+ به‌صورت خودکار به هر اندپوینت Minimal API دارای پارامتر IFormFile/[FromForm]
// متادیتای «نیازمند Antiforgery» می‌افزاید. اگر میدل‌ور Antiforgery در پایپ‌لاین ثبت نشده باشد،
// همان اندپوینت‌ها در لحظه دریافت درخواست با InvalidOperationException («...anti-forgery metadata,
// but a middleware was not found...») متوقف می‌شوند و ExceptionHandlingMiddleware آن را به یک 500
// عمومی تبدیل می‌کند — این دقیقاً علت ریشه‌ای خطاهای 500 گزارش‌شده روی آپلود لوگو/بنر/مدارک شرکت،
// آواتار کارجو و پیوست تیکت پشتیبانی بود.
// راه‌حل انتخاب‌شده: به‌جای ثبت کامل زیرسیستم AddAntiforgery()/UseAntiforgery() (که برای سناریوی
// CSRF مبتنی بر Cookie/Session طراحی شده و نیازمند دریافت/ارسال توکن Antiforgery از فرانت‌اند است)،
// روی تک‌تک اندپوینت‌های multipart/form-data این پروژه متد .DisableAntiforgery() فراخوانی شده است
// (نگاه کنید به: CandidateEndpoints, CompanyEndpoints, SupportEndpoints, BannerAdEndpoints).
// این API کاملاً Stateless و مبتنی بر JWT Bearer Token است، نه Cookie/Session — بنابراین محافظت
// Antiforgery اساساً برای مدل احراز هویت این پلتفرم مصداق ندارد و افزودن آن هیچ سطح امنیتی
// اضافه‌ای فراهم نمی‌کند، در حالی که ثبت‌نکردن آن از تغییرات غیرضروری در فرانت‌اند (دریافت و ارسال
// هدر توکن Antiforgery در هر درخواست) جلوگیری می‌کند.

// ---------- Endpoints ----------
app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapGeographyEndpoints();
app.MapPublicStatsEndpoints();
app.MapCompanyEndpoints();
app.MapJobAdEndpoints();
app.MapCandidateEndpoints();
app.MapPaymentEndpoints();
app.MapSupportEndpoints();
app.MapBannerAdEndpoints();
app.MapOwnerDashboardEndpoints();

app.Run();

// امکان استفاده در پروژه‌های تست یکپارچگی (WebApplicationFactory)
public partial class Program { }
