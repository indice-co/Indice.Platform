using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Core.Data.Models;

#pragma warning disable 1591
public class DbCheckpointType
{
    public Guid Id { get; set; }
    public Guid CaseTypeId { get; set; }
    public string Code { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Translations { get; set; }
    public CaseStatus Status { get; set; }
    public bool Private { get; set; }
    public virtual DbCaseType CaseType { get; set; } = null!;
}
#pragma warning restore 1591
