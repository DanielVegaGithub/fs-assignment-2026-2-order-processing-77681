using System.Text;
using System.Text.Json;
using Inventory.Service.Messaging;
using Inventory.Service.Publishing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;

namespace Inventory.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMqSettings _settings;
    private readonly RabbitMqPublisher _publisher;
    private IConnection? _connection;
    private IChannel? _channel;

    public Worker(
        ILogger<Worker> logger,
        RabbitMqSettings settings,
        RabbitMqPublisher publisher)
    {
        _logger = logger;
        _settings = settings;
        _publisher = publisher;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: "order-submitted",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: "inventory-confirmed-for-payment",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: "inventory-confirmed-for-api",
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

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            var message = JsonSerializer.Deserialize<OrderSubmittedEvent>(json);

            if (message is not null)
            {
                _logger.LogInformation(
                    "Inventory recibió OrderSubmittedEvent. OrderId: {OrderId}, CustomerId: {CustomerId}, TotalAmount: {TotalAmount}",
                    message.OrderId,
                    message.CustomerId,
                    message.TotalAmount);

                var inventoryConfirmedEvent = new InventoryConfirmedEvent
                {
                    OrderId = message.OrderId,
                    Notes = "Stock confirmado correctamente"
                };

                await _publisher.PublishAsync(
                    "inventory-confirmed-for-payment",
                    inventoryConfirmedEvent,
                    stoppingToken);

                await _publisher.PublishAsync(
                    "inventory-confirmed-for-api",
                    inventoryConfirmedEvent,
                    stoppingToken);

                _logger.LogInformation(
                    "Inventory publicó InventoryConfirmedEvent para Payment y API. OrderId: {OrderId}",
                    message.OrderId);
            }

            await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
        };

        await _channel.BasicConsumeAsync(
            queue: "order-submitted",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
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