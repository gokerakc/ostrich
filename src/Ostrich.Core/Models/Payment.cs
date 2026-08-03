using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Ostrich.Core.Models;

public class Payment
{
    [Key]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("amount")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    [MaxLength(3)]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("merchant")]
    [MaxLength(200)]
    public string Merchant { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    [NotMapped]
    public bool IsProcessed => Status == "Processed";
}
