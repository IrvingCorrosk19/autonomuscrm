using Microsoft.EntityFrameworkCore;
using AutonomusCRM.Domain.Tenants;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Users;
using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Application.Policies;
using AutonomusCRM.Infrastructure.Persistence.EventStore;

namespace AutonomusCRM.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<Domain.Users.User> Users => Set<Domain.Users.User>();
    public DbSet<Application.Automation.Workflows.Workflow> Workflows => Set<Application.Automation.Workflows.Workflow>();
    public DbSet<Application.Policies.Policy> Policies => Set<Application.Policies.Policy>();
    public DbSet<DomainEventRecord> DomainEvents => Set<DomainEventRecord>();
    public DbSet<EventStore.Snapshot> Snapshots => Set<EventStore.Snapshot>();
    public DbSet<TimeSeries.TimeSeriesMetric> TimeSeriesMetrics => Set<TimeSeries.TimeSeriesMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de Tenant
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Settings).HasColumnType("jsonb");
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configuración de Customer
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
        });

        // Configuración de Lead
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
        });

        // Configuración de Deal
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
        });

        // Configuración de User
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
        });

        // Configuración de Workflow
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
        });

        // Configuración de Policy
        modelBuilder.Entity<Policy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Expression).IsRequired().HasMaxLength(2000);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Name });
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
        });

        // Configuración de Event Store
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
        });

        // Configuración de Snapshots
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

        // Configuración de Time Series
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
        });
    }
}

