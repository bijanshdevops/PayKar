namespace IndustrialPlatform.Infrastructure.Sms;

public sealed class MeliPayamakSettings
{
    public const string SectionName = "MeliPayamak";

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BodyId { get; set; } = "0";
    public string ApiBaseUrl { get; set; } = "https://rest.payamak-panel.com/api/SendSMS/";
}
