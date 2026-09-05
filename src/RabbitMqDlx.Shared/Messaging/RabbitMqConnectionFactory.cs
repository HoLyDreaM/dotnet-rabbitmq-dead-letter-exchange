using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMqDlx.Shared.Logging;
using RabbitMqDlx.Shared.Options;

namespace RabbitMqDlx.Shared.Messaging;

public static class RabbitMqConnectionFactory
{
    public static ConnectionFactory Create(RabbitMqOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };
    }

    public static ConnectionFactory Create(IOptions<RabbitMqOptions> options)
        => Create(options.Value);

    public static async Task<IConnection> CreateConnectionAsync(
        RabbitMqOptions options,
        string clientProvidedName,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var factory = Create(options);
        var connection = await factory.CreateConnectionAsync(clientProvidedName, cancellationToken)
            .ConfigureAwait(false);

        logger?.LogInformation(
            LogEvents.ConnectionOpened,
            "RabbitMQ bağlantısı açıldı: {Host}:{Port} vhost={VHost} client={Client}",
            options.HostName,
            options.Port,
            options.VirtualHost,
            clientProvidedName);

        return connection;
    }

    public static CreateChannelOptions PublisherConfirmChannelOptions { get; } =
        new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
}
