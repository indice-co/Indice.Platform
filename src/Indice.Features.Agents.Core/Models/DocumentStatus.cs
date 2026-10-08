namespace Indice.Features.Agents.Core.Models;

/// <summary>Lifecycle status of an ingested document.</summary>
public enum DocumentStatus
{
    /// <summary>Awaiting chunking/embedding.</summary>
    Pending = 0,

    /// <summary>Successfully chunked and embedded.</summary>
    Ingested = 1,

    /// <summary>Processing failed; see logs for details.</summary>
    Failed = 2,
}
