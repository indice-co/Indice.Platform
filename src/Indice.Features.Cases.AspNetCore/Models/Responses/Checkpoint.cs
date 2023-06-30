namespace Indice.Features.Cases.Models.Responses;

/// <summary>The checkpoint entry for a case.</summary>
public class Checkpoint
{
    /// <summary>The Id of the checkpoint.</summary>
    public Guid Id { get; set; }
    
    /// <summary>The checkpoint type. This is the inner status the back-officer can see.</summary>
    public CheckpointType CheckpointType { get; set; }
    
    /// <summary>The completed date of the checkpoint.</summary>
    public DateTimeOffset? CompletedDate { get; set; }

    /// <summary>The due date of the checkpoint.</summary>
    public DateTimeOffset? DueDate { get; set; }
}