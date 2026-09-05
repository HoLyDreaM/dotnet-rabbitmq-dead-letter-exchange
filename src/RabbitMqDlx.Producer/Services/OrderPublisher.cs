using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMqDlx.Shared.Logging;
using RabbitMqDlx.Shared.Messaging;
using RabbitMqDlx.Shared.Models;
using RabbitMqDlx.Shared.Options;
using RabbitMqDlx.Shared.Topology;

namespace RabbitMqDlx.Producer.Services;

public interface IOrderPublisher
{
    Task PublishAsync(OrderMessage message, CancellationToken cancellationToken = default);
}

public sealed class OrderPublisher : IOrderPublisher, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqOptions _options;
    private readonly ILogger<OrderPublisher> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _topologyReady;

    public OrderPublisher(IOptions<RabbitMqOptions> options, ILogger<OrderPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(OrderMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureChannelAsync(cancellationToken).ConfigureAwait(false);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));
            var props = new BasicProperties
            {
                ContentType = MessageHeaders.ContentTypeJson,
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = message.OrderId.ToString("D"),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Headers = MessageHeaders.WithRetryCount(null, 0)
            };

            // Publisher confirmation tracking açıkken BasicPublishAsync onay gelene kadar tamamlanır.
            await _channel!.BasicPublishAsync(
                exchange: TopologyNames.OrdersExchange,
                routingKey: TopologyNames.OrderCreatedRoutingKey,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                LogEvents.PublishConfirmed,
                "Mesaj onaylandı (publisher confirm): OrderId={OrderId} FailureMode={FailureMode}",
                message.OrderId,
                message.FailureMode);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                LogEvents.PublishFailed,
                ex,
                "Yayınlama başarısız: OrderId={OrderId}",
                message.OrderId);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureChannelAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true } && _topologyReady)
        {
            return;
        }

        if (_channel is not null)
        {
            await _channel.DisposeAsync().ConfigureAwait(false);
            _channel = null;
        }

        if (_connection is null || !_connection.IsOpen)
        {
            if (_connection is not null)
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
            }

            _connection = await RabbitMqConnectionFactory
                .CreateConnectionAsync(_options, "RabbitMqDlx.Producer", _logger, cancellationToken)
                .ConfigureAwait(false);
        }

        _channel = await _connection
            .CreateChannelAsync(RabbitMqConnectionFactory.PublisherConfirmChannelOptions, cancellationToken)
            .ConfigureAwait(false);

        await RabbitMqTopology.EnsureAsync(_channel, _options, _logger, cancellationToken)
            .ConfigureAwait(false);
        _topologyReady = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync().ConfigureAwait(false);
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        _gate.Dispose();
    }
}
