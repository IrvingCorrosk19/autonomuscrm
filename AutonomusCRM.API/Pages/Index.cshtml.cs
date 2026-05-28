using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Leads.Queries;
using AutonomusCRM.Application.Deals.Queries;
using AutonomusCRM.Application.Customers.Queries;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Domain.Deals;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;

namespace AutonomusCRM.API.Pages;

public class IndexModel : PageModel
{
    public DashboardViewModel ViewModel { get; set; } = new();
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IServiceProvider serviceProvider, ILogger<IndexModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            // Obtener tenant por defecto (para desarrollo, en producción vendría del usuario autenticado)
            var tenantId = await GetDefaultTenantIdAsync();
            
            if (tenantId == Guid.Empty)
            {
                ViewModel.HasData = false;
                return;
            }

            ViewModel.HasData = true;
            ViewModel.TenantId = tenantId;

            // Cargar leads
            var leadsHandler = _serviceProvider.GetRequiredService<IRequestHandler<GetLeadsByTenantQuery, IEnumerable<LeadDto>>>();
            var leadsQuery = new GetLeadsByTenantQuery(tenantId);
            var leads = await leadsHandler.HandleAsync(leadsQuery);
            ViewModel.Leads = leads.ToList();

            // Cargar deals
            var dealsHandler = _serviceProvider.GetRequiredService<IRequestHandler<GetDealsByTenantQuery, IEnumerable<DealDto>>>();
            var dealsQuery = new GetDealsByTenantQuery(tenantId);
            var deals = await dealsHandler.HandleAsync(dealsQuery);
            ViewModel.Deals = deals.ToList();

            // Calcular estadísticas
            ViewModel.NewLeadsLast24h = ViewModel.Leads.Count(l => l.CreatedAt >= DateTime.UtcNow.AddHours(-24));
            ViewModel.TotalLeads = ViewModel.Leads.Count;
            ViewModel.TotalDeals = ViewModel.Deals.Count;
            var dealRepo = _serviceProvider.GetRequiredService<IDealRepository>();
            var dealEntities = (await dealRepo.GetByTenantIdAsync(tenantId)).ToList();
            ViewModel.DealsAtRisk = dealEntities.Count(d =>
                d.Status == DealStatus.Open &&
                d.Metadata.TryGetValue("AtRisk", out var ar) && ar?.ToString() == "true");
            if (ViewModel.DealsAtRisk == 0)
                ViewModel.DealsAtRisk = ViewModel.Deals.Count(d => d.Status == DealStatus.Open && (d.Probability ?? 0) < 50);

            var openDeals = ViewModel.Deals.Where(d => d.Status == DealStatus.Open).ToList();
            ViewModel.EstimatedRevenue = openDeals.Sum(d => d.Amount);
            ViewModel.WeightedPipeline = openDeals.Sum(d => d.Amount * (d.Probability ?? 0) / 100m);
            ViewModel.RevenueClosed = ViewModel.Deals
                .Where(d => d.Stage == DealStage.ClosedWon)
                .Sum(d => d.Amount);

            var won = ViewModel.Deals.Count(d => d.Stage == DealStage.ClosedWon);
            var lost = ViewModel.Deals.Count(d => d.Stage == DealStage.ClosedLost);
            ViewModel.WinRate = (won + lost) > 0 ? (won * 100.0 / (won + lost)) : 0;

            var qualifiedLeads = ViewModel.Leads.Count(l => l.Status == LeadStatus.Qualified);
            ViewModel.ConversionRate = ViewModel.TotalLeads > 0
                ? (qualifiedLeads * 100.0 / ViewModel.TotalLeads)
                : 0;

            var taskHandler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Tasks.Queries.GetWorkflowTasksQuery, IEnumerable<AutonomusCRM.Application.Tasks.Queries.WorkflowTaskDto>>>();
            var openTasks = (await taskHandler.HandleAsync(new AutonomusCRM.Application.Tasks.Queries.GetWorkflowTasksQuery(tenantId, "Open"))).ToList();
            ViewModel.OverdueTasks = openTasks.Count(t => t.IsOverdue);

            // Pipeline por etapa
            ViewModel.PipelineProspecting = ViewModel.Deals.Where(d => d.Stage == DealStage.Prospecting).Sum(d => d.Amount);
            ViewModel.PipelineQualification = ViewModel.Deals.Where(d => d.Stage == DealStage.Qualification).Sum(d => d.Amount);
            ViewModel.PipelineProposal = ViewModel.Deals.Where(d => d.Stage == DealStage.Proposal).Sum(d => d.Amount);
            ViewModel.PipelineClosing = ViewModel.Deals.Where(d => d.Stage == DealStage.Negotiation).Sum(d => d.Amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data");
            ViewModel.HasData = false;
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}

public class DashboardViewModel
{
    public bool HasData { get; set; }
    public Guid TenantId { get; set; }
    public List<LeadDto> Leads { get; set; } = new();
    public List<DealDto> Deals { get; set; } = new();
    
    // Estadísticas
    public int NewLeadsLast24h { get; set; }
    public int TotalLeads { get; set; }
    public int TotalDeals { get; set; }
    public int DealsAtRisk { get; set; }
    public decimal EstimatedRevenue { get; set; }
    public decimal WeightedPipeline { get; set; }
    public decimal RevenueClosed { get; set; }
    public double ConversionRate { get; set; }
    public double WinRate { get; set; }
    public int OverdueTasks { get; set; }

    // Pipeline
    public decimal PipelineProspecting { get; set; }
    public decimal PipelineQualification { get; set; }
    public decimal PipelineProposal { get; set; }
    public decimal PipelineClosing { get; set; }
}

