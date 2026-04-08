using Inventory.Service;
using Inventory.Service.Messaging;
using Inventory.Service.Publishing;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/inventory-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog();

    var rabbitMqSettings = new RabbitMqSettings();
    builder.Configuration.GetSection("RabbitMq").Bind(rabbitMqSettings);

    builder.Services.AddSingleton(rabbitMqSettings);
    builder.Services.AddSingleton<RabbitMqPublisher>();
    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Inventory.Service terminó con error");
}
finally
{
    Log.CloseAndFlush();
}