using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Server.Models;

public class CasesActor
{
    /// <summary>The Id of the user.</summary>
    public string? Id { get; set; }

    /// <summary>The name of the user.</summary>
    public string? Name { get; set; }

    /// <summary>The email of the user.</summary>
    public string? Email { get; set; }

    public AuditMeta ToAuditMeta() =>
        new() {
            Id = Id,
            Name = Name,
            Email = Email
        };
}