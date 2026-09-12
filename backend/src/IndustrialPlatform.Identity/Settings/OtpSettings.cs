namespace IndustrialPlatform.Identity.Settings;

public sealed class OtpSettings
{
    public const string SectionName = "Otp";

    public int ExpirationSeconds { get; set; } = 120;
    public int MaxRequestsPerWindow { get; set; } = 3;
    public int RequestWindowMinutes { get; set; } = 10;
    public int MaxVerifyAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
}
