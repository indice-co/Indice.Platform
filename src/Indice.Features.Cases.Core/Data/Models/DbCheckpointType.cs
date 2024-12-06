using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Core.Data.Models;

#pragma warning disable 1591
public class DbCheckpointType
{
    public Guid Id { get; set; }
    public Guid CaseTypeId { get; set; }
    public string Code { get; set; } = null!;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public TranslationDictionary<CheckpointTypeTranslation>? Translations { get; set; }
    public CaseStatus Status { get; set; }
    public bool Private { get; set; }
    public virtual DbCaseType CaseType { get; set; } = null!;
}
#pragma warning restore 1591
