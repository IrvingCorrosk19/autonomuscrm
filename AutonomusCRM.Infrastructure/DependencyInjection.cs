using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Infrastructure.Events;
using AutonomusCRM.Infrastructure.Events.EventBus;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Infrastructure.Persistence.EventStore;
using AutonomusCRM.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositories
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<IDealRepository, DealRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Event Bus
        services.AddSingleton<IEventBus, InMemoryEventBus>();

        // Event Store
        services.AddScoped<EventStore>();

        // Event Dispatcher
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Automation
        services.AddScoped<Application.Automation.Workflows.IWorkflowEngine, Infrastructure.Automation.WorkflowEngine>();

        // Decision Engine
        services.AddScoped<Application.DecisionEngine.IDecisionEngine, Infrastructure.DecisionEngine.DecisionEngine>();

        // Policy Engine
        services.AddScoped<Application.Policies.IPolicyEngine, Infrastructure.Policies.PolicyEngine>();

        // Cache (Redis)
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
            });
            services.AddScoped<Caching.ICacheService, Caching.RedisCacheService>();
        }
        else
        {
            // Fallback a cache en memoria si Redis no está configurado
            services.AddMemoryCache();
            services.AddScoped<Caching.ICacheService, Caching.RedisCacheService>();
        }

        // Event Bus (RabbitMQ o InMemory)
        var rabbitMQOptions = configuration.GetSection("RabbitMQ").Get<Events.EventBus.RabbitMQOptions>();
        if (rabbitMQOptions != null && !string.IsNullOrEmpty(rabbitMQOptions.HostName))
        {
            services.Configure<Events.EventBus.RabbitMQOptions>(configuration.GetSection("RabbitMQ"));
            services.AddSingleton<Events.EventBus.IEventBus, Events.EventBus.RabbitMQEventBus>();
        }
        else
        {
            // Fallback a InMemoryEventBus si RabbitMQ no está configurado
            services.AddSingleton<Events.EventBus.IEventBus, Events.EventBus.InMemoryEventBus>();
        }

        // Metrics
        services.AddSingleton<Metrics.IMetricsService, Metrics.MetricsService>();

        // Time Series
        services.AddScoped<Persistence.TimeSeries.ITimeSeriesRepository, Persistence.TimeSeries.TimeSeriesRepository>();

        // Snapshot Store
        services.AddScoped<Persistence.EventStore.ISnapshotStore, Persistence.EventStore.SnapshotStore>();

        // Event Sourcing Service
        services.AddScoped<Application.Events.EventSourcing.IEventSourcingService, Application.Events.EventSourcing.EventSourcingService>();

        // Multi-Region Service
        services.AddScoped<Application.MultiRegion.IRegionService, Application.MultiRegion.RegionService>();

        // Health Checks
        services.AddHealthChecks()
            .AddAutonomusHealthChecks();

        return services;
    }
}

