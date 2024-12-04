﻿namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>The response payload when creating a case.</summary>
public class CreateCaseResponse
{
    /// <summary>The Id of the case that created.</summary>
    public Guid Id { get; set; }

    /// <summary>The created date of the case that created.</summary>
    public DateTimeOffset Created { get; set; }
}