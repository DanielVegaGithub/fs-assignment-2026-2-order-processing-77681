using OrderProcessing.Application.Dtos;

namespace OrderProcessing.Api.Data;

public class InMemoryDataStore
{
    public List<ProductDto> Products { get; } = new()
    {
        new ProductDto
        {
            Id = 1,
            Name = "Laptop",
            Price = 999.99m,
            StockQuantity = 10
        },
        new ProductDto
        {
            Id = 2,
            Name = "Mouse",
            Price = 25.50m,
            StockQuantity = 50
        },
        new ProductDto
        {
            Id = 3,
            Name = "Keyboard",
            Price = 70.00m,
            StockQuantity = 30
        }
    };

    public List<OrderDto> Orders { get; } = new();

    public int NextOrderId { get; set; } = 1;
}