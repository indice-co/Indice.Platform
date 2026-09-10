using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Registry of the agents available for routing and discovery. It is populated from the
/// <see cref="AgentInfo"/> metadata registered in DI alongside each agent workflow, so a newly registered
/// agent becomes routable and discoverable without this type having to change.
/// </summary>
public class AgentInfoRegistry
{
    private readonly IReadOnlyList<AgentInfo> _agents;

    /// <summary>Creates a new <see cref="AgentInfoRegistry"/>.</summary>
    /// <param name="agents">The agent metadata registered in DI — one <see cref="AgentInfo"/> per registered agent.</param>
    public AgentInfoRegistry(IEnumerable<AgentInfo> agents) {
        ArgumentNullException.ThrowIfNull(agents);
        _agents = agents.ToList();
    }

    /// <summary>All registered agents, including the meta <c>auto</c> router entry. Used by the discovery endpoint.</summary>
    public IReadOnlyList<AgentInfo> All() => _agents;

    /// <summary>
    /// The agents the master router may hand a request to: every registered agent except the meta
    /// <c>auto</c> router itself, which must never route to itself.
    /// </summary>
    public IReadOnlyList<AgentInfo> RoutableTargets() =>
        _agents.Where(agent => !string.Equals(agent.Name, AgentsConstants.AgentNames.Auto, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>Finds a registered agent by name (case-insensitive), or <c>null</c> when none matches.</summary>
    /// <param name="name">The agent name to look up.</param>
    public AgentInfo? Find(string name) =>
        _agents.FirstOrDefault(agent => string.Equals(agent.Name, name, StringComparison.OrdinalIgnoreCase));
}
