using Microsoft.EntityFrameworkCore;
using AutonomusCRM.Domain.Tenants;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Users;
using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Application.Policies;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Infrastructure.Persistence.EventStore;
using AutonomusCRM.Application.Common.Tenancy;

namespace AutonomusCRM.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    private readonly ICurrentTenantAccessor _tenantAccessor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentTenantAccessor tenantAccessor) : base(options)
    {
        _tenantAccessor = tenantAccessor;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<Domain.Users.User> Users => Set<Domain.Users.User>();
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<DomainEventRecord> DomainEvents => Set<DomainEventRecord>();
    public DbSet<EventStore.Snapshot> Snapshots => Set<EventStore.Snapshot>();
    public DbSet<TimeSeries.TimeSeriesMetric> TimeSeriesMetrics => Set<TimeSeries.TimeSeriesMetric>();
    public DbSet<WorkflowTask> WorkflowTasks => Set<WorkflowTask>();
    public DbSet<SalesQuota> SalesQuotas => Set<SalesQuota>();
    public DbSet<CustomerContract> CustomerContracts => Set<CustomerContract>();
    public DbSet<CustomerCommunicationLog> CustomerCommunicationLogs => Set<CustomerCommunicationLog>();
    public DbSet<FailedEventMessage> FailedEventMessages => Set<FailedEventMessage>();

    private Guid? CurrentTenantId => _tenantAccessor.TenantId;
    private bool BypassFilters => _tenantAccessor.BypassTenantFilter;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Settings).HasColumnType("jsonb");
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Company).HasMaxLength(200);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Email });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Company).HasMaxLength(200);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Source).IsRequired();
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<Deal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.ExpectedAmount).HasPrecision(18, 2);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Stage).IsRequired();
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.CustomerId });
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.Property(e => e.Version).IsConcurrencyToken();
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<SalesQuota>(entity =>
        {
            entity.ToTable("SalesQuotas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PeriodType).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.UserId, e.PeriodType, e.PeriodStart });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<CustomerContract>(entity =>
        {
            entity.ToTable("CustomerContracts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
            entity.Property(e => e.AnnualValue).HasPrecision(18, 2);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.CustomerId });
            entity.HasIndex(e => new { e.TenantId, e.RenewalDate });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<CustomerCommunicationLog>(entity =>
        {
            entity.ToTable("CustomerCommunicationLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Channel).IsRequired().HasMaxLength(20);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TemplateKey).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Recipient).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.TrackingId).HasMaxLength(64);
            entity.Property(e => e.Variables).HasColumnType("jsonb");
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.CustomerId });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<WorkflowTask>(entity =>
        {
            entity.ToTable("WorkflowTasks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RelatedEntityType).HasMaxLength(100);
            entity.Property(e => e.Priority).HasMaxLength(20);
            entity.Property(e => e.TaskType).HasMaxLength(50);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Roles).HasColumnType("jsonb");
            entity.Property(e => e.Claims).HasColumnType("jsonb");
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Triggers).HasColumnType("jsonb");
            entity.Property(e => e.Conditions).HasColumnType("jsonb");
            entity.Property(e => e.Actions).HasColumnType("jsonb");
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<Policy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Expression).IsRequired().HasMaxLength(2000);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Name });
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<DomainEventRecord>(entity =>
        {
            entity.ToTable("DomainEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EventData).IsRequired().HasColumnType("jsonb");
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.OccurredOn);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.AggregateId);
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<EventStore.Snapshot>(entity =>
        {
            entity.ToTable("Snapshots");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AggregateType).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SnapshotData).IsRequired().HasColumnType("jsonb");
            entity.HasIndex(e => e.AggregateId);
            entity.HasIndex(e => new { e.AggregateId, e.AggregateType });
            entity.HasIndex(e => e.Version);
        });

        modelBuilder.Entity<TimeSeries.TimeSeriesMetric>(entity =>
        {
            entity.ToTable("TimeSeriesMetrics");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MetricName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Tags).HasColumnType("jsonb");
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.MetricName);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.TenantId, e.MetricName, e.Timestamp });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<FailedEventMessage>(entity =>
        {
            entity.ToTable("FailedEventMessages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(200);
            entity.Property(e => e.RoutingKey).HasMaxLength(200);
            entity.Property(e => e.Payload).IsRequired().HasColumnType("jsonb");
            entity.Property(e => e.Error).HasMaxLength(4000);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.MessageId).IsUnique();
            entity.HasIndex(e => e.FailedAt);
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });
    }
}
