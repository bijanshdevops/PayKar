using IndustrialPlatform.Shared.Entities;

namespace IndustrialPlatform.Domain.Geography;

/// <summary>استان — طبق سند 02-Domain-Glossary.md بخش ۱.۱.</summary>
public sealed class Province : BaseEntity<Guid>
{
    public string Name { get; private set; } = string.Empty;

    private Province() { }

    private Province(Guid id, string name) : base(id)
    {
        Name = name;
    }

    public static Province Create(string name) => new(Guid.NewGuid(), name);
}
