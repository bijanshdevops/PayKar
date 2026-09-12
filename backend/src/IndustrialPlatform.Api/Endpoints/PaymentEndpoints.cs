using IndustrialPlatform.Api.Common;
using IndustrialPlatform.Application.Ads.Commands;
using IndustrialPlatform.Application.Payments.Commands;
using IndustrialPlatform.Application.Payments.Queries;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace IndustrialPlatform.Api.Endpoints;

/// <summary>
/// اندپوینت‌های پرداخت هزینه ثابت ثبت آگهی (ZarinPal) — طبق سند 04-Api-Contract.md بخش ۵.۶
/// و سند 05-Security-Rules.md بخش ۶ (Verify همیشه سمت سرور). جایگزین کامل سیستم پلن‌های ارتقاء/Boost.
/// </summary>
public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var managedGroup = app.MapGroup("/api/v1/payments").WithTags("Payments").RequireAuthorization("CompanyManagerOnly");

        managedGroup.MapGet("/listing-fee", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetJobAdListingFeeQuery(), ct);
            return result.ToApiResult();
        });

        // صفحهٔ «امور مالی و تراکنش‌ها» در داشبورد کارفرما — طبق 04_company_dashboard_spec.md.
        managedGroup.MapGet("/company/transactions", async (
            int? page, int? pageSize, string? status, string? purpose, DateTime? from, DateTime? to, string? search,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetMyCompanyTransactionsQuery(page is > 0 ? page.Value : 1, pageSize is > 0 ? pageSize.Value : 20, status, purpose, from, to, search), ct);
            return result.ToApiResult();
        });

        // خلاصهٔ آماری (کارت‌های بالای صفحه) — مانده کیف پول + تجمیع کل تراکنش‌های شرکت.
        managedGroup.MapGet("/company/transactions/summary", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyCompanyTransactionSummaryQuery(), ct);
            return result.ToApiResult();
        });

        managedGroup.MapPost("/job-ads/{jobAdId:guid}/submit-for-review", async (Guid jobAdId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SubmitJobAdForReviewCommand(jobAdId), ct);
            return result.ToApiResult("به درگاه پرداخت هدایت می‌شوید.", 201);
        });

        // پرداخت هزینه ثابت بنر تبلیغاتی — طبق ADR-008.
        managedGroup.MapPost("/banner-ads/{bannerAdId:guid}/submit-for-review", async (Guid bannerAdId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SubmitBannerAdForReviewCommand(bannerAdId), ct);
            return result.ToApiResult("به درگاه پرداخت هدایت می‌شوید.", 201);
        });

        // اندپوینت بازگشت زرین‌پال — بدون احراز هویت (مرورگر کاربر مستقیماً به این آدرس هدایت می‌شود)
        // و در نهایت با Redirect به صفحه نتیجه در فرانت‌اند ختم می‌شود.
        var publicGroup = app.MapGroup("/api/v1/payments").WithTags("Payments");

        publicGroup.MapGet("/callback", async (
            string Authority, string Status, ISender sender, IConfiguration configuration, CancellationToken ct) =>
        {
            var frontendBaseUrl = (configuration["FrontendBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
            var result = await sender.Send(new ProcessPaymentCallbackCommand(Authority, Status), ct);

            if (result.IsFailure)
            {
                var failureUrl = $"{frontendBaseUrl}/payment-result?success=false&message={Uri.EscapeDataString(result.Error.Message)}";
                return Results.Redirect(failureUrl);
            }

            var payload = result.Value;
            var query = $"success={payload.Success.ToString().ToLowerInvariant()}" +
                        $"&refId={Uri.EscapeDataString(payload.RefId ?? string.Empty)}" +
                        $"&message={Uri.EscapeDataString(payload.Message ?? string.Empty)}" +
                        $"&jobAdId={payload.JobAdId}" +
                        $"&bannerAdId={payload.BannerAdId}" +
                        $"&candidateId={payload.CandidateId}";
            return Results.Redirect($"{frontendBaseUrl}/payment-result?{query}");
        });

        return app;
    }
}
