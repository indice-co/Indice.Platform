namespace Indice.Features.Cases.Data.Models;

#pragma warning disable 1591
public class DbCaseType
{
    public Guid Id { get; set; }
    public Guid? CategoryId { get; set; }
    public string Code { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }        
    public string DataSchema { get; set; }
    public string Layout { get; set; }
    public string Translations { get; set; }
    public string LayoutTranslations { get; set; }
    public string Tags { get; set; }
    public string Config { get; set; }
    public int? Order { get; set; }
    /// <summary>The allowed Roles that can create a new Case</summary>
    public string CanCreateRoles { get; set; }
    /// <summary>Available checkpoints for this case type</summary>
    public virtual List<DbCheckpointType> CheckpointTypes { get; set; }
    public virtual DbCategory Category { get; set; }
}
#pragma warning restore 1591
