﻿namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>The Translation of the checkpoint type.</summary>
public class CheckpointTypeTranslation
{
    /// <summary>The title of the checkpoint type.</summary>
    public string Title { get; set; } = null!;
    /// <summary>The checkpoint type description.</summary>
    public string? Description { get; set; }
}

