using Microsoft.EntityFrameworkCore;
using OrderProcessing.Api.Consumers;
using OrderProcessing.Api.Services;
using OrderProcessing.Infrastructure.Messaging;
using OrderProcessing.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;
using System.Text.Json.Serialization;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/order-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ReactFrontend", policy =>
        {
            policy
                .WithOrigins("http://localhost:61259", "http://localhost:61260")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    builder.Services.AddDbContext<OrderProcessingDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

    var rabbitMqSettings = new RabbitMqSettings();
    builder.Configuration.GetSection("RabbitMq").Bind(rabbitMqSettings);

    builder.Services.AddSingleton(rabbitMqSettings);
    builder.Services.AddSingleton<RabbitMqPublisher>();
    builder.Services.AddScoped<OrderWorkflowService>();
    builder.Services.AddHostedService<OrderStatusConsumer>();

    builder.Services.AddOpenApi();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
        await DbSeeder.SeedAsync(dbContext);
    }

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseCors("ReactFrontend");

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "OrderProcessing.Api terminó con error");
}
finally
{
    Log.CloseAndFlush();
}