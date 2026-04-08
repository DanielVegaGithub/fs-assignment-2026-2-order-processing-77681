namespace OrderProcessing.Domain.Entities;

public class InventoryRecord
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public bool IsAvailable { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}