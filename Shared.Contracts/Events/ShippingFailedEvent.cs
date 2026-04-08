namespace Shared.Contracts.Events;

public class ShippingFailedEvent
{
    public int OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
}