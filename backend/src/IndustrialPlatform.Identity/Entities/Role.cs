using IndustrialPlatform.Shared.Entities;

namespace IndustrialPlatform.Identity.Entities;

/// <summary>نقش سیستمی — طبق سند 00-Project-Overview.md: Admin, CompanyManager, Candidate.</summary>
public sealed class Role : BaseEntity<Guid>
{
    public const string Admin = "Admin";
    public const string CompanyManager = "CompanyManager";
    public const string Candidate = "Candidate";

    public string Name { get; private set; } = string.Empty;

    private Role() { }

    private Role(Guid id, string name) : base(id)
    {
        Name = name;
    }

    public static Role Create(string name) => new(Guid.NewGuid(), name);
}
