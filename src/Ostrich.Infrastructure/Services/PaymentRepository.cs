using Microsoft.EntityFrameworkCore;
using Ostrich.Core.Models;
using Ostrich.Core.Services;
using Ostrich.Infrastructure.Data;

namespace Ostrich.Infrastructure.Services;

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

    public async Task UpdateAsync(Payment payment, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        context.Payments.Update(payment);
        await context.SaveChangesAsync(ct);
    }

    public async Task ProcessPaymentAsync(Payment payment, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        var existing = await context.Payments
            .FirstOrDefaultAsync(p => p.ExternalId == payment.ExternalId, ct);

        if (existing is not null)
            return;

        var account = await context.Accounts.FindAsync([payment.AccountId], ct);
        if (account is null)
            throw new InvalidOperationException($"Account {payment.AccountId} not found");

        payment.Status = "Processed";
        payment.ProcessedAt = DateTime.UtcNow;
        context.Payments.Add(payment);

        var ledger = new Ledger
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            AccountId = payment.AccountId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            CreatedAt = DateTime.UtcNow
        };
        context.Ledgers.Add(ledger);

        account.Balance += payment.Amount;
        account.Version += 1;
        account.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
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
