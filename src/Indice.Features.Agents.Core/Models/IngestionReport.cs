namespace Indice.Features.Agents.Core.Models;

/// <summary>The endpoint response shape for a single-file ingest.</summary>
public class IngestionReport
{
    /// <summary>Identifier of the persisted document; <c>null</c> when the upload was skipped.</summary>
    public Guid? DocumentId { get; init; }

    /// <summary>The uploaded file name (echoed for client convenience).</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>Number of chunks inserted for this document; 0 when skipped.</summary>
    public int ChunksCreated { get; init; }

    /// <summary>True when the upload matched an existing document hash and no work was performed.</summary>
    public bool Skipped { get; init; }

    /// <summary>Reason for skipping; populated only when <see cref="Skipped"/> is true (e.g. <c>"unchanged"</c>).</summary>
    public string? SkippedReason { get; init; }

    /// <summary>True when an existing document with the same source was deleted before inserting the new one.</summary>
    public bool Replaced { get; init; }
}
