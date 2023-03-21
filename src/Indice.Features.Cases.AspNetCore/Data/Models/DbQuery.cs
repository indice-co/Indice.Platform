namespace Indice.Features.Cases.Data.Models;

#pragma warning disable 1591
public class DbQuery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; }
    public string FriendlyName { get; set; }
    public string Parameters { get; set; }
}
#pragma warning restore 1591
