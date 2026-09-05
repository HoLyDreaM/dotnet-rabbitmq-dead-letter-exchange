using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMqDlx.Shared.Logging;
using RabbitMqDlx.Shared.Options;

namespace RabbitMqDlx.Shared.Topology;

/// <summary>
/// Exchange/queue/binding kurulumunu idempotent ve durable şekilde uygular.
/// </summary>
public static class RabbitMqTopology
{
    public static async Task EnsureAsync(
        IChannel channel,
        RabbitMqOptions options,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(options);

        await channel.ExchangeDeclareAsync(
            exchange: TopologyNames.OrdersExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        await channel.ExchangeDeclareAsync(
            exchange: TopologyNames.RetryExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        await channel.ExchangeDeclareAsync(
            exchange: TopologyNames.DlxExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var mainArgs = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = TopologyNames.RetryExchange,
            ["x-dead-letter-routing-key"] = TopologyNames.Retry5sRoutingKey
        };

        await channel.QueueDeclareAsync(
            queue: TopologyNames.MainQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: mainArgs,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        await channel.QueueBindAsync(
            queue: TopologyNames.MainQueue,
            exchange: TopologyNames.OrdersExchange,
            routingKey: TopologyNames.OrderCreatedRoutingKey,
            arguments: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var retryArgs = new Dictionary<string, object?>
        {
            ["x-message-ttl"] = options.RetryTtlMilliseconds,
            ["x-dead-letter-exchange"] = TopologyNames.OrdersExchange,
            ["x-dead-letter-routing-key"] = TopologyNames.OrderCreatedRoutingKey
        };

        await channel.QueueDeclareAsync(
            queue: TopologyNames.Retry5sQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: retryArgs,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        await channel.QueueBindAsync(
            queue: TopologyNames.Retry5sQueue,
            exchange: TopologyNames.RetryExchange,
            routingKey: TopologyNames.Retry5sRoutingKey,
            arguments: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        await channel.QueueDeclareAsync(
            queue: TopologyNames.Dlq,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        await channel.QueueBindAsync(
            queue: TopologyNames.Dlq,
            exchange: TopologyNames.DlxExchange,
            routingKey: TopologyNames.DeadRoutingKey,
            arguments: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        logger?.LogInformation(
            LogEvents.TopologyEnsured,
            "RabbitMQ topolojisi hazır: main={MainQueue}, retry={RetryQueue}, dlq={Dlq}",
            TopologyNames.MainQueue,
            TopologyNames.Retry5sQueue,
            TopologyNames.Dlq);
    }

    public static async Task EnsureAsync(
        IChannel channel,
        IOptions<RabbitMqOptions> options,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureAsync(channel, options.Value, logger, cancellationToken).ConfigureAwait(false);
    }
}
