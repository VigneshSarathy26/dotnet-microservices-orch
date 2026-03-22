using System;

namespace Orchestrator.Contracts.Order;

public interface SubmitOrder
{
    Guid OrderId { get; }
    DateTime Timestamp { get; }
    string CustomerNumber { get; }
}
