using Indice.Features.Agents.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Agents.Server.Endpoints;

internal static class AgentsHandlers
{
    /// <summary>Handles the discovery of available agents.</summary>
    public static Ok<List<AgentInfo>> Discovery() {
        // Implementation for discovering agents goes here.
        return TypedResults.Ok(new List<AgentInfo>() { 
            new AgentInfo(
                Name: AgentsConstants.AgentNames.Auto,
                Description: "This is an agent that discovers user intent and passes it to the appropriate sub agent.",
                InputContentTypes: ["text/plain" ],
                OutputContentTypes: ["text/markdown", AgentsConstants.MediaTypes.MultipleChoice],
                Capabilities: [ new AgentCapability("Master intent classification", "Discovers user intent and routes it to the appropriate sub-agent.") ],
                Domains: [],
                Tags: ["Intent"],
                Links: []),

            new AgentInfo(
                Name: AgentsConstants.AgentNames.Knowledge,
                Description: "This is an agent that can answer questions based on a knowledge base.",
                InputContentTypes: ["text/plain" ],
                OutputContentTypes: ["text/markdown", AgentsConstants.MediaTypes.MultipleChoice],
                Capabilities: [ new AgentCapability("Knowledge retrieval", "Answers questions based on a knowledge base.") ],
                Domains: [],
                Tags: ["Knowledge", "FAQ"],
                Links: [])
        });
    }
}
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
/// This record represents the information about an agent, including its name, description, input and output content types, capabilities, domains, tags, links, author, and metadata.
/// </summary>
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
    object? Metadata = null);