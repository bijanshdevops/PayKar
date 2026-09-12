using IndustrialPlatform.Shared.Entities;

namespace IndustrialPlatform.Domain.Geography;

/// <summary>شهرک/ناحیه صنعتی — طبق سند 02-Domain-Glossary.md بخش ۱.۲.</summary>
public sealed class IndustrialZone : BaseEntity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public Guid CityId { get; private set; }
    public ZoneType ZoneType { get; private set; }

    private IndustrialZone() { }

    private IndustrialZone(Guid id, string name, Guid cityId, ZoneType zoneType) : base(id)
    {
        Name = name;
        CityId = cityId;
        ZoneType = zoneType;
    }

    public static IndustrialZone Create(string name, Guid cityId, ZoneType zoneType) =>
        new(Guid.NewGuid(), name, cityId, zoneType);
}
