namespace Ostrich.Core.Models;

public class Ledger
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}