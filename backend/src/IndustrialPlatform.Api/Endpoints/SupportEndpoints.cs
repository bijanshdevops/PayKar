using IndustrialPlatform.Api.Common;
using IndustrialPlatform.Application.Support.Commands;
using IndustrialPlatform.Application.Support.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IndustrialPlatform.Api.Endpoints;

/// <summary>
/// اندپوینت‌های تیکت پشتیبانی کاربر↔تیم پلتفرم (فانوس سبز ارتباطات) — طبق ADR-007،
/// گسترش‌یافته در فاز «مدیریت پشتیبانی و تیکت‌ها» (Department/Priority/ضمیمه فایل/شمارنده وضعیت).
/// </summary>
public static class SupportEndpoints
{
    public static IEndpointRouteBuilder MapSupportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/support-tickets").WithTags("Support").RequireAuthorization();

        // ایجاد تیکت جدید همراه با اولین پیام و ضمیمه اختیاری — هر سه نقش (Candidate/CompanyManager/Admin) مجازند.
        // multipart/form-data چون ممکن است یک فایل ضمیمه (تصویر/PDF/DOCX) همراه داشته باشد.
        group.MapPost("/", async (
            IFormFile? file, [FromForm] string subject, [FromForm] string message,
            [FromForm] string department, [FromForm] string priority, ISender sender, CancellationToken ct) =>
        {
            if (file is not null)
            {
                await using var stream = file.OpenReadStream();
                var commandWithFile = new CreateSupportTicketCommand(subject, message, department, priority, stream, file.FileName, file.Length);
                var resultWithFile = await sender.Send(commandWithFile, ct);
                return resultWithFile.ToApiResult("تیکت شما با موفقیت ثبت شد.", 201);
            }

            var command = new CreateSupportTicketCommand(subject, message, department, priority);
            var result = await sender.Send(command, ct);
            return result.ToApiResult("تیکت شما با موفقیت ثبت شد.", 201);
        }).DisableAntiforgery();

        // لیست تیکت‌های خودِ کاربر جاری، با فیلتر اختیاری وضعیت.
        group.MapGet("/me", async (string? status, int? page, int? pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyTicketsQuery(status, page is > 0 ? page.Value : 1, pageSize is > 0 ? pageSize.Value : 20), ct);
            return result.ToApiResult();
        });

        // شمارنده تیکت‌های خودِ کاربر جاری به تفکیک وضعیت — برای تب‌های فیلتر صفحه «تیکت‌های من».
        group.MapGet("/me/status-summary", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyTicketStatusSummaryQuery(), ct);
            return result.ToApiResult();
        });

        // لیست همه تیکت‌ها برای پنل پشتیبانی — صرفاً تیم پشتیبانی (Admin).
        group.MapGet("/", async (string? status, int? page, int? pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAllTicketsQuery(status, page is > 0 ? page.Value : 1, pageSize is > 0 ? pageSize.Value : 20), ct);
            return result.ToApiResult();
        }).RequireAuthorization("AdminOnly");

        // شمارنده همه تیکت‌ها (همه کاربران) به تفکیک وضعیت — برای پنل پشتیبانی (Admin).
        group.MapGet("/status-summary", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAllTicketStatusSummaryQuery(), ct);
            return result.ToApiResult();
        }).RequireAuthorization("AdminOnly");

        // جزئیات یک تیکت به‌همراه پیام‌ها — بررسی مالکیت/دسترسی داخل Handler انجام می‌شود.
        group.MapGet("/{ticketId:guid}", async (Guid ticketId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTicketByIdQuery(ticketId), ct);
            return result.ToApiResult();
        });

        // افزودن پیام جدید (و ضمیمه اختیاری) به تیکت — بررسی مالکیت/دسترسی داخل Handler انجام می‌شود.
        group.MapPost("/{ticketId:guid}/messages", async (
            Guid ticketId, IFormFile? file, [FromForm] string body, ISender sender, CancellationToken ct) =>
        {
            if (file is not null)
            {
                await using var stream = file.OpenReadStream();
                var commandWithFile = new AddSupportMessageCommand(ticketId, body, stream, file.FileName, file.Length);
                var resultWithFile = await sender.Send(commandWithFile, ct);
                return resultWithFile.ToApiResult("پیام شما ارسال شد.", 201);
            }

            var command = new AddSupportMessageCommand(ticketId, body);
            var result = await sender.Send(command, ct);
            return result.ToApiResult("پیام شما ارسال شد.", 201);
        }).DisableAntiforgery();

        // تغییر وضعیت تیکت — صرفاً تیم پشتیبانی (Admin).
        group.MapPost("/{ticketId:guid}/status", async (Guid ticketId, UpdateSupportTicketStatusRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateSupportTicketStatusCommand(ticketId, dto.NewStatus), ct);
            return result.ToApiResult("وضعیت تیکت به‌روزرسانی شد.");
        }).RequireAuthorization("AdminOnly");

        // بازگشایی تیکت بسته‌شده — مجاز برای صاحب تیکت (بدون نیاز به نقش Admin؛ بررسی مالکیت داخل Handler).
        group.MapPost("/{ticketId:guid}/reopen", async (Guid ticketId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ReopenSupportTicketCommand(ticketId), ct);
            return result.ToApiResult("تیکت دوباره باز شد.");
        });

        // بستن تیکت توسط صاحب تیکت — کاربر عادی دسترسی به دراپ‌داون AdminOnly ندارد و صرفاً می‌تواند تیکت خودش را ببندد.
        group.MapPost("/{ticketId:guid}/close", async (Guid ticketId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CloseSupportTicketCommand(ticketId), ct);
            return result.ToApiResult("تیکت بسته شد.");
        });

        return app;
    }

    private sealed record UpdateSupportTicketStatusRequestDto(string NewStatus);
}
