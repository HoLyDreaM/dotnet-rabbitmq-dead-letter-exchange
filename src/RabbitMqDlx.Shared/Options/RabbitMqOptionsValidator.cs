using Microsoft.Extensions.Options;

namespace RabbitMqDlx.Shared.Options;

public sealed class RabbitMqOptionsValidator : IValidateOptions<RabbitMqOptions>
{
    public ValidateOptionsResult Validate(string? name, RabbitMqOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.HostName))
        {
            failures.Add($"{nameof(RabbitMqOptions.HostName)} zorunludur.");
        }

        if (options.Port is < 1 or > 65535)
        {
            failures.Add($"{nameof(RabbitMqOptions.Port)} 1-65535 aralığında olmalıdır.");
        }

        if (string.IsNullOrWhiteSpace(options.UserName))
        {
            failures.Add($"{nameof(RabbitMqOptions.UserName)} zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            failures.Add($"{nameof(RabbitMqOptions.Password)} zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(options.VirtualHost))
        {
            failures.Add($"{nameof(RabbitMqOptions.VirtualHost)} zorunludur.");
        }

        if (options.MaxRetry is < 1 or > 20)
        {
            failures.Add($"{nameof(RabbitMqOptions.MaxRetry)} 1-20 aralığında olmalıdır.");
        }

        if (options.RetryTtlMilliseconds is < 100 or > 3_600_000)
        {
            failures.Add($"{nameof(RabbitMqOptions.RetryTtlMilliseconds)} 100-3600000 aralığında olmalıdır.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
