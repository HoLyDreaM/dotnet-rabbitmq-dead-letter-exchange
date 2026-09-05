using Microsoft.Extensions.Logging;

namespace RabbitMqDlx.Shared.Logging;

/// <summary>
/// Yapılandırılmış log olay kimlikleri (1000–2005).
/// </summary>
public static class LogEvents
{
    public static readonly EventId TopologyEnsured = new(1000, nameof(TopologyEnsured));
    public static readonly EventId ConnectionOpened = new(1001, nameof(ConnectionOpened));
    public static readonly EventId MessagePublished = new(1100, nameof(MessagePublished));
    public static readonly EventId PublishConfirmed = new(1101, nameof(PublishConfirmed));
    public static readonly EventId PublishFailed = new(1102, nameof(PublishFailed));
    public static readonly EventId MessageReceived = new(1200, nameof(MessageReceived));
    public static readonly EventId MessageProcessed = new(1201, nameof(MessageProcessed));
    public static readonly EventId TransientRetry = new(1300, nameof(TransientRetry));
    public static readonly EventId MaxRetryExceeded = new(1301, nameof(MaxRetryExceeded));
    public static readonly EventId PoisonToDlq = new(1400, nameof(PoisonToDlq));
    public static readonly EventId PublishedToDlq = new(1401, nameof(PublishedToDlq));
    public static readonly EventId ConsumerFault = new(2000, nameof(ConsumerFault));
    public static readonly EventId ChannelClosed = new(2001, nameof(ChannelClosed));
    public static readonly EventId AckCompleted = new(2005, nameof(AckCompleted));
}
