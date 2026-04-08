namespace OrderProcessing.Domain.Entities;

public class ShipmentRecord
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ShipmentReference { get; set; } = string.Empty;
    public DateTime? EstimatedDispatchDate { get; set; }
    public bool IsCreated { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
