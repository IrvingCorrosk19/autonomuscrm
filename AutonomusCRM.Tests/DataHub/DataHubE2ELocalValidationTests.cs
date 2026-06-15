using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Domain.Tenants;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.Tests.DataHub;

[Collection("PostgresWebIntegration")]
[Trait("Category", "Integration")]
[Trait("Category", "DataHubE2E")]
public class DataHubE2ELocalValidationTests
{
    private readonly PostgresWebApplicationFixture _fixture;
    private static readonly string E2eDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ops", "certification", "datahub-e2e"));

    public DataHubE2ELocalValidationTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task E2E_FullLeadImportFlow_Passes()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (client, factory, tenantId, token) = await LoginAsAdminAsync();
        SetAuth(client, token);

        var csvPath = Path.Combine(E2eDir, "leads-valid.csv");
        Skip.If(!File.Exists(csvPath), $"Missing {csvPath}");

        // Step 1 Upload
        var jobId = await UploadCsvAsync(client, tenantId, csvPath, "Lead");
        Assert.NotEqual(Guid.Empty, jobId);

        // Step 2 Analyze
        var analyzeResp = await client.PostAsync($"/api/datahub/jobs/{jobId}/analyze?tenantId={tenantId}", null);
        Assert.Equal(HttpStatusCode.OK, analyzeResp.StatusCode);
        var ai = await analyzeResp.Content.ReadFromJsonAsync<DataHubAiAnalysisResultDto>();
        Assert.NotNull(ai);
        Assert.True(ai!.OverallConfidencePercent >= 50, $"AI confidence too low: {ai.OverallConfidencePercent}");
        Assert.Contains(ai.ColumnDetections, c => c.DetectedType == "Email" && c.ConfidencePercent >= 70);

        // Step 3 Job detail / mapping
        var detail = await GetJobAsync(client, tenantId, jobId);
        Assert.NotEmpty(detail!.DetectedColumns);
        Assert.Contains(detail.Mappings, m => m.TargetField == "Email");

        // Step 4 Auto-fix
        var fixResp = await client.PostAsync($"/api/datahub/jobs/{jobId}/autofix?tenantId={tenantId}", null);
        Assert.Equal(HttpStatusCode.OK, fixResp.StatusCode);

        // Step 5 Validate
        var valResp = await client.PostAsync($"/api/datahub/jobs/{jobId}/validate?tenantId={tenantId}", null);
        Assert.Equal(HttpStatusCode.OK, valResp.StatusCode);

        // Step 6 Cleaning summary
        var cleanResp = await client.GetAsync($"/api/datahub/jobs/{jobId}/cleaning?tenantId={tenantId}");
        Assert.Equal(HttpStatusCode.OK, cleanResp.StatusCode);
        var cleaning = await cleanResp.Content.ReadFromJsonAsync<DataHubCleaningSummaryDto>();
        Assert.NotNull(cleaning);
        Assert.True(cleaning!.TotalRows >= 4);

        // Step 7 Import (upload uses dryRun=false by default; validate sets ReadyToImport)
        var importResp = await client.PostAsync($"/api/datahub/jobs/{jobId}/import?tenantId={tenantId}", null);
        Assert.Equal(HttpStatusCode.OK, importResp.StatusCode);

        // Step 8 Wait for completion
        var completed = await WaitForJobStatusAsync(client, tenantId, jobId, "Completed", "CompletedWithErrors");
        Assert.True(completed, "Job did not complete in time");

        // Step 9 Jobs list / history
        var jobsResp = await client.GetAsync($"/api/datahub/jobs?tenantId={tenantId}");
        Assert.Equal(HttpStatusCode.OK, jobsResp.StatusCode);
        var jobs = await jobsResp.Content.ReadFromJsonAsync<List<DataHubJobSummaryDto>>();
        Assert.Contains(jobs!, j => j.Id == jobId);

        // Step 10 Metrics
        var metricsResp = await client.GetAsync($"/api/datahub/jobs/{jobId}/metrics?tenantId={tenantId}");
        Assert.Equal(HttpStatusCode.OK, metricsResp.StatusCode);

        // Quality score
        var scoreResp = await client.GetAsync($"/api/datahub/quality/score?tenantId={tenantId}");
        Assert.Equal(HttpStatusCode.OK, scoreResp.StatusCode);
    }

    [SkippableFact]
    public async Task E2E_InvalidEmail_DetectedOnValidation()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (client, _, tenantId, token) = await LoginAsAdminAsync();
        SetAuth(client, token);

        var jobId = await UploadCsvAsync(client, tenantId, Path.Combine(E2eDir, "leads-invalid-email.csv"), "Lead");
        await client.PostAsync($"/api/datahub/jobs/{jobId}/analyze?tenantId={tenantId}", null);
        var valResp = await client.PostAsync($"/api/datahub/jobs/{jobId}/validate?tenantId={tenantId}", null);
        Assert.Equal(HttpStatusCode.OK, valResp.StatusCode);
        var result = await valResp.Content.ReadFromJsonAsync<DataHubValidationResultDto>();
        Assert.NotNull(result);
        Assert.True(result!.InvalidRows >= 1 || result.Errors.Any(e => e.ErrorCode == "InvalidEmail"));
    }

    [SkippableFact]
    public async Task E2E_FormulaInjection_SanitizedInStaging()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (client, factory, tenantId, token) = await LoginAsAdminAsync();
        SetAuth(client, token);

        var jobId = await UploadCsvAsync(client, tenantId, Path.Combine(E2eDir, "leads-formula-injection.csv"), "Lead");
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IDataHubRepository>();
        var rows = await repo.GetRowsAsync(tenantId, jobId, 0, 10, CancellationToken.None);
        var formulaRow = rows.FirstOrDefault(r => r.RawData.Values.Any(v => v?.Contains("cmd") == true));
        if (formulaRow != null)
        {
            var emailVal = formulaRow.RawData.Values.FirstOrDefault(v => v?.StartsWith("'=") == true || v?.StartsWith("=") == true);
            Assert.True(emailVal == null || emailVal.StartsWith("'"), "Formula should be sanitized with leading quote");
        }
    }

    [SkippableFact]
    public async Task E2E_ViewerRole_ForbiddenOnDataHub()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        var client = _fixture.Client ?? throw new InvalidOperationException();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await IntegrationTestTenantHelper.ResolveAdminTenantIdAsync(db);

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(
            "viewer@autonomuscrm.local", "Viewer123!", tenantId));
        Skip.If(login.StatusCode != HttpStatusCode.OK, "Viewer user not seeded");
        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())!.AccessToken;
        SetAuth(client, token);

        var resp = await client.GetAsync($"/api/datahub/jobs?tenantId={tenantId}");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [SkippableFact]
    public async Task E2E_CrossTenant_JobNotFound()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (client, factory, tenantId, token) = await LoginAsAdminAsync();
        SetAuth(client, token);

        var jobId = await UploadCsvAsync(client, tenantId, Path.Combine(E2eDir, "leads-valid.csv"), "Lead");
        var otherTenantId = await EnsureSecondTenantAsync(factory, tenantId);

        var resp = await client.GetAsync($"/api/datahub/jobs/{jobId}?tenantId={otherTenantId}");
        Assert.True(resp.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task E2E_CrossTenant_RollbackForbidden()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (client, factory, tenantId, token) = await LoginAsAdminAsync();
        SetAuth(client, token);

        var jobId = await UploadCsvAsync(client, tenantId, Path.Combine(E2eDir, "leads-valid.csv"), "Lead");
        var otherTenantId = await EnsureSecondTenantAsync(factory, tenantId);

        var resp = await client.PostAsync($"/api/datahub/jobs/{jobId}/rollback?tenantId={otherTenantId}", null);
        Assert.True(resp.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task E2E_EicarUpload_Blocked()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (client, _, tenantId, token) = await LoginAsAdminAsync();
        SetAuth(client, token);

        var eicar = Encoding.ASCII.GetBytes(
            "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*");
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(eicar), "file", "eicar.csv");
        var resp = await client.PostAsync(
            $"/api/datahub/upload?tenantId={tenantId}&targetEntity=Lead&loadMode=InsertOnly", content);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    private async Task<(HttpClient Client, CustomWebApplicationFactory Factory, Guid TenantId, string Token)> LoginAsAdminAsync()
        => await IntegrationTestTenantHelper.LoginAdminAsync(
            _fixture.Client ?? throw new InvalidOperationException(),
            _fixture.Factory ?? throw new InvalidOperationException());

    private static void SetAuth(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<Guid> UploadCsvAsync(HttpClient client, Guid tenantId, string path, string entity)
    {
        using var content = new MultipartFormDataContent();
        var bytes = await File.ReadAllBytesAsync(path);
        content.Add(new ByteArrayContent(bytes), "file", Path.GetFileName(path));
        var resp = await client.PostAsync(
            $"/api/datahub/upload?tenantId={tenantId}&targetEntity={entity}&loadMode=InsertOnly", content);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await resp.Content.ReadFromJsonAsync<DataHubUploadResultDto>();
        return result!.JobId;
    }

    private static async Task<DataHubJobDetailDto?> GetJobAsync(HttpClient client, Guid tenantId, Guid jobId)
    {
        var resp = await client.GetAsync($"/api/datahub/jobs/{jobId}?tenantId={tenantId}");
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<DataHubJobDetailDto>();
    }

    private static async Task<bool> WaitForJobStatusAsync(HttpClient client, Guid tenantId, Guid jobId, params string[] statuses)
    {
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (DateTime.UtcNow < deadline)
        {
            var detail = await GetJobAsync(client, tenantId, jobId);
            if (detail != null && statuses.Contains(detail.Summary.Status))
                return true;
            await Task.Delay(2000);
        }
        return false;
    }

    private static async Task<Guid> EnsureSecondTenantAsync(CustomWebApplicationFactory factory, Guid existingTenantId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var other = await db.Tenants.AsNoTracking().Where(t => t.Id != existingTenantId).Select(t => t.Id).FirstOrDefaultAsync();
        if (other != Guid.Empty) return other;

        var tenant = Tenant.Create("E2E Other Tenant", "DataHub isolation test");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id;
    }
}
