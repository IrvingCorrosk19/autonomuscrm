using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Tasks.Commands;

public record CompleteWorkflowTaskCommand(Guid TaskId, Guid TenantId) : IRequest<bool>;
