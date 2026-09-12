namespace IndustrialPlatform.Identity.Services;

/// <summary>پیاده‌سازی IPasswordHasher با BCrypt — طبق ADR-005. رمز خام هرگز ذخیره نمی‌شود.</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string plainPassword) => BCrypt.Net.BCrypt.HashPassword(plainPassword);

    public bool Verify(string plainPassword, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // هش نامعتبر/خراب — به‌جای پرتاب Exception، تایید ناموفق در نظر گرفته می‌شود.
            return false;
        }
    }
}
