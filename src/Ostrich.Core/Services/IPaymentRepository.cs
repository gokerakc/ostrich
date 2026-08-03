using Ostrich.Core.Models;

namespace Ostrich.Core.Services;

public interface IPaymentRepository
{
    Task SaveAsync(Payment payment, CancellationToken ct = default);
    Task<PagedResult<Payment>> ListProcessedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
