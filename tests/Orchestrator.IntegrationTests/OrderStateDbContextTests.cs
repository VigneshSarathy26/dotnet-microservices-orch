using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Orchestrator.Infrastructure.Database;
using Orchestrator.StateMachines.Order;

namespace Orchestrator.IntegrationTests;

[TestFixture]
public class OrderStateDbContextTests
{
    private DbContextOptions<OrderStateDbContext> _options = null!;

    [SetUp]
    public void Setup()
    {
        _options = new DbContextOptionsBuilder<OrderStateDbContext>()
            .UseInMemoryDatabase(databaseName: $"SagaTestDb_{Guid.NewGuid()}") // Unique DB per test
            .Options;
    }

    [Test]
    public async Task Can_save_and_retrieve_OrderState_From_Database()
    {
        var orderId = Guid.NewGuid();
        var state = new OrderState
        {
            CorrelationId = orderId,
            CurrentState = "Submitted",
            CustomerNumber = "INTEG-123",
            Updated = DateTime.UtcNow
        };

        // Save state
        using (var context = new OrderStateDbContext(_options))
        {
            context.Set<OrderState>().Add(state);
            await context.SaveChangesAsync();
        }

        // Retrieve and verify state
        using (var context = new OrderStateDbContext(_options))
        {
            var savedState = await context.Set<OrderState>().FirstOrDefaultAsync(x => x.CorrelationId == orderId);
            
            Assert.That(savedState, Is.Not.Null);
            Assert.That(savedState!.CorrelationId, Is.EqualTo(orderId));
            Assert.That(savedState.CustomerNumber, Is.EqualTo("INTEG-123"));
            Assert.That(savedState.CurrentState, Is.EqualTo("Submitted"));
            Assert.That(savedState.Updated, Is.Not.Null);
        }
    }
}
