using IndustrialPlatform.Application.Payments;
using IndustrialPlatform.Domain.Payments;
using IndustrialPlatform.Shared.Api;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly AppDbContext _dbContext;

    public PaymentTransactionRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.PaymentTransactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<PaymentTransaction?> GetByAuthorityAsync(string authority, CancellationToken cancellationToken = default) =>
        _dbContext.PaymentTransactions.FirstOrDefaultAsync(t => t.Authority == authority, cancellationToken);

    public async Task<PagedResult<PaymentTransaction>> GetPagedByCompanyIdAsync(
        Guid companyId,
        int page,
        int pageSize,
        PaymentTransactionStatus? status = null,
        PaymentPurpose? purpose = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PaymentTransactions.Where(t => t.CompanyId == companyId);

        if (status is not null)
            query = query.Where(t => t.Status == status.Value);

        if (purpose is not null)
            query = query.Where(t => t.Purpose == purpose.Value);

        if (fromUtc is not null)
            query = query.Where(t => t.CreatedAtUtc >= fromUtc.Value);

        if (toUtc is not null)
            query = query.Where(t => t.CreatedAtUtc <= toUtc.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t =>
                t.Authority.Contains(term) ||
                (t.RefId != null && t.RefId.Contains(term)) ||
                (t.JobAdId != null && _dbContext.JobAds.Any(j => j.Id == t.JobAdId && j.Title.Contains(term))));
        }

        query = query.OrderByDescending(t => t.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return PagedResult<PaymentTransaction>.Create(items, totalCount, page, pageSize);
    }

    public async Task<CompanyTransactionAggregate> GetCompanyAggregateAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PaymentTransactions.Where(t => t.CompanyId == companyId);

        var totalCount = await query.CountAsync(cancellationToken);
        var successfulQuery = query.Where(t => t.Status == PaymentTransactionStatus.Success);
        var successfulCount = await successfulQuery.CountAsync(cancellationToken);
        var totalPaid = await successfulQuery.SumAsync(t => (long?)t.AmountInRials, cancellationToken) ?? 0L;
        var lastAt = await query.OrderByDescending(t => t.CreatedAtUtc).Select(t => (DateTime?)t.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);

        return new CompanyTransactionAggregate(totalPaid, successfulCount, totalCount, lastAt);
    }

    public void Add(PaymentTransaction transaction) => _dbContext.PaymentTransactions.Add(transaction);
}
