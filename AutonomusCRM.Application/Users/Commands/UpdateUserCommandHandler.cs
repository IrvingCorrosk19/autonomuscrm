using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Users.Commands;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateUserCommandHandler> _logger;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(UpdateUserCommand request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user == null || user.TenantId != request.TenantId)
        {
            _logger.LogWarning("User {UserId} not found or tenant mismatch", request.UserId);
            throw new InvalidOperationException("Usuario no encontrado o no pertenece al tenant");
        }

        // Actualizar información básica
        if (request.FirstName != null || request.LastName != null || request.Email != null)
        {
            user.UpdateInfo(request.FirstName, request.LastName, request.Email);
        }

        // Actualizar estado
        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value && !user.IsActive)
            {
                user.Activate();
            }
            else if (!request.IsActive.Value && user.IsActive)
            {
                user.Deactivate();
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User {UserId} updated successfully", request.UserId);
        
        return true;
    }
}

