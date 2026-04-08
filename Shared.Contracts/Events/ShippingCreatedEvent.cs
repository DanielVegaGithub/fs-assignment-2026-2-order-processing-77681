namespace Shared.Contracts.Events;

public class ShippingCreatedEvent
{
    public int OrderId { get; set; }
    public string ShipmentReference { get; set; } = string.Empty;
    public DateTime? EstimatedDispatchDate { get; set; }
}