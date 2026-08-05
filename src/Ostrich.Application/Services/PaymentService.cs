using Ostrich.Core.Models;
using Ostrich.Core.Services;

namespace Ostrich.Application.Services;

public record PaymentDto(
    Guid Id,
    decimal Amount,
    string Currency,
    string Merchant,
    string Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    DateTime? RefundedAt,
    DateTime? CancelledAt)
{
    public static PaymentDto FromPayment(Payment p) => new(
        p.Id, p.Amount, p.Currency, p.Merchant, p.Status, p.CreatedAt, p.ProcessedAt, p.RefundedAt, p.CancelledAt);
}

public interface IPaymentService
{
    Task ProcessPaymentAsync(Payment payment, CancellationToken ct = default);
    Task<PagedResult<PaymentDto>> GetPaymentsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<PaymentDto?> GetPaymentAsync(Guid id, CancellationToken ct = default);
    Task<RefundResult> RefundPaymentAsync(Guid id, CancellationToken ct = default);
}

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repository;

    public PaymentService(IPaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task ProcessPaymentAsync(Payment payment, CancellationToken ct = default)
    {
        payment.Status = "Processed";
        payment.ProcessedAt = DateTime.UtcNow;
        await _repository.SaveAsync(payment, ct);
    }

    public async Task<PagedResult<PaymentDto>> GetPaymentsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.ListProcessedAsync(page, pageSize, ct);
        var items = result.Items.Select(PaymentDto.FromPayment).ToList();
        return new PagedResult<PaymentDto>(items, result.Total, result.Page, result.PageSize);
    }

    public async Task<PaymentDto?> GetPaymentAsync(Guid id, CancellationToken ct = default)
    {
        var payment = await _repository.GetByIdAsync(id, ct);
        return payment is null ? null : PaymentDto.FromPayment(payment);
    }

    public async Task<RefundResult> RefundPaymentAsync(Guid id, CancellationToken ct = default)
    {
        var payment = await _repository.GetByIdAsync(id, ct);
        
        if(payment is null)
            return new RefundResult(false, "PaymentNotFound");

        switch (payment.Status)
        {
            case "Refunded":
                return new RefundResult(false, "AlreadyRefunded");
            case "Processed":
                payment.Status = "Refunded";
                payment.RefundedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(payment, ct);
                
                return new RefundResult(true, "");
            case "Pending":
                payment.Status = "Cancelled";
                payment.CancelledAt = DateTime.UtcNow;
                await _repository.UpdateAsync(payment, ct);
                
                return new RefundResult(false, "PaymentCancelled");
            default:
                throw new ArgumentException("Invalid payment status");
        }
    }
}
