namespace Indice.Features.Agents.Core.Data;

/// <summary>A durable workflow checkpoint blob. One row per committed superstep checkpoint, keyed by run (conversation) id.</summary>
public class DbCheckpoint
{
    /// <summary>The workflow run identifier (the conversation id).</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>The checkpoint identifier assigned when the checkpoint was committed.</summary>
    public string CheckpointId { get; set; } = string.Empty;

    /// <summary>The serialized (JSON) checkpoint payload. Framework-owned shape; stored opaquely.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Optional parent checkpoint id (lineage), when supplied by the runtime.</summary>
    public string? ParentCheckpointId { get; set; }

    /// <summary>Commit timestamp. Used to return the index ordered oldest-first.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
