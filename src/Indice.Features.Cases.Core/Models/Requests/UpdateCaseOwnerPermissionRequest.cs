namespace Indice.Features.Cases.Core.Models.Requests;

/// <summary>Request to change the permission of the owner of a case.</summary>
public class UpdateCaseOwnerPermissionRequest
{
    /// <summary>The new permission of the owner.</summary>
    public ContactPermission Permission { get; set; }
}
