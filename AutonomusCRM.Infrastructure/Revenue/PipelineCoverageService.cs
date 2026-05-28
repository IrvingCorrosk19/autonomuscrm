using AutonomusCRM.Application.Revenue;

namespace AutonomusCRM.Infrastructure.Revenue;

public class PipelineCoverageService : IPipelineCoverageService
{
    private readonly ISalesPerformanceEngine _performance;

    public PipelineCoverageService(ISalesPerformanceEngine performance)
    {
        _performance = performance;
    }

    public async Task<IReadOnlyList<PipelineCoverageDto>> GetCoverageAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var leaderboard = await _performance.GetLeaderboardAsync(tenantId, cancellationToken);
        var teamPipeline = leaderboard.Sum(r => r.OpenPipelineWeighted);
        var teamQuota = leaderboard.Sum(r => r.QuotaTarget);

        var list = leaderboard.Select(r => new PipelineCoverageDto(
            r.UserId,
            r.Email,
            r.OpenPipelineWeighted,
            r.QuotaTarget,
            r.PipelineCoveragePercent,
            r.PipelineCoveragePercent >= 300 || r.QuotaTarget == 0)).ToList();

        list.Insert(0, new PipelineCoverageDto(
            null,
            "Equipo",
            teamPipeline,
            teamQuota,
            teamQuota > 0 ? (double)(teamPipeline / teamQuota * 100) : 0,
            teamQuota > 0 && teamPipeline >= teamQuota * 3));

        return list;
    }
}
