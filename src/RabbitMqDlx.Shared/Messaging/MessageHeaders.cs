using System.Globalization;
using System.Text;

namespace RabbitMqDlx.Shared.Messaging;

public static class MessageHeaders
{
    public const string DemoRetryCount = "x-demo-retry-count";
    public const string DeathReason = "x-demo-death-reason";
    public const string ContentTypeJson = "application/json";

    public static int GetRetryCount(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue(DemoRetryCount, out var raw) || raw is null)
        {
            return 0;
        }

        return raw switch
        {
            int i => i,
            long l => checked((int)l),
            byte b => b,
            short s => s,
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0
        };
    }

    public static Dictionary<string, object?> WithRetryCount(
        IDictionary<string, object?>? existing,
        int retryCount,
        string? deathReason = null)
    {
        var headers = existing is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(existing);

        headers[DemoRetryCount] = retryCount;

        if (!string.IsNullOrWhiteSpace(deathReason))
        {
            headers[DeathReason] = deathReason;
        }

        return headers;
    }
}
