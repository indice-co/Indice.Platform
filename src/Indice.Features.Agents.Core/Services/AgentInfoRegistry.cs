using System.Security.Claims;
using Indice.Features.Agents.Core.Models;
using Indice.Security;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Registry of the agents available for routing and discovery. It is populated from the
/// <see cref="AgentInfo"/> metadata registered in DI alongside each agent workflow, so a newly registered
/// agent becomes routable and discoverable without this type having to change.
/// </summary>
/// <remarks>
/// Agents that declare <see cref="AgentInfo.RequiredRoles"/> are only visible to callers holding one of those roles. Every
/// overload that takes a <see cref="ClaimsPrincipal"/> applies that rule; the overloads without one are unfiltered and meant
/// for startup code and tests, not for anything that serves a caller.
/// </remarks>
public class AgentInfoRegistry
{
    private readonly IReadOnlyList<AgentInfo> _agents;

    /// <summary>Creates a new <see cref="AgentInfoRegistry"/>.</summary>
    /// <param name="agents">The agent metadata registered in DI — one <see cref="AgentInfo"/> per registered agent.</param>
    public AgentInfoRegistry(IEnumerable<AgentInfo> agents) {
        ArgumentNullException.ThrowIfNull(agents);
        _agents = agents.ToList();
    }

    /// <summary>All registered agents, including the meta <c>auto</c> router entry, regardless of who is asking.</summary>
    public IReadOnlyList<AgentInfo> All() => _agents;

    /// <summary>The registered agents <paramref name="user"/> may see, including the meta <c>auto</c> router entry. Used by the discovery endpoint.</summary>
    /// <param name="user">The caller, or <c>null</c> for an unauthenticated one.</param>
    public IReadOnlyList<AgentInfo> All(ClaimsPrincipal? user) => _agents.Where(agent => IsAllowed(agent, user)).ToList();

    /// <summary>
    /// The agents the master router may hand a request to: every registered agent except the meta
    /// <c>auto</c> router itself, which must never route to itself. Not filtered by caller.
    /// </summary>
    public IReadOnlyList<AgentInfo> RoutableTargets() =>
        _agents.Where(agent => !string.Equals(agent.Name, AgentsConstants.AgentNames.Auto, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>The routable agents <paramref name="user"/> may use.</summary>
    /// <param name="user">The caller, or <c>null</c> for an unauthenticated one.</param>
    public IReadOnlyList<AgentInfo> RoutableTargets(ClaimsPrincipal? user) =>
        RoutableTargets().Where(agent => IsAllowed(agent, user)).ToList();

    /// <summary>Finds a registered agent by name (case-insensitive), or <c>null</c> when none matches. Not filtered by caller.</summary>
    /// <param name="name">The agent name to look up.</param>
    public AgentInfo? Find(string name) =>
        _agents.FirstOrDefault(agent => string.Equals(agent.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Finds a registered agent by name that <paramref name="user"/> may use. An agent the caller is not allowed to use is
    /// reported exactly like one that does not exist, so a name cannot be probed.
    /// </summary>
    /// <param name="name">The agent name to look up.</param>
    /// <param name="user">The caller, or <c>null</c> for an unauthenticated one.</param>
    public AgentInfo? Find(string name, ClaimsPrincipal? user) =>
        Find(name) is { } agent && IsAllowed(agent, user) ? agent : null;

    /// <summary>
    /// Whether <paramref name="user"/> may use <paramref name="agent"/>. An agent with no <see cref="AgentInfo.RequiredRoles"/>
    /// is open to everyone; otherwise the caller must be authenticated and hold at least one of the roles. There is no
    /// administrator bypass: a role-restricted agent is restricted for administrators too.
    /// </summary>
    /// <param name="agent">The agent.</param>
    /// <param name="user">The caller, or <c>null</c> for an unauthenticated one.</param>
    public bool IsAllowed(AgentInfo agent, ClaimsPrincipal? user) {
        ArgumentNullException.ThrowIfNull(agent);
        if (agent.RequiredRoles is not { Count: > 0 } roles) {
            return true;
        }
        if (user?.Identity?.IsAuthenticated != true) {
            return false;
        }
        return roles.Any(role => !string.IsNullOrWhiteSpace(role) && (user.IsInRole(role) || user.HasRoleClaim(role)));
    }
}
