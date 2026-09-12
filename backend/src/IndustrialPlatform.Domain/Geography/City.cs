using IndustrialPlatform.Shared.Entities;

namespace IndustrialPlatform.Domain.Geography;

/// <summary>شهر/شهرستان — طبق سند 02-Domain-Glossary.md بخش ۱.۱.</summary>
public sealed class City : BaseEntity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public Guid ProvinceId { get; private set; }

    private City() { }

    private City(Guid id, string name, Guid provinceId) : base(id)
    {
        Name = name;
        ProvinceId = provinceId;
    }

    public static City Create(string name, Guid provinceId) => new(Guid.NewGuid(), name, provinceId);
}
