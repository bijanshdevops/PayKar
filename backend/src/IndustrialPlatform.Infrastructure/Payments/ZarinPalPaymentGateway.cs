using System.Net.Http.Json;
using System.Text.Json.Serialization;
using IndustrialPlatform.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IndustrialPlatform.Infrastructure.Payments;

/// <summary>
/// آداپتور درگاه پرداخت زرین‌پال — پیاده‌سازی پورت IPaymentGateway.
/// طبق سند 05-Security-Rules.md بخش ۶: Verify همیشه سمت سرور و بر اساس مبلغ ذخیره‌شده انجام می‌شود،
/// هرگز بر اساس ورودی کلاینت.
/// </summary>
public sealed class ZarinPalPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly ZarinPalSettings _settings;
    private readonly ILogger<ZarinPalPaymentGateway> _logger;

    public ZarinPalPaymentGateway(HttpClient httpClient, IOptions<ZarinPalSettings> settings, ILogger<ZarinPalPaymentGateway> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PaymentRequestResult> RequestPaymentAsync(long amountInRials, string description, string callbackUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new ZarinPalRequestBody(_settings.MerchantId, amountInRials, description, callbackUrl);
            using var response = await _httpClient.PostAsJsonAsync($"{_settings.ApiBaseUrl}request.json", requestBody, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<ZarinPalRequestResponse>(cancellationToken: cancellationToken);

            if (result?.Data?.Code != 100 || string.IsNullOrEmpty(result.Data.Authority))
            {
                var message = result?.Errors?.Message ?? "ثبت تراکنش با زرین‌پال ناموفق بود.";
                return new PaymentRequestResult(false, null, null, message);
            }

            var redirectUrl = $"{_settings.StartPayUrl}{result.Data.Authority}";
            return new PaymentRequestResult(true, result.Data.Authority, redirectUrl, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ارتباط با درگاه زرین‌پال هنگام ثبت تراکنش.");
            return new PaymentRequestResult(false, null, null, "ارتباط با درگاه پرداخت برقرار نشد.");
        }
    }

    public async Task<PaymentVerificationResult> VerifyPaymentAsync(string authority, long amountInRials, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new ZarinPalVerifyBody(_settings.MerchantId, amountInRials, authority);
            using var response = await _httpClient.PostAsJsonAsync($"{_settings.ApiBaseUrl}verify.json", requestBody, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<ZarinPalVerifyResponse>(cancellationToken: cancellationToken);

            if (result?.Data?.Code is 100 or 101)
            {
                return new PaymentVerificationResult(true, result.Data.RefId?.ToString(), null);
            }

            var message = result?.Errors?.Message ?? "تایید تراکنش زرین‌پال ناموفق بود.";
            return new PaymentVerificationResult(false, null, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ارتباط با درگاه زرین‌پال هنگام تایید تراکنش.");
            return new PaymentVerificationResult(false, null, "ارتباط با درگاه پرداخت برقرار نشد.");
        }
    }

    private sealed record ZarinPalRequestBody(
        [property: JsonPropertyName("merchant_id")] string MerchantId,
        [property: JsonPropertyName("amount")] long Amount,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("callback_url")] string CallbackUrl);

    private sealed record ZarinPalVerifyBody(
        [property: JsonPropertyName("merchant_id")] string MerchantId,
        [property: JsonPropertyName("amount")] long Amount,
        [property: JsonPropertyName("authority")] string Authority);

    private sealed record ZarinPalRequestResponse(
        [property: JsonPropertyName("data")] ZarinPalRequestData? Data,
        [property: JsonPropertyName("errors")] ZarinPalErrors? Errors);

    private sealed record ZarinPalRequestData(
        [property: JsonPropertyName("code")] int Code,
        [property: JsonPropertyName("authority")] string? Authority);

    private sealed record ZarinPalVerifyResponse(
        [property: JsonPropertyName("data")] ZarinPalVerifyData? Data,
        [property: JsonPropertyName("errors")] ZarinPalErrors? Errors);

    private sealed record ZarinPalVerifyData(
        [property: JsonPropertyName("code")] int Code,
        [property: JsonPropertyName("ref_id")] long? RefId);

    private sealed record ZarinPalErrors(
        [property: JsonPropertyName("message")] string? Message);
}
