namespace Indice.Features.Messages.Core.Data.Models;

/// <summary>Distribution list entity.</summary>
public class DbDistributionList : DbAuditableEntity
{
    /// <summary>The unique id.</summary>
    public Guid Id { get; set; }
    /// <summary>The name of the distribution list.</summary>
    public string Name { get; set; } = null!;
    /// <summary>The alias of distribution list.</summary>
    /// <remarks>Optional, but if set then the value must be Unique</remarks>
    public string? Alias { get; set; }
    /// <summary>Contact - Distribution list join entity type.</summary>
    public List<DbDistributionListContact> ContactDistributionLists { get; set; } = [];
}
