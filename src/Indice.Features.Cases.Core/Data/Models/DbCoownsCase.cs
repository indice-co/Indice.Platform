using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Core.Data.Models;

/// <summary>
/// Represents a co-ownership association between a case and a co-owner contact.
/// </summary>
/// <remarks>Stores the identifiers and navigation data needed to link a case to a co-owner in the data
/// model.</remarks>
public class DbCoownsCase
{
    /// <summary>
    /// The id of the co-ownership record
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// The id of the case
    /// </summary>
    public Guid CaseId { get; set; }
    /// <summary>
    /// The associated case
    /// </summary>
    public DbCase Case { get; set; } = null!;
    /// <summary>
    /// The co-owner of the case
    /// </summary>
    public CoOwnerContactMeta CoOwner { get; set; } = null!;

}
