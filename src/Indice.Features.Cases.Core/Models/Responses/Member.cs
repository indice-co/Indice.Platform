namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>The role case type model.</summary>
internal class Member
{
 
    /// <summary>The Id of the member.</summary>
    public Guid Id { get; set; }

    /// <summary>The user role that relates with this member.</summary>
    public string? RoleName { get; set; }

    /// <summary>The Id of the case type that relates with this member.</summary>
    public Guid CaseTypeId { get; set; }

    /// <summary>The Id of the case type that relates with this member.</summary>
    public Guid CheckpointTypeId { get; set; }

    /// <summary>The <see cref="CaseTypePartial"/> model.</summary>
    public CaseTypePartial? CaseTypePartial { get; set; }

    /// <summary>The <see cref="CheckpointType"/> model.</summary>
    public CheckpointType? CheckpointType { get; set; }
}
