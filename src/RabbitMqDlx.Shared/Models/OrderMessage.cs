namespace RabbitMqDlx.Shared.Models;

public sealed class OrderMessage
{
    public required Guid OrderId { get; init; }

    public required string CustomerId { get; init; }

    public required decimal Amount { get; init; }

    public FailureMode FailureMode { get; init; } = FailureMode.None;

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
