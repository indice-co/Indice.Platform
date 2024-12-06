namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>A class that represents a timeline entry for a case.</summary>
public class TimelineEntry
{
    /// <summary>The timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>User that created the entry.</summary>
    public AuditMeta CreatedBy { get; set; } = new AuditMeta();
    
    /// <summary>Checks if the entry is Checkpoint change.</summary>
    public bool IsCheckpoint => Checkpoint != null;
    
    /// <summary>The checkpoint entry. Can be null.</summary>
    public Checkpoint? Checkpoint { get; set; }

    /// <summary>The comment entry. Can be null.</summary>
    public Comment? Comment { get; set; }
}