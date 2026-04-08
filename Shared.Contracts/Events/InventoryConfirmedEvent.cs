namespace Shared.Contracts.Events;

public class InventoryConfirmedEvent
{
    public int OrderId { get; set; }
    public string Notes { get; set; } = string.Empty;
}