using System.Text;
using System.Text.Json;
using OrderProcessing.Api.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;

namespace OrderProcessing.Api.Consumers;

public class OrderStatusConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderStatusConsumer> _logger;
    private readonly IConfiguration _configuration;

    private IConnection? _connection;
    private IChannel? _channel;

    public OrderStatusConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderStatusConsumer> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:HostName"],
            Port = int.Parse(_configuration["RabbitMq:Port"]!),
            UserName = _configuration["RabbitMq:UserName"],
            Password = _configuration["RabbitMq:Password"]
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: "inventory-confirmed-for-api",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: "payment-approved-for-api",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: "shipping-created-for-api",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is null)
        {
            return;
        }

        await StartInventoryConfirmedConsumer(stoppingToken);
        await StartPaymentApprovedConsumer(stoppingToken);
        await StartShippingCreatedConsumer(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task StartInventoryConfirmedConsumer(CancellationToken stoppingToken)
    {
        if (_channel is null) return;

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var message = JsonSerializer.Deserialize<InventoryConfirmedEvent>(json);

            if (message is not null)
            {
                using var scope = _scopeFactory.CreateScope();
                var workflowService = scope.ServiceProvider.GetRequiredService<OrderWorkflowService>();

                var updated = await workflowService.MarkInventoryConfirmedAsync(
                    message.OrderId,
                    message.Notes,
                    stoppingToken);

                if (updated)
                {
                    _logger.LogInformation(
                        "Pedido {OrderId} actualizado a InventoryConfirmed y registro de inventario guardado",
                        message.OrderId);
                }
                else
                {
                    _logger.LogWarning(
                        "No se encontró el pedido {OrderId} al procesar InventoryConfirmedEvent",
                        message.OrderId);
                }
            }

            await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
        };

        await _channel.BasicConsumeAsync(
            queue: "inventory-confirmed-for-api",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    private async Task StartPaymentApprovedConsumer(CancellationToken stoppingToken)
    {
        if (_channel is null) return;

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var message = JsonSerializer.Deserialize<PaymentApprovedEvent>(json);

            if (message is not null)
            {
                using var scope = _scopeFactory.CreateScope();
                var workflowService = scope.ServiceProvider.GetRequiredService<OrderWorkflowService>();

                var updated = await workflowService.MarkPaymentApprovedAsync(
                    message.OrderId,
                    message.Amount,
                    message.TransactionReference,
                    stoppingToken);

                if (updated)
                {
                    _logger.LogInformation(
                        "Pedido {OrderId} actualizado a PaymentApproved y registro de pago guardado",
                        message.OrderId);
                }
                else
                {
                    _logger.LogWarning(
                        "No se encontró el pedido {OrderId} al procesar PaymentApprovedEvent",
                        message.OrderId);
                }
            }

            await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
        };

        await _channel.BasicConsumeAsync(
            queue: "payment-approved-for-api",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    private async Task StartShippingCreatedConsumer(CancellationToken stoppingToken)
    {
        if (_channel is null) return;

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var message = JsonSerializer.Deserialize<ShippingCreatedEvent>(json);

            if (message is not null)
            {
                using var scope = _scopeFactory.CreateScope();
                var workflowService = scope.ServiceProvider.GetRequiredService<OrderWorkflowService>();

                var updated = await workflowService.MarkShippingCreatedAsync(
                    message.OrderId,
                    message.ShipmentReference,
                    message.EstimatedDispatchDate,
                    stoppingToken);

                if (updated)
                {
                    _logger.LogInformation(
                        "Pedido {OrderId} actualizado a Completed y registro de envío guardado",
                        message.OrderId);
                }
                else
                {
                    _logger.LogWarning(
                        "No se encontró el pedido {OrderId} al procesar ShippingCreatedEvent",
                        message.OrderId);
                }
            }

            await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
        };

        await _channel.BasicConsumeAsync(
            queue: "shipping-created-for-api",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
            await _connection.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}