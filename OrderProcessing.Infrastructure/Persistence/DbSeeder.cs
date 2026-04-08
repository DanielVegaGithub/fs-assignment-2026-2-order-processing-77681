using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(OrderProcessingDbContext context)
    {
        if (!context.Products.Any())
        {
            context.Products.AddRange(
                new Product
                {
                    Name = "Laptop",
                    Price = 999.99m,
                    StockQuantity = 10
                },
                new Product
                {
                    Name = "Mouse",
                    Price = 25.50m,
                    StockQuantity = 50
                },
                new Product
                {
                    Name = "Keyboard",
                    Price = 70.00m,
                    StockQuantity = 30
                }
            );
        }

        if (!context.Customers.Any())
        {
            context.Customers.Add(
                new Customer
                {
                    FullName = "Cliente Demo",
                    Email = "cliente.demo@test.com"
                }
            );
        }

        await context.SaveChangesAsync();
    }
}