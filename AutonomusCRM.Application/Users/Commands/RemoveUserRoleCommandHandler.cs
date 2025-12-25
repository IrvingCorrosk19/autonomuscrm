using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Users.Commands;

public class RemoveUserRoleCommandHandler : IRequestHandler<RemoveUserRoleCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveUserRoleCommandHandler> _logger;

    public RemoveUserRoleCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<RemoveUserRoleCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(RemoveUserRoleCommand request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user == null || user.TenantId != request.TenantId)
        {
            _logger.LogWarning("User {UserId} not found or tenant mismatch", request.UserId);
            throw new InvalidOperationException("Usuario no encontrado o no pertenece al tenant");
        }

        user.RemoveRole(request.Role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Role {Role} removed from user {UserId}", request.Role, request.UserId);
        return true;
    }
}

