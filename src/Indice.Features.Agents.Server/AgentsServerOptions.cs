
using Indice.Features.Agents.Core;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Agents.Server;

/// <summary>
/// Agents server options
/// </summary>
public class AgentsServerOptions 
{
    /// <summary>The path prefix for the endpoints registered</summary>
    public PathString PathPrefix { get; set; } = "/";
    /// <summary>Endpoints group name</summary>
    /// <remarks>Default value is "agents".</remarks>
    public string GroupName { get; set; } = "agents";
    /// <summary>Chat endpoints security requirement.</summary>
    /// <remarks>Default value is "chat".</remarks>
    public string ChatRequiredScope { get; set; } = "chat";
    /// <summary>Ingest endpoints security requirement.</summary>
    /// <remarks>Default value is "ingest".</remarks>
    public string IngestRequiredScope { get; set; } = "ingest";
    /// <summary>
    /// Configure Agents core options 
    /// </summary>
    public Action<AgentsOptions>? ConfigureAgents { get; set; }
}
