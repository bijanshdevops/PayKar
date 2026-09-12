using IndustrialPlatform.Api.Common;
using IndustrialPlatform.Application.Candidates.Commands;
using IndustrialPlatform.Application.JobAds.Commands;
using IndustrialPlatform.Application.JobAds.Queries;
using MediatR;

namespace IndustrialPlatform.Api.Endpoints;

/// <summary>اندپوینت‌های آگهی‌های شغلی — طبق سند 04-Api-Contract.md بخش ۵.۴ (بازنگری‌شده برای جریان تایید ادمین).</summary>
public static class JobAdEndpoints
{
    public static IEndpointRouteBuilder MapJobAdEndpoints(this IEndpointRouteBuilder app)
    {
        var publicGroup = app.MapGroup("/api/v1/job-ads").WithTags("JobAds");

        publicGroup.MapGet("/", async (
            Guid? industrialZoneId, Guid? cityId, string? workShift, bool? hasCommuteService,
            string? contractType, decimal? minSalaryAmount, string? keyword,
            int? page, int? pageSize, string? sortBy, string? sortDir,
            ISender sender, CancellationToken ct) =>
        {
            var query = new SearchJobAdsQuery(
                industrialZoneId, cityId, workShift, hasCommuteService, contractType, minSalaryAmount, keyword,
                page is > 0 ? page.Value : 1,
                pageSize is > 0 ? pageSize.Value : 20,
                string.IsNullOrWhiteSpace(sortBy) ? "createdAtUtc" : sortBy,
                string.IsNullOrWhiteSpace(sortDir) ? "desc" : sortDir);

            var result = await sender.Send(query, ct);
            return result.ToApiResult();
        });

        publicGroup.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetJobAdByIdQuery(id), ct);
            return result.ToApiResult();
        });

        // طبق تصمیم صریح محصولی این فاز — ثبت بازدید واقعی. عمداً یک Command مجزا (نه side-effect
        // در Query بالا) تا Query خالص/بدون تغییر وضعیت بماند. بدون نیاز به ورود؛ هر بازدیدکننده مجاز است.
        publicGroup.MapPost("/{id:guid}/view", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RecordJobAdViewCommand(id), ct);
            return result.ToApiResult();
        });

        // طبق 02_home_page_spec.md / 03_jobseeker_dashboard_spec.md — نشان‌کردن/لغو نشان آگهی توسط کارجو.
        // Toggle: هر بار فراخوانی وضعیت را برعکس می‌کند؛ فقط کارجوی واردشده مجاز است.
        publicGroup.MapPost("/{id:guid}/bookmark/toggle", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ToggleBookmarkCommand(id), ct);
            return result.ToApiResult();
        }).RequireAuthorization("CandidateOnly");

        var managedGroup = app.MapGroup("/api/v1/job-ads").WithTags("JobAds").RequireAuthorization("CompanyManagerOnly");

        managedGroup.MapPost("/", async (CreateJobAdRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var command = new CreateJobAdCommand(
                dto.Title, dto.Description, dto.WorkShift, dto.HasCommuteService, dto.CommuteServiceRoutes,
                dto.MealPlan, dto.InsuranceTypes, dto.SalaryRangeType, dto.FixedAmount, dto.MinAmount, dto.MaxAmount,
                dto.ContractType, dto.GenderPreference, dto.MinAge, dto.MaxAge, dto.MinEducationLevel,
                dto.MinExperienceYears, dto.MilitaryServiceStatus, dto.HeadcountNeeded, dto.ApplicationDeadlineUtc,
                dto.RequiredSkills, dto.AdditionalBenefits);
            var result = await sender.Send(command, ct);
            return result.ToApiResult("آگهی به‌صورت پیش‌نویس ایجاد شد. برای انتشار، هزینه ثبت آگهی را پرداخت و برای بررسی ارسال کنید.", 201);
        });

        managedGroup.MapPut("/{id:guid}", async (Guid id, CreateJobAdRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateJobAdCommand(
                id, dto.Title, dto.Description, dto.WorkShift, dto.HasCommuteService, dto.CommuteServiceRoutes,
                dto.MealPlan, dto.InsuranceTypes, dto.SalaryRangeType, dto.FixedAmount, dto.MinAmount, dto.MaxAmount,
                dto.ContractType, dto.GenderPreference, dto.MinAge, dto.MaxAge, dto.MinEducationLevel,
                dto.MinExperienceYears, dto.MilitaryServiceStatus, dto.HeadcountNeeded, dto.ApplicationDeadlineUtc,
                dto.RequiredSkills, dto.AdditionalBenefits);
            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });

        managedGroup.MapPost("/{id:guid}/close", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CloseJobAdCommand(id), ct);
            return result.ToApiResult("آگهی بسته شد.");
        });

        managedGroup.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteJobAdCommand(id), ct);
            return result.ToApiResult("آگهی با موفقیت حذف شد.");
        });

        managedGroup.MapGet("/my-company", async (int? page, int? pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyCompanyJobAdsQuery(page is > 0 ? page.Value : 1, pageSize is > 0 ? pageSize.Value : 20), ct);
            return result.ToApiResult();
        });

        // ---------- پنل Owner: استعلام/تایید/رد آگهی ----------
        var adminGroup = app.MapGroup("/api/v1/admin/job-ads").WithTags("Admin-JobAds").RequireAuthorization("AdminOnly");

        adminGroup.MapGet("/pending", async (int? page, int? pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPendingReviewJobAdsQuery(page is > 0 ? page.Value : 1, pageSize is > 0 ? pageSize.Value : 20), ct);
            return result.ToApiResult();
        });

        adminGroup.MapPost("/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ApproveJobAdCommand(id), ct);
            return result.ToApiResult("آگهی تایید و منتشر شد.");
        });

        adminGroup.MapPost("/{id:guid}/reject", async (Guid id, RejectJobAdRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RejectJobAdCommand(id, dto.Reason), ct);
            return result.ToApiResult("آگهی رد شد.");
        });

        // طبق تصمیم صریح محصولی این فاز — فلگ ساده «ویژه» فقط توسط ادمین، بدون هیچ پلن پرداختی/Boost.
        adminGroup.MapPatch("/{id:guid}/featured", async (Guid id, SetFeaturedRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetJobAdFeaturedCommand(id, dto.IsFeatured), ct);
            return result.ToApiResult(dto.IsFeatured ? "آگهی به‌عنوان ویژه علامت‌گذاری شد." : "آگهی از حالت ویژه خارج شد.");
        });

        return app;
    }

    private sealed record CreateJobAdRequestDto(
        string Title, string Description, string WorkShift, bool HasCommuteService, string? CommuteServiceRoutes,
        string MealPlan, IReadOnlyList<string> InsuranceTypes, string SalaryRangeType,
        decimal? FixedAmount, decimal? MinAmount, decimal? MaxAmount,
        string ContractType, string? GenderPreference, int? MinAge, int? MaxAge, string? MinEducationLevel,
        int? MinExperienceYears, string? MilitaryServiceStatus, int? HeadcountNeeded,
        DateTime? ApplicationDeadlineUtc, string? RequiredSkills, string? AdditionalBenefits);

    private sealed record RejectJobAdRequestDto(string Reason);
    private sealed record SetFeaturedRequestDto(bool IsFeatured);
}
