using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Events.EventSourcing.Queries;
using AutonomusCRM.Domain.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;

namespace AutonomusCRM.API.Pages;

public class AuditModel : PageModel
{
    public List<IDomainEvent> Events { get; set; } = new();
    public int TotalEventsCount { get; set; }
    public int TodayEventsCount { get; set; }
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public Guid TenantId { get; set; }
    public string? FilterEventType { get; set; }
    public DateTime? FilterFrom { get; set; }
    public DateTime? FilterTo { get; set; }
    public List<SelectListItem> EventTypes { get; set; } = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditModel> _logger;

    public AuditModel(IServiceProvider serviceProvider, ILogger<AuditModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task OnGetAsync(string? eventType = null, DateTime? from = null, DateTime? to = null)
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();
            FilterEventType = eventType;
            FilterFrom = from;
            FilterTo = to;

            var query = new GetAuditEventsQuery(TenantId, eventType, from, to, null, 0, 1000);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<GetAuditEventsQuery, IEnumerable<IDomainEvent>>>();
            Events = (await handler.HandleAsync(query, CancellationToken.None)).ToList();

            var eventStore = _serviceProvider.GetRequiredService<Application.Events.EventSourcing.IEventStore>();
            TotalEventsCount = await eventStore.CountByTenantAsync(TenantId, CancellationToken.None);
            var utcToday = DateTime.UtcNow.Date;
            var todayEvents = await eventStore.GetEventsByTenantAsync(TenantId, utcToday, utcToday.AddDays(1), CancellationToken.None);
            TodayEventsCount = todayEvents.Count;
            EventsByType = Events
                .GroupBy(e => e.EventType)
                .ToDictionary(g => g.Key, g => g.Count());

            var allEvents = await eventStore.GetEventsByTenantAsync(TenantId, null, null, CancellationToken.None);
            EventTypes = allEvents.Select(e => e.EventType).Distinct().OrderBy(e => e)
                .Select(e => new SelectListItem { Text = e, Value = e, Selected = e == eventType })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading audit events");
        }
    }

    public async Task<IActionResult> OnPostExportAsync()
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var query = new GetAuditEventsQuery(tenantId, FilterEventType, FilterFrom, FilterTo, null, 0, 10000);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<GetAuditEventsQuery, IEnumerable<IDomainEvent>>>();
            var events = await handler.HandleAsync(query, CancellationToken.None);

            var json = System.Text.Json.JsonSerializer.Serialize(events.Select(e => new
            {
                e.Id,
                e.EventType,
                e.TenantId,
                e.OccurredOn,
                e.CorrelationId
            }), new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"audit-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit events");
            return RedirectToPage("/Audit");
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}
