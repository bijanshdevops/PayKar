using IndustrialPlatform.Api.Common;
using IndustrialPlatform.Application.Companies.Commands;
using IndustrialPlatform.Application.Companies.Queries;
using IndustrialPlatform.Identity.Features.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IndustrialPlatform.Api.Endpoints;

/// <summary>اندپوینت‌های مدیریت شرکت — طبق سند 04-Api-Contract.md بخش ۵.۳ و سند 05 بخش ۳ (RBAC).</summary>
public static class CompanyEndpoints
{
    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/companies").WithTags("Companies").RequireAuthorization();

        group.MapPost("/", async (CreateCompanyCommand command, ISender sender, System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);

            // طبق ADR-010: اولین ثبت موفق پروفایل شرکت توسط یک کاربر، نقش CompanyManager را
            // به‌صورت خودکار و Idempotent به او می‌دهد (حتی اگر قبلاً فقط Candidate بوده باشد).
            if (result.IsSuccess)
            {
                var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    await sender.Send(new EnsureCompanyManagerRoleCommand(userId), ct);
                }
            }

            return result.ToApiResult("شرکت با موفقیت ثبت شد و در انتظار احراز هویت است.", 201);
        });

        group.MapGet("/me", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyCompanyQuery(), ct);
            return result.ToApiResult();
        });

        // طبق تسک #74: آمار اختصاصی پنل شرکت (تعداد آگهی‌ها، رزومه‌های دریافتی، بنرها، پرداختی‌ها).
        group.MapGet("/me/dashboard", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new IndustrialPlatform.Application.Dashboard.Queries.GetCompanyDashboardQuery(), ct);
            return result.ToApiResult();
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCompanyByIdQuery(id), ct);
            return result.ToApiResult();
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateCompanyRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateCompanyCommand(
                id, dto.Name, dto.AddressDetail, dto.IndustryCategory, dto.IndustrialZoneId, dto.ContactPhoneNumber,
                dto.Website, dto.Email, dto.Description);
            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });

        group.MapPost("/{id:guid}/verification-request", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RequestCompanyVerificationCommand(id), ct);
            return result.ToApiResult();
        });

        // ---------- مدارک احراز هویت شرکت (کارت ملی، آگهی تاسیس، پروانه بهره‌برداری و...) ----------
        // .DisableAntiforgery(): این API کاملاً Stateless/JWT است (نه Cookie-Session)، پس محافظت
        // Antiforgery (که برای سناریوی CSRF مرورگرهای Cookie-محور طراحی شده) اینجا مصداق ندارد. بدون
        // این فراخوانی، هر اندپوینت دارای پارامتر IFormFile در ASP.NET Core ۸+ با
        // InvalidOperationException («contains anti-forgery metadata, but a middleware was not found»)
        // در همان لحظه دریافت درخواست با خطای 500 متوقف می‌شود — این دقیقاً علت اصلی خطاهای 500
        // گزارش‌شده روی آپلود لوگو/بنر/مدارک شرکت و ایجاد تیکت پشتیبانی بود.
        group.MapPost("/documents", async (IFormFile file, [FromForm] string documentType, ISender sender, CancellationToken ct) =>
        {
            await using var stream = file.OpenReadStream();
            var command = new UploadCompanyDocumentCommand(stream, file.FileName, documentType, file.Length);
            var result = await sender.Send(command, ct);
            return result.ToApiResult("مدرک با موفقیت آپلود شد.", 201);
        }).DisableAntiforgery();

        group.MapDelete("/documents/{documentId:guid}", async (Guid documentId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteCompanyDocumentCommand(documentId), ct);
            return result.ToApiResult("مدرک با موفقیت حذف شد.");
        });

        group.MapGet("/{id:guid}/documents", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCompanyDocumentsQuery(id), ct);
            return result.ToApiResult();
        });

        // ---------- لوگو و بنر پروفایل شرکت ----------
        group.MapPost("/logo", async (IFormFile file, ISender sender, CancellationToken ct) =>
        {
            await using var stream = file.OpenReadStream();
            var result = await sender.Send(new UploadCompanyLogoCommand(stream, file.FileName, file.Length), ct);
            return result.ToApiResult("لوگو با موفقیت به‌روزرسانی شد.");
        }).DisableAntiforgery();

        group.MapPost("/banner", async (IFormFile file, ISender sender, CancellationToken ct) =>
        {
            await using var stream = file.OpenReadStream();
            var result = await sender.Send(new UploadCompanyBannerCommand(stream, file.FileName, file.Length), ct);
            return result.ToApiResult("تصویر بنر با موفقیت به‌روزرسانی شد.");
        }).DisableAntiforgery();

        var adminGroup = app.MapGroup("/api/v1/admin/companies").WithTags("Admin - Companies").RequireAuthorization("AdminOnly");

        adminGroup.MapGet("/pending", async (int page, int pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPendingCompaniesQuery(page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize), ct);
            return result.ToApiResult();
        });

        adminGroup.MapPost("/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ApproveCompanyCommand(id), ct);
            return result.ToApiResult("شرکت تایید شد.");
        });

        adminGroup.MapPost("/{id:guid}/reject", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RejectCompanyCommand(id), ct);
            return result.ToApiResult("شرکت رد شد.");
        });

        return app;
    }

    private sealed record UpdateCompanyRequestDto(
        string Name,
        string AddressDetail,
        string IndustryCategory,
        Guid IndustrialZoneId,
        string? ContactPhoneNumber = null,
        string? Website = null,
        string? Email = null,
        string? Description = null);
}
