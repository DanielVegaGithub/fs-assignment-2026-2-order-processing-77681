namespace OrderProcessing.Domain.Enums;

public enum OrderStatus
{
    Cart,
    Submitted,
    InventoryPending,
    InventoryConfirmed,
    InventoryFailed,
    PaymentPending,
    PaymentApproved,
    PaymentFailed,
    ShippingPending,
    ShippingCreated,
    Completed,
    Failed
}