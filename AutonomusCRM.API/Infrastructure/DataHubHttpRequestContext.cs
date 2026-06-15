using AutonomusCRM.Application.DataHub;

namespace AutonomusCRM.API.Infrastructure;

public sealed class DataHubHttpRequestContext : IDataHubRequestContext
{
    public DataHubHttpRequestContext(IHttpContextAccessor accessor)
    {
        var ctx = accessor.HttpContext;
        ClientIp = ctx?.Connection.RemoteIpAddress?.ToString();
        var ua = ctx?.Request.Headers.UserAgent.ToString();
        UserAgent = string.IsNullOrEmpty(ua) ? null : ua;
    }

    public string? ClientIp { get; }
    public string? UserAgent { get; }
}
