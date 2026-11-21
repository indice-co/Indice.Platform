using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models;

// Rich agent metadata
/// <summary>
/// This metadata is used to describe the capabilities of an agent, its input and output content types, and other relevant information.
/// </summary>
public record AgentCapability(string Name, string Description);
/// <summary>
/// This record represents a link related to an agent, such as documentation or a website.
/// </summary>
public record AgentLink(string Type, string Url);
/// <summary>
/// This record represents the author of an agent, including their name, email, and URL.
/// </summary>
public record AgentAuthor(string Name, string? Email = null, string? Url = null);

/// <summary>
/// This record represents the information about an agent, including its name, description, input and output content types, capabilities, domains, tags, links, author, metadata and icon.
/// </summary>
/// <param name="Name">The unique name of the agent.</param>
/// <param name="Description">A description of the agent.</param>
/// <param name="InputContentTypes">The list of input content types that the agent can handle.</param>
/// <param name="OutputContentTypes">The list of output content types that the agent can produce.</param>
/// <param name="Capabilities">The list of capabilities that the agent has.</param>
/// <param name="Domains">The list of domains that the agent is associated with.</param>
/// <param name="Tags">The list of tags that are associated with the agent.</param>
/// <param name="Links">The list of links that are related to the agent.</param>
/// <param name="Author">The author of the agent.</param>
/// <param name="Metadata">Additional metadata about the agent.</param>
/// <param name="Icon">A semantic icon token from <see cref="AgentsConstants.AgentIcons"/> — names what the flow is, leaving the glyph itself to the client.</param>
public record AgentInfo(
    string Name,
    string Description,
    List<string> InputContentTypes,
    List<string> OutputContentTypes,
    List<AgentCapability>? Capabilities = null,
    List<string>? Domains = null,
    List<string>? Tags = null,
    List<AgentLink>? Links = null,
    AgentAuthor? Author = null,
    object? Metadata = null,
    string? Icon = null)
{
    /// <summary>
    /// Roles of which the caller needs at least one to discover, select or be routed to this agent. <c>null</c> or empty means
    /// every caller. Guests and unauthenticated callers never qualify for an agent that lists roles. Server-side only: it is not
    /// part of the discovery response.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<string>? RequiredRoles { get; init; }

    /// <summary>
    /// The knowledge categories this agent reads, typically restricted ones (<see cref="AgentsOptions.TaxonomyOptions.RestrictedCategories"/>).
    /// The master router uses them to match a request against the knowledge base, but only for callers who may use the agent.
    /// Server-side only: it is not part of the discovery response.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<string>? KnowledgeCategories { get; init; }
}
