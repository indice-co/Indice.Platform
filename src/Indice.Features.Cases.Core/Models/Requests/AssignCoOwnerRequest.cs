
namespace Indice.Features.Cases.Core.Models.Requests;

/// <summary>
/// The request to assign a co-owner to a case.
/// </summary>
public class AssignCoOwnerRequest
{
    /// <summary>
    /// The co-owner to assign to the case. The <see cref="ContactMeta.Permission"/> defaults to <see cref="ContactPermission.Reader"/>.
    /// </summary>
    public ContactMeta CoOwner { get; set; } = null!;
}
