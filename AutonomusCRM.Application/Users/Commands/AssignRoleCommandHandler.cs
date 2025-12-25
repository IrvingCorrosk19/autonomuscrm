using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Users;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Users.Commands;

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<AssignRoleCommandHandler> _logger;

    public AssignRoleCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher,
        ILogger<AssignRoleCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(AssignRoleCommand request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user == null || user.TenantId != request.TenantId)
        {
            _logger.LogWarning("User {UserId} not found or tenant mismatch", request.UserId);
            return false;
        }

        user.AddRole(request.Role);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _eventDispatcher.DispatchAsync(user.DomainEvents, cancellationToken);
        user.ClearDomainEvents();

        _logger.LogInformation("Role {Role} assigned to user {UserId}", request.Role, request.UserId);
        return true;
    }
}

