using AutonomusCRM.API.Controllers;
using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Revenue;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AutonomusCRM.Tests.TruthSprint;

public class RevenueApiConsolidationTests
{
    [Fact]
    public void Revenue_page_model_uses_IRevenueOsService()
    {
        var ctor = typeof(AutonomusCRM.API.Pages.RevenueModel).GetConstructors()[0];
        var param = ctor.GetParameters().First(p => p.ParameterType == typeof(IRevenueOsService));
        Assert.NotNull(param);
    }

    [Fact]
    public async Task Os_dashboard_delegates_to_IRevenueOsService()
    {
        var tenantId = Guid.NewGuid();
        var expected = BuildOsDashboard();

        var osMock = new Mock<IRevenueOsService>();
        osMock.Setup(s => s.GetDashboardAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var controller = BuildController(osMock.Object, null);
        var result = await controller.GetOsDashboard(tenantId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
        osMock.Verify(s => s.GetDashboardAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Legacy_dashboard_delegates_to_executive_service()
    {
        var tenantId = Guid.NewGuid();
        var expected = BuildExecutiveDashboard();

        var execMock = new Mock<IExecutiveSalesDashboardService>();
        execMock.Setup(s => s.GetDashboardAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var controller = BuildController(null, execMock.Object);
        var result = await controller.GetDashboard(tenantId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
        execMock.Verify(s => s.GetDashboardAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static RevenueController BuildController(
        IRevenueOsService? os,
        IExecutiveSalesDashboardService? executive)
    {
        var services = new ServiceCollection();
        if (os != null) services.AddSingleton(os);
        if (executive != null) services.AddSingleton(executive);

        return new RevenueController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() }
            }
        };
    }

    private static RevenueOsDashboardDto BuildOsDashboard() =>
        new(
            new RevenueExecutiveOverviewDto(1, 2, 3, 4, 5, 6, 7, true),
            new RevenueHealthDto(80, 70, 10, 20, 90),
            Array.Empty<OutcomeAttributionRowDto>(),
            Array.Empty<RevenueInsightDto>(),
            new PredictiveRevenueForecastDto(Array.Empty<PredictiveHorizonDto>(), 85),
            Array.Empty<WinLossBreakdownDto>(),
            Array.Empty<WinLossBreakdownDto>(),
            new RevenueKpiSnapshotDto(1, 0.5, 100, 30, 0.8, 120, 0.4, 50, 10, 5),
            true);

    private static ExecutiveSalesDashboardDto BuildExecutiveDashboard() =>
        new(
            new RevenueKpiSnapshotDto(1, 0.5, 100, 30, 0.8, 120, 0.4, 50, 10, 5),
            Array.Empty<RevenueForecastDto>(),
            Array.Empty<RepPerformanceDto>(),
            Array.Empty<PipelineCoverageDto>(),
            Array.Empty<SlaBreachDto>(),
            Array.Empty<WinLossBreakdownDto>(),
            0);
}
