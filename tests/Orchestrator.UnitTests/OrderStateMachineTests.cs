using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Orchestrator.Contracts.Order;
using Orchestrator.StateMachines.Order;

namespace Orchestrator.UnitTests;

[TestFixture]
public class OrderStateMachineTests
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private OrderStateMachine _machine = null!;
    
    [SetUp]
    public async Task Setup()
    {
        var services = new ServiceCollection();
        
        services.AddMassTransitTestHarness(x =>
        {
            x.AddSagaStateMachine<OrderStateMachine, OrderState>()
             .InMemoryRepository();
        });
        
        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetTestHarness();
        
        await _harness.Start();
        _machine = _provider.GetRequiredService<OrderStateMachine>();
    }

    [TearDown]
    public async Task Teardown()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    [Test]
    public async Task Should_transition_to_Submitted_and_publish_event_on_SubmitOrder()
    {
        var orderId = Guid.NewGuid();
        
        var message = new SubmitOrderMessage
        {
            OrderId = orderId,
            Timestamp = DateTime.UtcNow,
            CustomerNumber = "CUST-UNIT-1"
        };

        await _harness.Bus.Publish<SubmitOrder>(message);

        // Ensure the command was consumed by the State Machine
        Assert.That(await _harness.Consumed.Any<SubmitOrder>(), Is.True);
        
        var sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderState>();
        Assert.That(await sagaHarness.Consumed.Any<SubmitOrder>(), Is.True);
        
        // Ensure the saga was created and transitioned to the correct state
        var sagaInstance = sagaHarness.Sagas.Contains(orderId);
        Assert.That(sagaInstance, Is.Not.Null);
        Assert.That(sagaInstance!.CurrentState, Is.EqualTo(_machine.Submitted.Name));
        Assert.That(sagaInstance.CustomerNumber, Is.EqualTo("CUST-UNIT-1"));
        
        // Ensure the OrderSubmitted event was published
        Assert.That(await _harness.Published.Any<OrderSubmitted>(), Is.True);
    }
    
    private class SubmitOrderMessage : SubmitOrder
    {
        public Guid OrderId { get; set; }
        public DateTime Timestamp { get; set; }
        public string CustomerNumber { get; set; } = string.Empty;
    }
}
