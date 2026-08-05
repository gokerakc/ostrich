namespace Ostrich.Core.Models;

public class Payment
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Merchant { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    
    public DateTime? CancelledAt { get; set; }
    
    public DateTime? RefundedAt { get; set; }

    public bool IsProcessed => Status == "Processed";
}
