using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Auth.Commands;

public record VerifyMfaCommand(
    string TempToken,
    string MfaCode
) : IRequest<LoginResult>;

