using AutonomusCRM.Application.Common.Interfaces;
using OtpNet;

namespace AutonomusCRM.Application.Users.Commands;

public record EnableMfaCommand(
    Guid UserId,
    Guid TenantId
) : IRequest<EnableMfaResult>;

public record EnableMfaResult(
    string Secret,
    string QrCodeUrl
);

