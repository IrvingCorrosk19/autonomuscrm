using AutonomusCRM.Application.Common.Localization;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Common.Tenancy;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace AutonomusCRM.Application.Auth.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly ICurrentTenantAccessor _tenantAccessor;

    public LoginCommandHandler(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        ICurrentTenantAccessor tenantAccessor,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
    }

    public async Task<LoginResult> HandleAsync(LoginCommand request, CancellationToken cancellationToken = default)
    {
        var previousBypass = _tenantAccessor.BypassTenantFilter;
        try
        {
            return await HandleLoginCoreAsync(request, cancellationToken);
        }
        finally
        {
            _tenantAccessor.BypassTenantFilter = previousBypass;
        }
    }

    private async Task<LoginResult> HandleLoginCoreAsync(LoginCommand request, CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId;
        Domain.Users.User? user = null;

        if (tenantId != Guid.Empty)
        {
            _tenantAccessor.TenantId = tenantId;
            user = await _userRepository.GetByEmailAsync(tenantId, request.Email, cancellationToken);
        }
        else
        {
            _tenantAccessor.BypassTenantFilter = true;
            var tenants = await _tenantRepository.GetAllAsync(cancellationToken);
            foreach (var tenant in tenants)
            {
                _tenantAccessor.TenantId = tenant.Id;
                user = await _userRepository.GetByEmailAsync(tenant.Id, request.Email, cancellationToken);
                if (user is not null)
                {
                    tenantId = tenant.Id;
                    break;
                }
            }
        }

        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException(LocalizationKeys.Auth_InvalidCredentials);

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException(LocalizationKeys.Auth_InvalidCredentials);

        user.RecordLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (user.MfaEnabled)
        {
            var tempToken = _tokenService.GenerateMfaPendingToken(user);
            return new LoginResult(tempToken, string.Empty, DateTime.UtcNow.AddMinutes(5), true);
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await _refreshTokenService.IssueAsync(user.Id, user.TenantId, cancellationToken);

        return new LoginResult(accessToken, refreshToken, DateTime.UtcNow.AddHours(1), false);
    }
}
