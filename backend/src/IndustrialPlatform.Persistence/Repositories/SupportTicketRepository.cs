using IndustrialPlatform.Application.Support;
using IndustrialPlatform.Domain.Support;
using IndustrialPlatform.Shared.Api;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class SupportTicketRepository : ISupportTicketRepository
{
    private readonly AppDbContext _dbContext;

    public SupportTicketRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.SupportTickets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<PagedResult<SupportTicket>> GetByUserIdAsync(
        Guid userId, SupportTicketStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SupportTickets.Where(t => t.UserId == userId);
        if (status is not null)
            query = query.Where(t => t.Status == status.Value);

        query = query.OrderByDescending(t => t.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return PagedResult<SupportTicket>.Create(items, totalCount, page, pageSize);
    }

    public async Task<PagedResult<SupportTicket>> GetAllAsync(SupportTicketStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SupportTickets.AsQueryable();
        if (status is not null)
            query = query.Where(t => t.Status == status.Value);

        query = query.OrderByDescending(t => t.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return PagedResult<SupportTicket>.Create(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyDictionary<SupportTicketStatus, int>> GetStatusCountsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var grouped = await _dbContext.SupportTickets
            .Where(t => t.UserId == userId)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return grouped.ToDictionary(g => g.Status, g => g.Count);
    }

    public async Task<IReadOnlyDictionary<SupportTicketStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken = default)
    {
        var grouped = await _dbContext.SupportTickets
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return grouped.ToDictionary(g => g.Status, g => g.Count);
    }

    public Task<int> CountByCreationYearAsync(int year, CancellationToken cancellationToken = default) =>
        _dbContext.SupportTickets.CountAsync(t => t.CreatedAtUtc.Year == year, cancellationToken);

    public void Add(SupportTicket ticket) => _dbContext.SupportTickets.Add(ticket);
}
