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
/// <param name="Icon">
/// A semantic icon token from <see cref="AgentsConstants.AgentIcons"/> — names what the flow is, leaving the glyph itself to the client.
/// Appended last on purpose: positional record parameters are public API, so inserting one mid-list would break existing deconstruction.
/// </param>
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
    string? Icon = null);
