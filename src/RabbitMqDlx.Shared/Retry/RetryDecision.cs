using RabbitMqDlx.Shared.Models;

namespace RabbitMqDlx.Shared.Retry;

/// <summary>
/// Yol B: uygulama düzeyinde retry/DLQ kararı. Pure function — broker I/O yok.
/// requeue=true kullanılmaz; karar PublishToRetry / PublishToDlq / Process şeklindedir.
/// </summary>
public static class RetryDecision
{
    public static RetryAction Decide(FailureMode failureMode, int currentRetryCount, int maxRetry)
    {
        if (maxRetry < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetry), maxRetry, "MaxRetry en az 1 olmalıdır.");
        }

        if (currentRetryCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentRetryCount), currentRetryCount, "Retry sayısı negatif olamaz.");
        }

        return failureMode switch
        {
            FailureMode.None => RetryAction.Process,
            FailureMode.Poison => RetryAction.PublishToDlq,
            FailureMode.Transient => currentRetryCount >= maxRetry
                ? RetryAction.PublishToDlq
                : RetryAction.PublishToRetry,
            _ => RetryAction.PublishToDlq
        };
    }

    /// <summary>
    /// Retry kuyruğuna yayınlanırken kullanılacak yeni sayaç (current + 1).
    /// </summary>
    public static int NextRetryCount(int currentRetryCount) => checked(currentRetryCount + 1);
}
