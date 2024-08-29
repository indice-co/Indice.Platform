namespace Indice.Features.Cases.Models.Responses;
/// <summary>The CaseMember dto.</summary>
public class CaseMember
{
    /// <summary>The Id of the record.</summary>
    public Guid Id { get; set; }
    /// <summary>The Id of the case.</summary>
    public Guid CaseId { get; set; }
    /// <summary>The Id of the persona.</summary>
    public string MemberId { get; set; }
    /// <summary>The type  of the persona.</summary>
    public byte Type { get; set; }
    /// <summary>The access level given.</summary>
    public int Accesslevel { get; set; }
    /// <summary>When the record was created.</summary>
    public DateTimeOffset DateInserted { get; set; }
}