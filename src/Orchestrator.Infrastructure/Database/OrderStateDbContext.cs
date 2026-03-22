using System.Collections.Generic;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orchestrator.StateMachines.Order;

namespace Orchestrator.Infrastructure.Database;

public class OrderStateDbContext : SagaDbContext
{
    public OrderStateDbContext(DbContextOptions<OrderStateDbContext> options)
        : base(options)
    {
    }

    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new OrderStateMap(); }
    }
}

public class OrderStateMap : SagaClassMap<OrderState>
{
    protected override void Configure(EntityTypeBuilder<OrderState> entity, ModelBuilder model)
    {
        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.SubmitDate);
        entity.Property(x => x.CustomerNumber).HasMaxLength(256);
        entity.Property(x => x.Updated);
    }
}
