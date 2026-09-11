namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// The outcome of master intent routing for an <c>auto</c> request: either the name of the agent that
/// should handle the request, or an out-of-scope decision when no registered agent fits.
/// </summary>
public class RouteDecision
{
    /// <summary>The name (and DI key) of the chosen agent workflow, or <c>null</c> when the request is out of scope.</summary>
    public string? AgentName { get; set; }

    /// <summary>A short, polite explanation shown to the user when the request is out of scope; otherwise <c>null</c>.</summary>
    public string? Reason { get; set; }

    /// <summary>Whether the request is in scope for any registered agent.</summary>
    public bool IsInScope { get; set; }
}
