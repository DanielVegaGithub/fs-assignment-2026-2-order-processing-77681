namespace OrderProcessing.Application.Requests;

public class CheckoutOrderRequest
{
    public int CustomerId { get; set; }
    public List<CheckoutOrderItemRequest> Items { get; set; } = new();
}