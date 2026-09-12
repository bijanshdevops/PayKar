namespace IndustrialPlatform.Identity.Services;

/// <summary>پورت هش/تایید رمز عبور — طبق ADR-005. پیاده‌سازی با BCrypt (وابستگی از پیش موجود پروژه Identity).</summary>
public interface IPasswordHasher
{
    string Hash(string plainPassword);
    bool Verify(string plainPassword, string passwordHash);
}
