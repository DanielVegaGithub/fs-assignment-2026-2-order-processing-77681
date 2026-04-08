namespace Shared.Contracts.Events;

public class PaymentApprovedEvent
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
}