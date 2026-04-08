using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderProcessing.Application.Dtos;
using OrderProcessing.Application.Requests;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;
using OrderProcessing.Infrastructure.Messaging;
using OrderProcessing.Infrastructure.Persistence;
using Shared.Contracts.Events;

namespace OrderProcessing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderProcessingDbContext _dbContext;
    private readonly RabbitMqPublisher _rabbitMqPublisher;

    public OrdersController(
        OrderProcessingDbContext dbContext,
        RabbitMqPublisher rabbitMqPublisher)
    {
        _dbContext = dbContext;
        _rabbitMqPublisher = rabbitMqPublisher;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
    {
        var orders = await _dbContext.Orders
            .Include(o => o.Items)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                CreatedAt = o.CreatedAt,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                Items = o.Items.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(int id)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        var result = new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CreatedAt = order.CreatedAt,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        return Ok(result);
    }

    [HttpGet("{id:int}/status")]
    public async Task<ActionResult<object>> GetOrderStatus(int id)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            order.Id,
            order.Status
        });
    }

    [HttpGet("/api/customers/{customerId:int}/orders")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetCustomerOrders(int customerId)
    {
        var orders = await _dbContext.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                CreatedAt = o.CreatedAt,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                Items = o.Items.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<OrderDto>> Checkout([FromBody] CheckoutOrderRequest request)
    {
        if (request is null)
        {
            return BadRequest("El cuerpo de la petición es obligatorio.");
        }

        if (request.CustomerId <= 0)
        {
            return BadRequest("CustomerId debe ser mayor que 0.");
        }

        var customerExists = await _dbContext.Customers.AnyAsync(c => c.Id == request.CustomerId);

        if (!customerExists)
        {
            return BadRequest("El cliente no existe.");
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            return BadRequest("El pedido debe tener al menos un item.");
        }

        var orderItems = new List<OrderItem>();
        decimal totalAmount = 0;

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
            {
                return BadRequest($"La cantidad del producto {item.ProductId} debe ser mayor que 0.");
            }

            var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);

            if (product is null)
            {
                return BadRequest($"Producto no encontrado: {item.ProductId}");
            }

            orderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });

            totalAmount += item.Quantity * product.Price;
        }

        var order = new Order
        {
            CustomerId = request.CustomerId,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Submitted,
            TotalAmount = totalAmount,
            Items = orderItems
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        var orderSubmittedEvent = new OrderSubmittedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            Items = order.Items.Select(i => new OrderSubmittedItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        await _rabbitMqPublisher.PublishAsync("order-submitted", orderSubmittedEvent);

        var result = new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CreatedAt = order.CreatedAt,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, result);
    }
}