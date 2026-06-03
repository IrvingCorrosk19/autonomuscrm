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
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.Integrations;
using AutonomusCRM.Application.Billing;
using AutonomusCRM.Application.Trust;
using AutonomusCRM.Application.Voice;
using AutonomusCRM.Application.DataPlatform;
using AutonomusCRM.Application.EnterpriseAuth;
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
    public DbSet<ProductUsageEvent> ProductUsageEvents => Set<ProductUsageEvent>();
    public DbSet<CustomerFeedback> CustomerFeedbacks => Set<CustomerFeedback>();
    public DbSet<CustomerAnalyticsSnapshot> CustomerAnalyticsSnapshots => Set<CustomerAnalyticsSnapshot>();
    public DbSet<AiDecisionAudit> AiDecisionAudits => Set<AiDecisionAudit>();
    public DbSet<AutonomousPlaybookState> AutonomousPlaybookStates => Set<AutonomousPlaybookState>();
    public DbSet<BusinessKnowledgeRecord> BusinessKnowledgeRecords => Set<BusinessKnowledgeRecord>();
    public DbSet<MlFeatureSnapshot> MlFeatureSnapshots => Set<MlFeatureSnapshot>();
    public DbSet<MlModelVersion> MlModelVersions => Set<MlModelVersion>();
    public DbSet<MlPipelineRun> MlPipelineRuns => Set<MlPipelineRun>();
    public DbSet<MlDriftReport> MlDriftReports => Set<MlDriftReport>();
    public DbSet<BusinessKnowledgeGraphEdge> BusinessKnowledgeGraphEdges => Set<BusinessKnowledgeGraphEdge>();
    public DbSet<NbaOutcomeRecord> NbaOutcomeRecords => Set<NbaOutcomeRecord>();
    public DbSet<FailedEventMessage> FailedEventMessages => Set<FailedEventMessage>();
    public DbSet<TenantIntegrationConnection> TenantIntegrations => Set<TenantIntegrationConnection>();
    public DbSet<TenantBillingAccount> TenantBillingAccounts => Set<TenantBillingAccount>();
    public DbSet<AiApprovalRequest> AiApprovalRequests => Set<AiApprovalRequest>();
    public DbSet<VoiceCallLog> VoiceCallLogs => Set<VoiceCallLog>();
    public DbSet<CdpStreamEvent> CdpStreamEvents => Set<CdpStreamEvent>();
    public DbSet<ScimGroup> ScimGroups => Set<ScimGroup>();

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

        modelBuilder.Entity<ProductUsageEvent>(entity =>
        {
            entity.ToTable("ProductUsageEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Module).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(30);
            entity.Property(e => e.SessionId).HasMaxLength(64);
            entity.Property(e => e.Industry).HasMaxLength(100);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.RecordedAt });
            entity.HasIndex(e => new { e.TenantId, e.Module });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<CustomerFeedback>(entity =>
        {
            entity.ToTable("CustomerFeedbacks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FeedbackType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Comment).HasMaxLength(4000);
            entity.Property(e => e.Segment).HasMaxLength(50);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.CustomerId });
            entity.HasIndex(e => new { e.TenantId, e.FeedbackType });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<CustomerAnalyticsSnapshot>(entity =>
        {
            entity.ToTable("CustomerAnalyticsSnapshots");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Segment).IsRequired().HasMaxLength(30);
            entity.Property(e => e.RevenueAmount).HasPrecision(18, 2);
            entity.Property(e => e.CsatScore).HasPrecision(5, 2);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.CustomerId, e.SnapshotDate }).IsUnique();
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<AiDecisionAudit>(entity =>
        {
            entity.ToTable("AiDecisionAudits");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DecisionType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.BusinessOutcomeDetail).HasMaxLength(2000);
            entity.Property(e => e.Evidence).HasColumnType("jsonb");
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.CustomerId });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<AutonomousPlaybookState>(entity =>
        {
            entity.ToTable("AutonomousPlaybookStates");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PlaybookType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
            entity.HasIndex(e => new { e.TenantId, e.CustomerId, e.PlaybookType });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<BusinessKnowledgeRecord>(entity =>
        {
            entity.ToTable("BusinessKnowledgeRecords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PatternKey).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Outcome).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.TenantId, e.PatternKey }).IsUnique();
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<MlFeatureSnapshot>(entity =>
        {
            entity.ToTable("MlFeatureSnapshots");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DatasetType).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Features).HasColumnType("jsonb");
            entity.Property(e => e.Label).HasMaxLength(50);
            entity.HasIndex(e => new { e.TenantId, e.DatasetType });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<MlModelVersion>(entity =>
        {
            entity.ToTable("MlModelVersions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ModelType).IsRequired().HasMaxLength(30);
            entity.Property(e => e.VersionTag).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Weights).HasColumnType("jsonb");
            entity.Property(e => e.Metrics).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.TenantId, e.ModelType, e.Status });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<MlPipelineRun>(entity =>
        {
            entity.ToTable("MlPipelineRuns");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DatasetType).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.RunMetrics).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.TenantId, e.DatasetType });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<MlDriftReport>(entity =>
        {
            entity.ToTable("MlDriftReports");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ModelType).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Details).HasColumnType("jsonb");
            entity.HasIndex(e => e.TenantId);
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<BusinessKnowledgeGraphEdge>(entity =>
        {
            entity.ToTable("BusinessKnowledgeGraphEdges");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceType).IsRequired().HasMaxLength(40);
            entity.Property(e => e.TargetType).IsRequired().HasMaxLength(40);
            entity.Property(e => e.RelationType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.TenantId, e.SourceId, e.TargetId });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<NbaOutcomeRecord>(entity =>
        {
            entity.ToTable("NbaOutcomeRecords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(30);
            entity.Property(e => e.RecommendedAction).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Channel).IsRequired().HasMaxLength(30);
            entity.HasIndex(e => e.TenantId);
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

        modelBuilder.Entity<TenantIntegrationConnection>(entity =>
        {
            entity.ToTable("TenantIntegrations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Settings).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.TenantId, e.Provider }).IsUnique();
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<TenantBillingAccount>(entity =>
        {
            entity.ToTable("TenantBillingAccounts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PlanId).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.HasIndex(e => e.TenantId).IsUnique();
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<VoiceCallLog>(entity =>
        {
            entity.ToTable("VoiceCallLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.Direction).HasMaxLength(20);
            entity.Property(e => e.Outcome).HasMaxLength(50);
            entity.Property(e => e.Provider).HasMaxLength(50);
            entity.HasIndex(e => new { e.TenantId, e.StartedAt });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<AiApprovalRequest>(entity =>
        {
            entity.ToTable("AiApprovalRequests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DecisionType).HasMaxLength(100);
            entity.Property(e => e.RecommendedAction).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.HasIndex(e => new { e.TenantId, e.Status });
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

        modelBuilder.Entity<CdpStreamEvent>(entity =>
        {
            entity.ToTable("CdpStreamEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(100);
            entity.Property(e => e.Payload).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.TenantId, e.OccurredAt });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });

        modelBuilder.Entity<ScimGroup>(entity =>
        {
            entity.ToTable("ScimGroups");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.MemberEmails).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.TenantId, e.DisplayName });
            entity.HasQueryFilter(e => BypassFilters || (CurrentTenantId != null && e.TenantId == CurrentTenantId));
        });
    }
}
