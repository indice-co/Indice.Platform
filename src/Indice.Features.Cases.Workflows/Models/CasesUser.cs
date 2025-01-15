namespace Indice.Features.Cases.Workflows.Models;

/// <summary>Related to the user principal that performed an action.</summary>
public record CasesUser
{
    /// <summary>The Id of the user.</summary>
    public string? Id { get; set; }

    /// <summary>The name of the user.</summary>
    public string? Name { get; set; }

    /// <summary>The email of the user.</summary>
    public string? Email { get; set; }

    /// <summary>The timestamp the action happened.</summary>
    public DateTimeOffset? When { get; set; } = DateTimeOffset.Now;
}