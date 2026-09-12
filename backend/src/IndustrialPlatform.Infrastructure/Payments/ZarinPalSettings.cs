namespace IndustrialPlatform.Infrastructure.Payments;

public sealed class ZarinPalSettings
{
    public const string SectionName = "ZarinPal";

    public string MerchantId { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://payment.zarinpal.com/pg/v4/payment/";
    public string StartPayUrl { get; set; } = "https://payment.zarinpal.com/pg/StartPay/";
    public bool Sandbox { get; set; }
}
