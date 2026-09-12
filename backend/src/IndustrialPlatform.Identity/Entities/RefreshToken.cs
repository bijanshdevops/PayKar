using IndustrialPlatform.Shared.Entities;

namespace IndustrialPlatform.Identity.Entities;

/// <summary>
/// Refresh Token با پشتیبانی چرخش (Rotation) و ابطال — طبق سند 05-Security-Rules.md بخش ۲.
/// مقدار خام توکن هرگز ذخیره نمی‌شود؛ فقط هش آن.
/// </summary>
public sealed class RefreshToken : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;

    private RefreshToken() { }

    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTime expiresAtUtc) : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAtUtc) =>
        new(Guid.NewGuid(), userId, tokenHash, expiresAtUtc);

    public void Revoke(string? replacedByTokenHash = null)
    {
        RevokedAtUtc = DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
