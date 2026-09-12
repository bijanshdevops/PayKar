using IndustrialPlatform.Api.Common;
using IndustrialPlatform.Application.Candidates.Commands;
using IndustrialPlatform.Application.Candidates.Queries;
using IndustrialPlatform.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IndustrialPlatform.Api.Endpoints;

/// <summary>اندپوینت‌های کارجو و درخواست‌های همکاری — طبق سند 04-Api-Contract.md بخش ۵.۵.</summary>
public static class CandidateEndpoints
{
    public static IEndpointRouteBuilder MapCandidateEndpoints(this IEndpointRouteBuilder app)
    {
        var candidateGroup = app.MapGroup("/api/v1/candidates").WithTags("Candidates").RequireAuthorization();

        candidateGroup.MapPost("/resumes", async (SaveCandidateProfileCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToApiResult("رزومه شما ذخیره شد.");
        });

        // مسیر جایگزین با معنای REST-ایِ «به‌روزرسانی» — همان SaveCandidateProfileCommand بالا را
        // صدا می‌زند (رفتار Upsert یکسان)؛ POST /resumes به‌خاطر سازگاری با فرانت‌اند فعلی حذف نشد.
        candidateGroup.MapPut("/me/profile", async (SaveCandidateProfileCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToApiResult("پروفایل شما ذخیره شد.");
        });

        candidateGroup.MapGet("/resumes/me", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyCandidateProfileQuery(), ct);
            return result.ToApiResult();
        });

        // طبق فاز «پروفایل و رزومه‌ساز کارجو» — ترجیحات شغلی مستقل از فرم اصلی پروفایل.
        candidateGroup.MapPut("/me/preferences", async (UpdateCandidateJobPreferencesCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToApiResult("ترجیحات شغلی ذخیره شد.");
        });

        // طبق تصمیم صریح محصولی این فاز — آواتار کارجو هنگام Apply روی یک آگهی، به همراه سایر مشخصات
        // هویتی، در اختیار همان کارفرما قرار می‌گیرد (نمای «آخرین رزومه‌های دریافتی» داشبورد شرکت).
        // .DisableAntiforgery(): طبق تغییر رفتار ASP.NET Core ۸+ — هر Minimal API Endpoint که پارامتر
        // IFormFile/[FromForm] داشته باشد، به‌صورت خودکار متادیتای Antiforgery می‌گیرد و اگر میان‌افزار
        // UseAntiforgery() در Pipeline نباشد، با InvalidOperationException («contains anti-forgery
        // metadata, but a middleware was not found») کل درخواست را با خطای 500 متوقف می‌کند. چون این
        // API کاملاً Stateless و مبتنی بر JWT Bearer Token است (نه Cookie/Session)، حمله CSRF که
        // Antiforgery برایش طراحی شده اساساً اینجا مصداق ندارد؛ راه‌حل رسمی مایکروسافت برای چنین
        // اندپوینت‌هایی دقیقاً همین DisableAntiforgery() است — نه اضافه‌کردن کل زیرسیستم Antiforgery
        // (که علاوه‌بر پیچیدگی غیرضروری، فرانت‌اند را هم مجبور به دریافت/ارسال یک هدر توکن جدید می‌کرد).
        candidateGroup.MapPost("/me/avatar", async (IFormFile file, ISender sender, CancellationToken ct) =>
        {
            await using var stream = file.OpenReadStream();
            var command = new UploadCandidateAvatarCommand(stream, file.FileName, file.Length);
            var result = await sender.Send(command, ct);
            return result.ToApiResult("تصویر پروفایل به‌روزرسانی شد.");
        }).DisableAntiforgery();

        // طبق فاز «مدیریت رزومه‌ها و متقاضیان» — ردیف‌های ساختاریافته تحصیلات/سوابق شغلی رزومه‌ساز.
        candidateGroup.MapPost("/me/educations", async (AddCandidateEducationCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToApiResult("سابقه تحصیلی افزوده شد.", 201);
        });

        candidateGroup.MapDelete("/me/educations/{educationId:guid}", async (Guid educationId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteCandidateEducationCommand(educationId), ct);
            return result.ToApiResult("سابقه تحصیلی حذف شد.");
        });

        candidateGroup.MapPost("/me/work-experiences", async (AddCandidateWorkExperienceCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToApiResult("سابقه شغلی افزوده شد.", 201);
        });

        candidateGroup.MapDelete("/me/work-experiences/{workExperienceId:guid}", async (Guid workExperienceId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteCandidateWorkExperienceCommand(workExperienceId), ct);
            return result.ToApiResult("سابقه شغلی حذف شد.");
        });

        // طبق فاز «پروفایل و رزومه‌ساز کارجو» — UpsertWorkExperienceCommand، مکمل AddCandidateWorkExperienceCommand
        // بالا: آن دستور فقط افزودن را پوشش می‌دهد، این دو اندپوینت افزودن+ویرایش اتمیک را با هم پوشش می‌دهند.
        candidateGroup.MapPost("/me/experiences", async (UpsertExperienceRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var command = new UpsertWorkExperienceCommand(null, dto.JobTitle, dto.CompanyName, dto.StartYear, dto.EndYear, dto.Description);
            var result = await sender.Send(command, ct);
            return result.ToApiResult("سابقه شغلی افزوده شد.", 201);
        });

        candidateGroup.MapPut("/me/experiences/{id:guid}", async (Guid id, UpsertExperienceRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var command = new UpsertWorkExperienceCommand(id, dto.JobTitle, dto.CompanyName, dto.StartYear, dto.EndYear, dto.Description);
            var result = await sender.Send(command, ct);
            return result.ToApiResult("سابقه شغلی ویرایش شد.");
        });

        // طبق فاز «پروفایل و رزومه‌ساز کارجو» (بخش تکمیلی) — همگام‌سازی/جایگزینی کامل مهارت‌ها و زبان‌ها،
        // و افزودن/حذف مدارک از فرم ویرایش رزومه.
        candidateGroup.MapPut("/resumes/me/skills", async (UpdateCandidateSkillsCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToApiResult("مهارت‌های شما ذخیره شد.");
        });

        candidateGroup.MapPut("/resumes/me/languages", async (UpdateCandidateLanguagesCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToApiResult("زبان‌های شما ذخیره شد.");
        });

        candidateGroup.MapPost("/resumes/me/certifications", async (AddCandidateCertificationCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToApiResult("مدرک/گواهینامه افزوده شد.", 201);
        });

        candidateGroup.MapDelete("/resumes/me/certifications/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteCandidateCertificationCommand(id), ct);
            return result.ToApiResult("مدرک/گواهینامه حذف شد.");
        });

        // طبق تسک #75 — پیگیری تمام درخواست‌های ارسالی کارجوی واردشده از پروفایل خودش.
        candidateGroup.MapGet("/me/applications", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyApplicationsQuery(), ct);
            return result.ToApiResult();
        });

        // آیکون بوک‌مارک روی کارت‌ها — فقط شناسهٔ آگهی‌های نشان‌شده، سبک و سریع.
        candidateGroup.MapGet("/me/bookmarks/ids", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyBookmarkedJobIdsQuery(), ct);
            return result.ToApiResult();
        });

        // صفحهٔ «آگهی‌های نشان‌شده» در داشبورد کارجو — جزئیات کامل هر آگهی.
        candidateGroup.MapGet("/me/bookmarks", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyBookmarkedJobsQuery(), ct);
            return result.ToApiResult();
        }).RequireAuthorization("CandidateOnly");

        // طبق ADR-013 — شروع پرداخت هزینه ثابت رزومه‌ساز و هدایت به درگاه زرین‌پال.
        candidateGroup.MapPost("/resumes/submit-payment", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SubmitResumeFeePaymentCommand(), ct);
            return result.ToApiResult("به درگاه پرداخت هدایت می‌شوید.", 201);
        }).RequireAuthorization("CandidateOnly");

        var applicationGroup = app.MapGroup("/api/v1").WithTags("Applications");

        applicationGroup.MapPost("/job-ads/{jobAdId:guid}/applications", async (Guid jobAdId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SubmitJobApplicationCommand(jobAdId), ct);
            return result.ToApiResult("درخواست شما ثبت شد.", 201);
        }).RequireAuthorization("CandidateOnly");

        // مسیر «ارسال مستقیم» — بدون تکمیل رزومه‌ساز، صرفاً نام و فایل رزومه (سند رزومه‌ساز + اعلان وضعیت).
        applicationGroup.MapPost("/job-ads/{jobAdId:guid}/applications/direct", async (
            Guid jobAdId, IFormFile file, [FromForm] string fullName, ISender sender, CancellationToken ct) =>
        {
            await using var stream = file.OpenReadStream();
            var command = new SubmitJobApplicationWithResumeFileCommand(jobAdId, stream, file.FileName, fullName);
            var result = await sender.Send(command, ct);
            return result.ToApiResult("درخواست شما ثبت شد.", 201);
        }).RequireAuthorization("CandidateOnly").DisableAntiforgery();

        // پیگیری بدون نیاز به ورود — طبق سند 04-Api-Contract.md بخش ۵.۵ و سند 02 بخش ۵.۲.
        applicationGroup.MapGet("/applications/track/{trackingToken}", async (string trackingToken, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetApplicationByTrackingTokenQuery(trackingToken), ct);
            return result.ToApiResult();
        });

        applicationGroup.MapGet("/job-ads/{jobAdId:guid}/applications", async (Guid jobAdId, int? page, int? pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetApplicationsForJobAdQuery(jobAdId, page is > 0 ? page.Value : 1, pageSize is > 0 ? pageSize.Value : 20), ct);
            return result.ToApiResult();
        }).RequireAuthorization("CompanyManagerOnly");

        // ویجت داشبورد شرکت «آخرین رزومه‌های دریافتی» — جدیدترین متقاضیان در میان همهٔ آگهی‌های شرکت جاری.
        applicationGroup.MapGet("/companies/me/recent-applicants", async (int? count, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetRecentApplicantsForCompanyQuery(count is > 0 ? count.Value : 5), ct);
            return result.ToApiResult();
        }).RequireAuthorization("CompanyManagerOnly");

        applicationGroup.MapPost("/applications/{applicationId:guid}/status", async (Guid applicationId, UpdateStatusRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateApplicationStatusCommand(
                applicationId, dto.NewStatus, dto.CompanyNotes, dto.InterviewDateTimeUtc, dto.NotifyCandidate ?? true);
            var result = await sender.Send(command, ct);
            return result.ToApiResult("وضعیت درخواست به‌روزرسانی شد.");
        }).RequireAuthorization("CompanyManagerOnly");

        // دانلود امن فایل رزومه یک متقاضی — فقط توسط کارفرمای صاحب همان آگهی (احرازهویت با Bearer Token،
        // برخلاف مسیر عمومی /uploads/... که فایل‌های آپلودی دیگر مانند لوگو/بنر شرکت را بدون احراز سرو می‌کند).
        applicationGroup.MapGet("/job-applications/{applicationId:guid}/resume", async (
            Guid applicationId, ISender sender, IFileStorageService fileStorageService, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetJobApplicationResumeQuery(applicationId), ct);
            if (result.IsFailure)
                return result.ToApiResult();

            var physicalPath = fileStorageService.ResolvePhysicalPath(result.Value.RelativeFileUrl);
            if (!File.Exists(physicalPath))
                return Results.NotFound(IndustrialPlatform.Shared.Api.ApiResponse<object>.Fail("فایل رزومه روی سرور یافت نشد.", 404));

            var contentType = ResumeContentTypeResolver.Resolve(physicalPath);
            return Results.File(physicalPath, contentType, result.Value.SuggestedFileName);
        }).RequireAuthorization("CompanyManagerOnly");

        return app;
    }

    private sealed record UpdateStatusRequestDto(string NewStatus, string? CompanyNotes = null, DateTime? InterviewDateTimeUtc = null, bool? NotifyCandidate = null);

    private sealed record UpsertExperienceRequestDto(string JobTitle, string CompanyName, int StartYear, int? EndYear = null, string? Description = null);
}

/// <summary>تعیین Content-Type فایل رزومه بر اساس پسوند — برای هدر صحیح در پاسخ دانلود.</summary>
internal static class ResumeContentTypeResolver
{
    public static string Resolve(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        _ => "application/octet-stream"
    };
}
