using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Users.Commands;

public class BulkUpdateUserStatusCommandHandler : IRequestHandler<BulkUpdateUserStatusCommand, int>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BulkUpdateUserStatusCommandHandler> _logger;

    public BulkUpdateUserStatusCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<BulkUpdateUserStatusCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> HandleAsync(BulkUpdateUserStatusCommand request, CancellationToken cancellationToken = default)
    {
        var updatedCount = 0;
        
        foreach (var userId in request.UserIds)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                
                if (user != null && user.TenantId == request.TenantId)
                {
                    if (request.IsActive && !user.IsActive)
                    {
                        user.Activate();
                        updatedCount++;
                    }
                    else if (!request.IsActive && user.IsActive)
                    {
                        user.Deactivate();
                        updatedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating user {UserId} in bulk operation", userId);
            }
        }
        
        if (updatedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        
        _logger.LogInformation("Bulk updated {Count} users to active={IsActive}", updatedCount, request.IsActive);
        return updatedCount;
    }
}

