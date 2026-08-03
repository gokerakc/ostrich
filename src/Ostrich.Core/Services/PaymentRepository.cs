using Microsoft.EntityFrameworkCore;
using Ostrich.Core.Data;
using Ostrich.Core.Models;

namespace Ostrich.Core.Services;

public class PaymentRepository : IPaymentRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public PaymentRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task SaveAsync(Payment payment, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        context.Payments.Add(payment);
        await context.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<Payment>> ListProcessedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var query = context.Payments
            .Where(p => p.Status == "Processed")
            .OrderByDescending(p => p.ProcessedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Payment>(items, total, page, pageSize);
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Payments.FindAsync([id], ct);
    }
}
