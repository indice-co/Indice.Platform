using Indice.Types;

namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>A customer-visible timeline entry.</summary>
public class MyTimelineEntry
{
    /// <summary>The timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>The author of the entry.</summary>
    public AuditMeta CreatedBy { get; set; } = new();
    /// <summary>Whether this entry is a checkpoint.</summary>
    public bool IsCheckpoint => Checkpoint != null;
    /// <summary>The checkpoint, if this is a checkpoint entry.</summary>
    public MyTimelineCheckpoint? Checkpoint { get; set; }
    /// <summary>The comment, if this is a comment entry.</summary>
    public Comment? Comment { get; set; }
}

/// <summary>A checkpoint in the customer timeline.</summary>
public class MyTimelineCheckpoint
{
    /// <summary>The customer-visible checkpoint type.</summary>
    public MyTimelineCheckpointType CheckpointType { get; set; } = null!;
    /// <summary>The completed date.</summary>
    public DateTimeOffset? CompletedDate { get; set; }
    /// <summary>The due date.</summary>
    public DateTimeOffset? DueDate { get; set; }
}

/// <summary>Customer-visible checkpoint type text and translations.</summary>
public class MyTimelineCheckpointType
{
    /// <summary>The localized title.</summary>
    public string? Title { get; set; }
    /// <summary>The localized description.</summary>
    public string? Description { get; set; }
    /// <summary>Available translations.</summary>
    public TranslationDictionary<CheckpointTypeTranslation>? Translations { get; set; }
}
