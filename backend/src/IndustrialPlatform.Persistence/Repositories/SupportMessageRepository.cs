using IndustrialPlatform.Application.Support;
using IndustrialPlatform.Domain.Support;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class SupportMessageRepository : ISupportMessageRepository
{
    private readonly AppDbContext _dbContext;

    public SupportMessageRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<SupportMessage>> GetByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default) =>
        await _dbContext.SupportMessages
            .Where(m => m.TicketId == ticketId)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, DateTime>> GetLastMessageTimestampsAsync(
        IReadOnlyCollection<Guid> ticketIds, CancellationToken cancellationToken = default)
    {
        if (ticketIds.Count == 0)
            return new Dictionary<Guid, DateTime>();

        var grouped = await _dbContext.SupportMessages
            .Where(m => ticketIds.Contains(m.TicketId))
            .GroupBy(m => m.TicketId)
            .Select(g => new { TicketId = g.Key, LastCreatedAtUtc = g.Max(m => m.CreatedAtUtc) })
            .ToListAsync(cancellationToken);

        return grouped.ToDictionary(g => g.TicketId, g => g.LastCreatedAtUtc);
    }

    public void Add(SupportMessage message) => _dbContext.SupportMessages.Add(message);
}
