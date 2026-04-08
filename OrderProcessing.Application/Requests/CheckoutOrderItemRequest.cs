namespace OrderProcessing.Application.Requests;

public class CheckoutOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}