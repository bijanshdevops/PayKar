namespace IndustrialPlatform.Identity.Entities;

/// <summary>جدول واسط کاربر-نقش (Many-to-Many).</summary>
public sealed class UserRole
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    private UserRole() { }

    private UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public static UserRole Create(Guid userId, Guid roleId) => new(userId, roleId);
}
