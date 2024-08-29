namespace Indice.Features.Cases.Data.Models;

#pragma warning disable 1591
public class DbCaseMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public string MemberId { get; set; }
    public byte Type { get; set; }
    public int Accesslevel { get; set; } = 0;
    public DateTimeOffset DateInserted { get; set; } = DateTimeOffset.Now;
    public virtual DbCase Case { get; set; }
}
#pragma warning restore 1591
