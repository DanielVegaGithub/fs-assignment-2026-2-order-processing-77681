namespace OrderProcessing.Domain.Entities;

public class PaymentRecord
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public bool IsApproved { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}