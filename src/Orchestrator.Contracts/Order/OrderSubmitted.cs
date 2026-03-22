using System;

namespace Orchestrator.Contracts.Order;

public interface OrderSubmitted
{
    Guid OrderId { get; }
    DateTime Timestamp { get; }
    string CustomerNumber { get; }
}
