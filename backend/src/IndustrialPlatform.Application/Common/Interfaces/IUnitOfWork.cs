namespace IndustrialPlatform.Application.Common.Interfaces;

/// <summary>پورت خروجی UnitOfWork — پیاده‌سازی واقعی در IndustrialPlatform.Persistence.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
