namespace Shared.Contracts.Events;

public class OrderSubmittedEvent
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderSubmittedItem> Items { get; set; } = new();
}

public class OrderSubmittedItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
