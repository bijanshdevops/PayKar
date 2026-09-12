using IndustrialPlatform.Application.Common.Interfaces;

namespace IndustrialPlatform.Persistence.Common;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
