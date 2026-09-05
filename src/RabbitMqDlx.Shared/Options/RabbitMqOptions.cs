using System.ComponentModel.DataAnnotations;

namespace RabbitMqDlx.Shared.Options;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    [MinLength(1)]
    public string HostName { get; set; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; set; } = 5672;

    [Required]
    [MinLength(1)]
    public string UserName { get; set; } = "guest";

    [Required]
    [MinLength(1)]
    public string Password { get; set; } = "guest";

    [Required]
    [MinLength(1)]
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Geçici (transient) hatalarda uygulama düzeyinde en fazla kaç kez retry kuyruğuna yayınlanacağı.
    /// </summary>
    [Range(1, 20)]
    public int MaxRetry { get; set; } = 3;

    /// <summary>
    /// Retry kuyruğu TTL (ms). Broker tarafında gecikme için kullanılır.
    /// </summary>
    [Range(100, 3_600_000)]
    public int RetryTtlMilliseconds { get; set; } = 5000;
}
