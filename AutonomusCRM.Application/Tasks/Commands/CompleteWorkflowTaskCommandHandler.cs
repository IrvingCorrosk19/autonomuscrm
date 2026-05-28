using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Tasks.Commands;

public class CompleteWorkflowTaskCommandHandler : IRequestHandler<CompleteWorkflowTaskCommand, bool>
{
    private readonly IWorkflowTaskRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteWorkflowTaskCommandHandler(IWorkflowTaskRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> HandleAsync(CompleteWorkflowTaskCommand request, CancellationToken cancellationToken = default)
    {
        var task = await _repository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task == null || task.TenantId != request.TenantId || task.Status == "Completed")
            return false;

        task.Complete();
        await _repository.UpdateAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
