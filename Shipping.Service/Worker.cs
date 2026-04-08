using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;
using Shipping.Service.Messaging;
using Shipping.Service.Publishing;

namespace Shipping.Service;

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
            queue: "payment-approved-for-shipping",
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

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            var message = JsonSerializer.Deserialize<PaymentApprovedEvent>(json);

            if (message is not null)
            {
                _logger.LogInformation(
                    "Shipping recibió PaymentApprovedEvent. OrderId: {OrderId}",
                    message.OrderId);

                var shippingCreatedEvent = new ShippingCreatedEvent
                {
                    OrderId = message.OrderId,
                    ShipmentReference = $"SHIP-{message.OrderId}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    EstimatedDispatchDate = DateTime.UtcNow.AddDays(1)
                };

                await _publisher.PublishAsync(
                    "shipping-created-for-api",
                    shippingCreatedEvent,
                    stoppingToken);

                _logger.LogInformation(
                    "Shipping publicó ShippingCreatedEvent para API. OrderId: {OrderId}",
                    message.OrderId);
            }

            await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
        };

        await _channel.BasicConsumeAsync(
            queue: "payment-approved-for-shipping",
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