using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Domain.Customers;

namespace AutonomusCRM.Infrastructure.Intelligence;

public class CustomerSegmentationEngine : ICustomerSegmentationEngine
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerHealthEngine _healthEngine;
    private readonly IChurnPredictionV2 _churnPrediction;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerSegmentationEngine(
        ICustomerRepository customerRepository,
        ICustomerHealthEngine healthEngine,
        IChurnPredictionV2 churnPrediction,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _healthEngine = healthEngine;
        _churnPrediction = churnPrediction;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CustomerSegmentDto>> SegmentAllAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var customers = (await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();
        var health = (await _healthEngine.CalculateAllAsync(tenantId, cancellationToken))
            .ToDictionary(h => h.CustomerId);
        var list = new List<CustomerSegmentDto>();

        foreach (var c in customers)
        {
            health.TryGetValue(c.Id, out var h);
            var segment = await ResolveSegmentInternalAsync(c, h?.HealthScore ?? 50, h?.Classification, cancellationToken);
            list.Add(new CustomerSegmentDto(c.Id, c.Name, segment, h?.HealthScore ?? 0, c.LifetimeValue));
        }

        return list;
    }

    public async Task<string> ResolveSegmentAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null || customer.TenantId != tenantId)
            return IntelligenceConstants.SegmentStable;

        var health = await _healthEngine.CalculateHealthAsync(tenantId, customerId, cancellationToken);
        return await ResolveSegmentInternalAsync(customer, health.HealthScore, health.Classification, cancellationToken);
    }

    public async Task ApplySegmentsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var segments = await SegmentAllAsync(tenantId, cancellationToken);
        foreach (var seg in segments)
        {
            var customer = await _customerRepository.GetByIdAsync(seg.CustomerId, cancellationToken);
            if (customer == null) continue;
            customer.UpdateMetadata("Segment", seg.Segment);
            if (seg.Segment == IntelligenceConstants.SegmentVip)
                customer.ChangeStatus(CustomerStatus.VIP);
            await _customerRepository.UpdateAsync(customer, cancellationToken);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> ResolveSegmentInternalAsync(
        Customer customer, int healthScore, string? classification, CancellationToken cancellationToken)
    {
        if (customer.Status == CustomerStatus.Churned)
            return IntelligenceConstants.SegmentChurned;

        var predictions = await _churnPrediction.PredictAsync(customer.TenantId, customer.Id, cancellationToken);
        var churnProb = predictions.FirstOrDefault()?.ChurnProbability ?? 0;

        if (customer.Status == CustomerStatus.VIP || (customer.LifetimeValue ?? 0) >= 50_000)
            return IntelligenceConstants.SegmentVip;
        if (classification == CustomerSuccessConstants.HealthCritical || churnProb >= 70)
            return IntelligenceConstants.SegmentAtRisk;
        if (healthScore >= 70 && (customer.LifetimeValue ?? 0) < 50_000)
            return IntelligenceConstants.SegmentGrowth;
        return IntelligenceConstants.SegmentStable;
    }
}
