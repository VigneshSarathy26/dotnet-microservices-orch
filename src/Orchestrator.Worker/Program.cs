using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orchestrator.Infrastructure.Database;
using Orchestrator.StateMachines.Order;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<OrderStateDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SagaDatabase") 
        ?? "Server=localhost,1433;Database=OrderSagaDb;User Id=sa;Password=YourS!cretP@ssw0rd;Encrypt=True;TrustServerCertificate=True;";
    
    // Switch to actual SQL Server relational persistence
    options.UseSqlServer(connectionString);
});

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddSagaStateMachine<OrderStateMachine, OrderState>()
     .EntityFrameworkRepository(r =>
     {
         r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
         r.ExistingDbContext<OrderStateDbContext>();
     });

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqHost = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        cfg.Host(rabbitMqHost, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
