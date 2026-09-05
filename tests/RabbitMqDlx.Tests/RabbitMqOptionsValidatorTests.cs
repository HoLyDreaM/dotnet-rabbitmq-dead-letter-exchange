using FluentAssertions;
using RabbitMqDlx.Shared.Options;

namespace RabbitMqDlx.Tests;

public class RabbitMqOptionsValidatorTests
{
    private readonly RabbitMqOptionsValidator _validator = new();

    [Fact]
    public void Valid_options_succeed()
    {
        var options = CreateValid();
        var result = _validator.Validate(null, options);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Empty_host_fails()
    {
        var options = CreateValid();
        options.HostName = " ";
        var result = _validator.Validate(null, options);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains(nameof(RabbitMqOptions.HostName)));
    }

    [Fact]
    public void Invalid_port_fails()
    {
        var options = CreateValid();
        options.Port = 0;
        var result = _validator.Validate(null, options);
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Invalid_maxRetry_fails()
    {
        var options = CreateValid();
        options.MaxRetry = 0;
        var result = _validator.Validate(null, options);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains(nameof(RabbitMqOptions.MaxRetry)));
    }

    [Fact]
    public void Invalid_ttl_fails()
    {
        var options = CreateValid();
        options.RetryTtlMilliseconds = 50;
        var result = _validator.Validate(null, options);
        result.Failed.Should().BeTrue();
    }

    private static RabbitMqOptions CreateValid() => new()
    {
        HostName = "localhost",
        Port = 5672,
        UserName = "guest",
        Password = "guest",
        VirtualHost = "/",
        MaxRetry = 3,
        RetryTtlMilliseconds = 5000
    };
}
