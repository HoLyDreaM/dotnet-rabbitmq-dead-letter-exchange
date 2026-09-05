namespace RabbitMqDlx.Shared.Topology;

/// <summary>
/// Sabit topoloji isimleri — demo ve makale ile birebir uyumlu.
/// </summary>
public static class TopologyNames
{
    public const string OrdersExchange = "demo.orders";
    public const string RetryExchange = "demo.orders.retry";
    public const string DlxExchange = "demo.orders.dlx";

    public const string MainQueue = "demo.orders.main";
    public const string Retry5sQueue = "demo.orders.retry.5s";
    public const string Dlq = "demo.orders.dlq";

    public const string OrderCreatedRoutingKey = "order.created";
    public const string Retry5sRoutingKey = "retry.5s";
    public const string DeadRoutingKey = "dead";
}
