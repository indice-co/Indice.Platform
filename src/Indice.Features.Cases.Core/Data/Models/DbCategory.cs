using Indice.Features.Cases.Core.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Core.Data.Models;

#pragma warning disable 1591
public class DbCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? Order { get; set; }
    public TranslationDictionary<CategoryTranslation>? Translations { get; set; }
}
#pragma warning restore 1591

