namespace Indice.Features.Cases.Core.Data.Models;

#pragma warning disable 1591
public class DbQuery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = null!;
    public string FriendlyName { get; set; } = null!;
    public string Parameters { get; set; } = null!;
}
#pragma warning restore 1591
