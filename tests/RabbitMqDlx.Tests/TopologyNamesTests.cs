using FluentAssertions;
using RabbitMqDlx.Shared.Topology;

namespace RabbitMqDlx.Tests;

public class TopologyNamesTests
{
    [Fact]
    public void Exchange_and_queue_names_match_contract()
    {
        TopologyNames.OrdersExchange.Should().Be("demo.orders");
        TopologyNames.RetryExchange.Should().Be("demo.orders.retry");
        TopologyNames.DlxExchange.Should().Be("demo.orders.dlx");

        TopologyNames.MainQueue.Should().Be("demo.orders.main");
        TopologyNames.Retry5sQueue.Should().Be("demo.orders.retry.5s");
        TopologyNames.Dlq.Should().Be("demo.orders.dlq");
    }

    [Fact]
    public void Routing_keys_match_contract()
    {
        TopologyNames.OrderCreatedRoutingKey.Should().Be("order.created");
        TopologyNames.Retry5sRoutingKey.Should().Be("retry.5s");
        TopologyNames.DeadRoutingKey.Should().Be("dead");
    }
}
