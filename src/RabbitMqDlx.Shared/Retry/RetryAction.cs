namespace RabbitMqDlx.Shared.Retry;

public enum RetryAction
{
    Process = 0,
    PublishToRetry = 1,
    PublishToDlq = 2
}
