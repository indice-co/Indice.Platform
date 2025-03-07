namespace Indice.Features.Cases.Core.Models;

/// <summary>The checkpoint type request model.</summary>
public class CheckpointTypeDetails
{
    /// <summary>The Id of the checkpoint type.</summary>
    public Guid Id { get; set; }

    /// <summary>The name of the checkpoint.</summary>
    public string Code { get; set; } = null!;

    /// <summary>The description of the checkpoint.</summary>
    public string? Description { get; set; }

    /// <summary>The public status of the checkpoint.</summary>
    public CaseStatus Status { get; set; }

    /// <summary>Boolean for whether the checkpoint is private.</summary>
    public bool Private { get; set; }

    /// <summary>The related roles for this checkpoint.</summary>
    public List<string>? Roles { get; set; }
}
