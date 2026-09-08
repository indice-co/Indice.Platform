using Microsoft.Agents.AI;

namespace Indice.Features.Agents.Core;

/// <summary>
/// Describes one routable agent in the Agents catalogue: the metadata advertised on the discovery endpoint and offered to the
/// intent router as a handoff target, plus the factory that materialises the underlying <see cref="AIAgent"/> for a chat turn.
/// Register instances with <c>AddAgent</c> (a ready <see cref="AIAgent"/>) or <c>AddAgentWorkflow</c> (a MAF <c>Workflow</c> wrapped as an agent).
/// </summary>
public class AgentDefinition
{
    /// <summary>Unique, URL-safe agent name. It is the value clients send as <c>agentName</c> and the id of the handoff tool the router calls.</summary>
    public required string Name { get; init; }

    /// <summary>What the agent does, in one or two sentences. Shown to users on discovery and to the intent router when it picks a target.</summary>
    public required string Description { get; init; }

    /// <summary>Semantic icon token from <see cref="AgentsConstants.AgentIcons"/>.</summary>
    public string? Icon { get; init; }

    /// <summary>Free-form tags advertised on discovery.</summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>Capabilities advertised on discovery.</summary>
    public List<AgentCapabilityDefinition> Capabilities { get; init; } = [];

    /// <summary>
    /// True for the master intent router itself. Router definitions are advertised on discovery but are never offered as handoff targets.
    /// </summary>
    public bool IsRouter { get; init; }

    /// <summary>
    /// Creates the <see cref="AIAgent"/> that handles a chat turn. Invoked once per request with the request-scoped service provider.
    /// Set by <c>AddAgentWorkflow</c>; required when registering through <c>AddAgent</c>.
    /// </summary>
    public Func<IServiceProvider, AIAgent>? CreateAgent { get; set; }
}

/// <summary>A named capability of an <see cref="AgentDefinition"/>, advertised on the discovery endpoint.</summary>
public class AgentCapabilityDefinition
{
    /// <summary>Short capability name.</summary>
    public required string Name { get; init; }

    /// <summary>One-sentence description of the capability.</summary>
    public required string Description { get; init; }
}
