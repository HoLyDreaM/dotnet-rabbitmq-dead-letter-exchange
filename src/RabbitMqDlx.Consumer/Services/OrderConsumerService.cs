using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqDlx.Shared.Logging;
using RabbitMqDlx.Shared.Messaging;
using RabbitMqDlx.Shared.Models;
using RabbitMqDlx.Shared.Options;
using RabbitMqDlx.Shared.Retry;
using RabbitMqDlx.Shared.Topology;

namespace RabbitMqDlx.Consumer.Services;

/// <summary>
/// Yol B: Transient → PublishToRetry(retry+1) + confirm + Ack;
/// MaxRetry aşıldı veya Poison → PublishToDlq + Ack; None → işle + Ack.
/// requeue=true kullanılmaz.
/// </summary>
public sealed class OrderConsumerService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOptions<RabbitMqOptions> _options;
    private readonly ILogger<OrderConsumerService> _logger;

    public OrderConsumerService(IOptions<RabbitMqOptions> options, ILogger<OrderConsumerService> logger)
    {
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.Value;

        while (!stoppingToken.IsCancellationRequested)
        {
            IConnection? connection = null;
            IChannel? channel = null;

            try
            {
                connection = await RabbitMqConnectionFactory
                    .CreateConnectionAsync(opts, "RabbitMqDlx.Consumer", _logger, stoppingToken)
                    .ConfigureAwait(false);

                channel = await connection
                    .CreateChannelAsync(RabbitMqConnectionFactory.PublisherConfirmChannelOptions, stoppingToken)
                    .ConfigureAwait(false);

                await RabbitMqTopology.EnsureAsync(channel, opts, _logger, stoppingToken)
                    .ConfigureAwait(false);

                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, stoppingToken)
                    .ConfigureAwait(false);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, ea) =>
                {
                    await HandleDeliveryAsync(channel, ea, opts, stoppingToken).ConfigureAwait(false);
                };

                await channel.BasicConsumeAsync(
                    queue: TopologyNames.MainQueue,
                    autoAck: false,
                    consumerTag: string.Empty,
                    noLocal: false,
                    exclusive: false,
                    arguments: null,
                    consumer: consumer,
                    cancellationToken: stoppingToken).ConfigureAwait(false);

                _logger.LogInformation(
                    LogEvents.MessageReceived,
                    "Consumer dinliyor: {Queue}",
                    TopologyNames.MainQueue);

                await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogEvents.ConsumerFault,
                    ex,
                    "Consumer bağlantı/kanal hatası; 3 sn sonra yeniden denenecek.");

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
            finally
            {
                if (channel is not null)
                {
                    await channel.DisposeAsync().ConfigureAwait(false);
                }

                if (connection is not null)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }

    private async Task HandleDeliveryAsync(
        IChannel channel,
        BasicDeliverEventArgs ea,
        RabbitMqOptions opts,
        CancellationToken stoppingToken)
    {
        OrderMessage? message = null;
        var retryCount = MessageHeaders.GetRetryCount(ea.BasicProperties.Headers);

        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            message = JsonSerializer.Deserialize<OrderMessage>(json, JsonOptions);

            if (message is null)
            {
                await PublishToDlqAndAckAsync(
                    channel,
                    ea,
                    retryCount,
                    "deserialize-null",
                    stoppingToken).ConfigureAwait(false);
                return;
            }

            _logger.LogInformation(
                LogEvents.MessageReceived,
                "Mesaj alındı: OrderId={OrderId} FailureMode={FailureMode} Retry={Retry}",
                message.OrderId,
                message.FailureMode,
                retryCount);

            var action = RetryDecision.Decide(message.FailureMode, retryCount, opts.MaxRetry);

            switch (action)
            {
                case RetryAction.Process:
                    // Başarılı iş — demo: yalnızca log
                    _logger.LogInformation(
                        LogEvents.MessageProcessed,
                        "Sipariş işlendi: OrderId={OrderId}",
                        message.OrderId);
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken)
                        .ConfigureAwait(false);
                    _logger.LogDebug(LogEvents.AckCompleted, "ACK: OrderId={OrderId}", message.OrderId);
                    break;

                case RetryAction.PublishToRetry:
                    var next = RetryDecision.NextRetryCount(retryCount);
                    await PublishAsync(
                        channel,
                        TopologyNames.RetryExchange,
                        TopologyNames.Retry5sRoutingKey,
                        ea.Body.ToArray(),
                        MessageHeaders.WithRetryCount(ea.BasicProperties.Headers, next),
                        ea.BasicProperties.MessageId,
                        stoppingToken).ConfigureAwait(false);

                    _logger.LogWarning(
                        LogEvents.TransientRetry,
                        "Transient → retry kuyruğu: OrderId={OrderId} Retry={Retry}/{Max}",
                        message.OrderId,
                        next,
                        opts.MaxRetry);

                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken)
                        .ConfigureAwait(false);
                    break;

                case RetryAction.PublishToDlq:
                    var reason = message.FailureMode == FailureMode.Poison
                        ? "poison"
                        : "max-retry-exceeded";

                    if (reason == "max-retry-exceeded")
                    {
                        _logger.LogError(
                            LogEvents.MaxRetryExceeded,
                            "MaxRetry aşıldı → DLQ: OrderId={OrderId} Retry={Retry}",
                            message.OrderId,
                            retryCount);
                    }
                    else
                    {
                        _logger.LogError(
                            LogEvents.PoisonToDlq,
                            "Poison → DLQ: OrderId={OrderId}",
                            message.OrderId);
                    }

                    await PublishToDlqAndAckAsync(channel, ea, retryCount, reason, stoppingToken)
                        .ConfigureAwait(false);
                    break;

                default:
                    await PublishToDlqAndAckAsync(channel, ea, retryCount, "unknown-action", stoppingToken)
                        .ConfigureAwait(false);
                    break;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(LogEvents.ConsumerFault, ex, "JSON parse hatası → DLQ");
            await PublishToDlqAndAckAsync(channel, ea, retryCount, "json-error", stoppingToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Beklenmeyen hata: requeue=true YASAK. DLQ'ya taşı + Ack.
            _logger.LogError(
                LogEvents.ConsumerFault,
                ex,
                "Beklenmeyen işleme hatası → DLQ: OrderId={OrderId}",
                message?.OrderId);
            await PublishToDlqAndAckAsync(channel, ea, retryCount, "unexpected", stoppingToken)
                .ConfigureAwait(false);
        }
    }

    private async Task PublishToDlqAndAckAsync(
        IChannel channel,
        BasicDeliverEventArgs ea,
        int retryCount,
        string reason,
        CancellationToken cancellationToken)
    {
        await PublishAsync(
            channel,
            TopologyNames.DlxExchange,
            TopologyNames.DeadRoutingKey,
            ea.Body.ToArray(),
            MessageHeaders.WithRetryCount(ea.BasicProperties.Headers, retryCount, reason),
            ea.BasicProperties.MessageId,
            cancellationToken).ConfigureAwait(false);

        _logger.LogWarning(
            LogEvents.PublishedToDlq,
            "DLQ'ya yayınlandı: reason={Reason} MessageId={MessageId}",
            reason,
            ea.BasicProperties.MessageId);

        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken)
            .ConfigureAwait(false);
        _logger.LogDebug(LogEvents.AckCompleted, "ACK after DLQ: MessageId={MessageId}", ea.BasicProperties.MessageId);
    }

    private static async Task PublishAsync(
        IChannel channel,
        string exchange,
        string routingKey,
        byte[] body,
        IDictionary<string, object?> headers,
        string? messageId,
        CancellationToken cancellationToken)
    {
        var props = new BasicProperties
        {
            ContentType = MessageHeaders.ContentTypeJson,
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = messageId,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            Headers = headers
        };

        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
