using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderProcessing.Infrastructure.Persistence;

namespace OrderProcessing.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly OrderProcessingDbContext _dbContext;

    public AdminController(OrderProcessingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("orders")]
    public async Task<ActionResult> GetOrders()
    {
        var orders = await _dbContext.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.Id)
            .Select(o => new
            {
                o.Id,
                o.CustomerId,
                o.CreatedAt,
                o.Status,
                o.TotalAmount,
                ItemCount = o.Items.Count
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("orders/{id:int}/workflow")]
    public async Task<ActionResult> GetOrderWorkflow(int id)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        var inventoryRecords = await _dbContext.InventoryRecords
            .Where(r => r.OrderId == id)
            .OrderBy(r => r.Id)
            .ToListAsync();

        var paymentRecords = await _dbContext.PaymentRecords
            .Where(r => r.OrderId == id)
            .OrderBy(r => r.Id)
            .ToListAsync();

        var shipmentRecords = await _dbContext.ShipmentRecords
            .Where(r => r.OrderId == id)
            .OrderBy(r => r.Id)
            .ToListAsync();

        return Ok(new
        {
            Order = new
            {
                order.Id,
                order.CustomerId,
                order.CreatedAt,
                order.Status,
                order.TotalAmount,
                Items = order.Items.Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    i.Quantity,
                    i.UnitPrice
                })
            },
            InventoryRecords = inventoryRecords,
            PaymentRecords = paymentRecords,
            ShipmentRecords = shipmentRecords
        });
    }
}