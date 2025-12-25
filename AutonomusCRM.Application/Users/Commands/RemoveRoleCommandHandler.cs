using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Users;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Users.Commands;

public class RemoveRoleCommandHandler : IRequestHandler<RemoveRoleCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<RemoveRoleCommandHandler> _logger;

    public RemoveRoleCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher,
        ILogger<RemoveRoleCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(RemoveRoleCommand request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user == null || user.TenantId != request.TenantId)
        {
            _logger.LogWarning("User {UserId} not found or tenant mismatch", request.UserId);
            return false;
        }

        user.RemoveRole(request.Role);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _eventDispatcher.DispatchAsync(user.DomainEvents, cancellationToken);
        user.ClearDomainEvents();

        _logger.LogInformation("Role {Role} removed from user {UserId}", request.Role, request.UserId);
        return true;
    }
}

