using Microsoft.EntityFrameworkCore;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;
using OrderProcessing.Infrastructure.Persistence;

namespace OrderProcessing.Api.Services;

public class OrderWorkflowService
{
    private readonly OrderProcessingDbContext _dbContext;

    public OrderWorkflowService(OrderProcessingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> MarkInventoryConfirmedAsync(
        int orderId,
        string notes,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            return false;
        }

        order.Status = OrderStatus.InventoryConfirmed;

        _dbContext.InventoryRecords.Add(new InventoryRecord
        {
            OrderId = orderId,
            IsAvailable = true,
            Notes = notes,
            CheckedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> MarkPaymentApprovedAsync(
        int orderId,
        decimal amount,
        string transactionReference,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            return false;
        }

        order.Status = OrderStatus.PaymentApproved;

        _dbContext.PaymentRecords.Add(new PaymentRecord
        {
            OrderId = orderId,
            Amount = amount,
            IsApproved = true,
            TransactionReference = transactionReference,
            Notes = "Pago aprobado correctamente",
            ProcessedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> MarkShippingCreatedAsync(
        int orderId,
        string shipmentReference,
        DateTime? estimatedDispatchDate,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            return false;
        }

        order.Status = OrderStatus.Completed;

        _dbContext.ShipmentRecords.Add(new ShipmentRecord
        {
            OrderId = orderId,
            ShipmentReference = shipmentReference,
            EstimatedDispatchDate = estimatedDispatchDate,
            IsCreated = true,
            Notes = "Envío creado correctamente",
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}