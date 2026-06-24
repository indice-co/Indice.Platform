
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
    public string GroupName { get; set; } = "agents";
    /// <summary>Chat endpoints security requirement.</summary>
    public string ChatRequiredScope { get; set; } = "chat";
    /// <summary>Ingest endpoints security requirement.</summary>
    public string IngestRequiredScope { get; set; } = "ingest";
    /// <summary>
    /// Configure Agents core options 
    /// </summary>
    public Action<AgentsOptions>? ConfigureAgents { get; set; }
}
