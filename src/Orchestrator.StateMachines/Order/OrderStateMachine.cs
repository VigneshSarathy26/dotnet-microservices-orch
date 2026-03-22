using System;
using MassTransit;
using Orchestrator.Contracts.Order;

namespace Orchestrator.StateMachines.Order;

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State Submitted { get; private set; } = null!;

    public Event<SubmitOrder> SubmitOrderEvent { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => SubmitOrderEvent, x => x.CorrelateById(context => context.Message.OrderId));

        Initially(
            When(SubmitOrderEvent)
                .Then(context =>
                {
                    context.Saga.CustomerNumber = context.Message.CustomerNumber;
                    context.Saga.SubmitDate = context.Message.Timestamp;
                    context.Saga.Updated = DateTime.UtcNow;
                })
                .Publish(context => (OrderSubmitted)new OrderSubmittedEvent(context.Saga))
                .TransitionTo(Submitted)
        );
    }
}

public class OrderSubmittedEvent : OrderSubmitted
{
    private readonly OrderState _saga;
    public OrderSubmittedEvent(OrderState saga) => _saga = saga;

    public Guid OrderId => _saga.CorrelationId;
    public DateTime Timestamp => _saga.SubmitDate ?? DateTime.UtcNow;
    public string CustomerNumber => _saga.CustomerNumber ?? string.Empty;
}
