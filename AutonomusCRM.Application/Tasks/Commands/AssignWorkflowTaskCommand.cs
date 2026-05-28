using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Tasks.Commands;

public record AssignWorkflowTaskCommand(Guid TaskId, Guid TenantId, Guid UserId) : IRequest<bool>;
