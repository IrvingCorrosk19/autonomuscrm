using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Users;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Users.Commands;

public class ToggleUserStatusCommandHandler : IRequestHandler<ToggleUserStatusCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<ToggleUserStatusCommandHandler> _logger;

    public ToggleUserStatusCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher,
        ILogger<ToggleUserStatusCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(ToggleUserStatusCommand request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user == null || user.TenantId != request.TenantId)
        {
            _logger.LogWarning("User {UserId} not found or tenant mismatch", request.UserId);
            return false;
        }

        if (request.IsActive)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate();
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _eventDispatcher.DispatchAsync(user.DomainEvents, cancellationToken);
        user.ClearDomainEvents();

        _logger.LogInformation("User {UserId} status changed to {IsActive}", request.UserId, request.IsActive);
        return true;
    }
}

