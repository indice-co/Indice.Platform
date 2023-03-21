namespace Indice.Features.Cases.Data.Models;

#pragma warning disable 1591
public class DbCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public string Description { get; set; }
    public int? Order { get; set; }
    public string Translations { get; set; }
}
#pragma warning restore 1591

