using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Tasks.Commands;

public class AssignWorkflowTaskCommandHandler : IRequestHandler<AssignWorkflowTaskCommand, bool>
{
    private readonly IWorkflowTaskRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignWorkflowTaskCommandHandler(
        IWorkflowTaskRepository repository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> HandleAsync(AssignWorkflowTaskCommand request, CancellationToken cancellationToken = default)
    {
        var task = await _repository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task == null || task.TenantId != request.TenantId)
            return false;

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.TenantId != request.TenantId)
            return false;

        task.AssignTo(request.UserId);
        await _repository.UpdateAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
