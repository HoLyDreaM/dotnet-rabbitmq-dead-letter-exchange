using FluentAssertions;
using RabbitMqDlx.Shared.Models;
using RabbitMqDlx.Shared.Retry;

namespace RabbitMqDlx.Tests;

public class RetryDecisionTests
{
    [Fact]
    public void None_returns_Process()
    {
        RetryDecision.Decide(FailureMode.None, currentRetryCount: 0, maxRetry: 3)
            .Should().Be(RetryAction.Process);
    }

    [Fact]
    public void Transient_below_max_returns_PublishToRetry()
    {
        RetryDecision.Decide(FailureMode.Transient, currentRetryCount: 0, maxRetry: 3)
            .Should().Be(RetryAction.PublishToRetry);

        RetryDecision.Decide(FailureMode.Transient, currentRetryCount: 2, maxRetry: 3)
            .Should().Be(RetryAction.PublishToRetry);
    }

    [Fact]
    public void Transient_at_or_above_max_returns_PublishToDlq()
    {
        RetryDecision.Decide(FailureMode.Transient, currentRetryCount: 3, maxRetry: 3)
            .Should().Be(RetryAction.PublishToDlq);

        RetryDecision.Decide(FailureMode.Transient, currentRetryCount: 5, maxRetry: 3)
            .Should().Be(RetryAction.PublishToDlq);
    }

    [Fact]
    public void Poison_always_returns_PublishToDlq()
    {
        RetryDecision.Decide(FailureMode.Poison, currentRetryCount: 0, maxRetry: 3)
            .Should().Be(RetryAction.PublishToDlq);

        RetryDecision.Decide(FailureMode.Poison, currentRetryCount: 10, maxRetry: 3)
            .Should().Be(RetryAction.PublishToDlq);
    }

    [Fact]
    public void NextRetryCount_increments()
    {
        RetryDecision.NextRetryCount(0).Should().Be(1);
        RetryDecision.NextRetryCount(2).Should().Be(3);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Decide_rejects_invalid_maxRetry(int maxRetry)
    {
        var act = () => RetryDecision.Decide(FailureMode.Transient, 0, maxRetry);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
