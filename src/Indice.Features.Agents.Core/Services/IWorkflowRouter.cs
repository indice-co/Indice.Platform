using Indice.Features.Agents.Core.Models;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Services;

/// <summary>Classifies an initial user message and decides which workflow should run.</summary>
public interface IWorkflowRouter
{
    /// <summary>Routes the provided first-turn message to a workflow decision.</summary>
    Task<WorkflowRoutingDecision> RouteAsync(ChatMessage message, CancellationToken cancellationToken = default);
}
