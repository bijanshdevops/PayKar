using IndustrialPlatform.Api.Common;
using IndustrialPlatform.Application.Ads.Commands;
using IndustrialPlatform.Application.Ads.Queries;
using MediatR;

namespace IndustrialPlatform.Api.Endpoints;

/// <summary>اندپوینت‌های بنر تبلیغاتی پنل Owner — طبق ADR-008 سند 08-Decision-Log.md.</summary>
public static class BannerAdEndpoints
{
    public static IEndpointRouteBuilder MapBannerAdEndpoints(this IEndpointRouteBuilder app)
    {
        // نمایش عمومی بنرهای فعال — بدون نیاز به احراز هویت (طبق ADR-008).
        var publicGroup = app.MapGroup("/api/v1/banner-ads").WithTags("BannerAds");

        publicGroup.MapGet("/active", async (string placement, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetActiveBannerAdsQuery(placement), ct);
            return result.ToApiResult();
        });

        // مدیریت بنر توسط شرکت — طبق ADR-008.
        var managedGroup = app.MapGroup("/api/v1/banner-ads").WithTags("BannerAds").RequireAuthorization("CompanyManagerOnly");

        // کاتالوگ جایگاه‌های تبلیغاتی — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۳)،
        // فقط برای شرکت‌های احراز هویت‌شده که در حال ثبت رزرو بنر هستند قابل مشاهده است.
        managedGroup.MapGet("/slots", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetBannerSlotsQuery(), ct);
            return result.ToApiResult();
        });

        // .DisableAntiforgery(): همان دلیل سایر اندپوینت‌های IFormFile این پروژه — API کاملاً Stateless/JWT
        // است، پس محافظت Antiforgery (طراحی‌شده برای CSRF مرورگرهای Cookie-محور) اینجا مصداق ندارد؛
        // بدون این فراخوانی، ASP.NET Core ۸+ با InvalidOperationException این اندپوینت را با خطای 500 متوقف می‌کرد.
        managedGroup.MapPost("/images", async (IFormFile file, ISender sender, CancellationToken ct) =>
        {
            await using var stream = file.OpenReadStream();
            var result = await sender.Send(new UploadBannerImageCommand(stream, file.FileName), ct);
            return result.ToApiResult("تصویر بنر آپلود شد.", 201);
        }).DisableAntiforgery();

        managedGroup.MapPost("/", async (CreateBannerAdRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateBannerAdCommand(dto.ImageUrl, dto.DestinationUrl, dto.Placement, dto.BannerSlotId, dto.DurationDays), ct);
            return result.ToApiResult("بنر به‌صورت پیش‌نویس ایجاد شد. برای انتشار، هزینه بنر را پرداخت و برای بررسی ارسال کنید.", 201);
        });

        managedGroup.MapPut("/{id:guid}", async (Guid id, UpdateBannerAdRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateBannerAdCommand(id, dto.ImageUrl, dto.DestinationUrl, dto.Placement), ct);
            return result.ToApiResult();
        });

        managedGroup.MapGet("/my-company", async (int? page, int? pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyCompanyBannerAdsQuery(page is > 0 ? page.Value : 1, pageSize is > 0 ? pageSize.Value : 20), ct);
            return result.ToApiResult();
        });

        // تمدید بنر با ۲۰٪ تخفیف از طریق کیف‌پول — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۵).
        managedGroup.MapPost("/{id:guid}/renew", async (Guid id, RenewBannerAdRequestDto? dto, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RenewBannerAdCommand(id, dto?.DurationDays), ct);
            return result.ToApiResult("بنر با موفقیت تمدید شد.");
        });

        // ---------- پنل Owner: استعلام/تایید/رد بنر ----------
        var adminGroup = app.MapGroup("/api/v1/admin/banner-ads").WithTags("Admin-BannerAds").RequireAuthorization("AdminOnly");

        adminGroup.MapGet("/pending", async (int? page, int? pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPendingReviewBannerAdsQuery(page is > 0 ? page.Value : 1, pageSize is > 0 ? pageSize.Value : 20), ct);
            return result.ToApiResult();
        });

        adminGroup.MapPost("/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ApproveBannerAdCommand(id), ct);
            return result.ToApiResult("بنر تایید و منتشر شد.");
        });

        adminGroup.MapPost("/{id:guid}/reject", async (Guid id, RejectBannerAdRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RejectBannerAdCommand(id, dto.Reason), ct);
            return result.ToApiResult("بنر رد شد.");
        });

        // ---------- ردیابی سبک عمومی بازدید/کلیک — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۶) ----------
        // یادداشت معماری آگاهانه: این دو اندپوینت به‌عمد از پاکت استاندارد ApiResponse<T> پیروی نمی‌کنند،
        // چون ماهیت HTTP آن‌ها اساساً با JSON envelope ناسازگار است — پاسخ 204 اصلاً نباید بدنه داشته باشد
        // و ریدایرکت ۳۰۲ باید مستقیماً توسط مرورگر دنبال شود (عیناً مطابق الگوی موجود در
        // PaymentEndpoints.MapGet("/callback", ...) که پیش‌تر برای بازگشت از درگاه زرین‌پال پیاده‌سازی شده است).
        var trackingGroup = app.MapGroup("/api/v1/public/banners").WithTags("BannerAds-Public");

        trackingGroup.MapPost("/{id:guid}/impression", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RecordBannerImpressionCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToApiResult();
        });

        trackingGroup.MapGet("/{id:guid}/impression", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RecordBannerImpressionCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToApiResult();
        });

        trackingGroup.MapGet("/{id:guid}/click", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RecordBannerClickCommand(id), ct);
            return result.IsSuccess ? Results.Redirect(result.Value) : result.ToApiResult();
        });

        return app;
    }

    private sealed record CreateBannerAdRequestDto(string ImageUrl, string DestinationUrl, string Placement, int BannerSlotId, int? DurationDays);
    private sealed record UpdateBannerAdRequestDto(string ImageUrl, string DestinationUrl, string Placement);
    private sealed record RejectBannerAdRequestDto(string Reason);
    private sealed record RenewBannerAdRequestDto(int? DurationDays);
}
