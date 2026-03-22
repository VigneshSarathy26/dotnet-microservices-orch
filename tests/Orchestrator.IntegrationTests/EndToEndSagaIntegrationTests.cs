using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Orchestrator.Contracts.Order;
using Orchestrator.Infrastructure.Database;
using Orchestrator.StateMachines.Order;

namespace Orchestrator.IntegrationTests;

[TestFixture]
public class EndToEndSagaIntegrationTests
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    [SetUp]
    public async Task Setup()
    {
        var services = new ServiceCollection();
        var dbName = $"SagaE2E_{Guid.NewGuid()}";

        // 1. Add EF Core DbContext with an InMemory provider
        services.AddDbContext<OrderStateDbContext>(options =>
            options.UseInMemoryDatabase(dbName)
                   .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        // 2. Configure MassTransit Test Harness using EF Core Repository
        services.AddMassTransitTestHarness(x =>
        {
            x.AddSagaStateMachine<OrderStateMachine, OrderState>()
             .EntityFrameworkRepository(r =>
             {
                 r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                 r.ExistingDbContext<OrderStateDbContext>();
             });
        });

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetTestHarness();

        await _harness.Start();
    }

    [TearDown]
    public async Task Teardown()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    [Test]
    public async Task Publish_SubmitOrder_RecordsState_InDatabase()
    {
        var orderId = Guid.NewGuid();
        var message = new SubmitOrderMessage
        {
            OrderId = orderId,
            Timestamp = DateTime.UtcNow,
            CustomerNumber = "INTEG-E2E"
        };

        // Act: Publish the initial event to the bus
        await _harness.Bus.Publish<SubmitOrder>(message);

        // Assert: Ensure the MassTransit Harness consumed the event
        Assert.That(await _harness.Consumed.Any<SubmitOrder>(), Is.True);
        
        // Assert: Ensure the Saga logic processed the event
        var sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderState>();
        Assert.That(await sagaHarness.Consumed.Any<SubmitOrder>(), Is.True);
        
        // Assert: Verify that EF Core successfully wrote the state to the DB underlying table
        using var scope = _provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderStateDbContext>();
        
        var savedSaga = await dbContext.Set<OrderState>().FirstOrDefaultAsync(x => x.CorrelationId == orderId);
        
        Assert.That(savedSaga, Is.Not.Null, "Saga was not saved to database.");
        Assert.That(savedSaga!.CurrentState, Is.EqualTo("Submitted"));
        Assert.That(savedSaga.CustomerNumber, Is.EqualTo("INTEG-E2E"));
    }
    
    private class SubmitOrderMessage : SubmitOrder
    {
        public Guid OrderId { get; set; }
        public DateTime Timestamp { get; set; }
        public string CustomerNumber { get; set; } = string.Empty;
    }
}
