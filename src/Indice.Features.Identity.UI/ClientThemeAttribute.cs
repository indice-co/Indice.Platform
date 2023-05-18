using System.Collections.ObjectModel;

namespace Indice.Features.Identity.UI;

/// <summary>Used to specify a page override for a specific client id.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class ClientThemeAttribute : Attribute
{
    /// <summary>Creates a new instance of <see cref="ClientThemeAttribute"/>.</summary>
    /// <param name="clientId">The client id.</param>
    /// <param name="otherClientIds">Extra client ids to include.</param>
    public ClientThemeAttribute(string clientId, params string[]? otherClientIds) {
        otherClientIds ??= Array.Empty<string>();
        ClientIds = new ReadOnlyCollection<string>(otherClientIds.Concat(new string[] { clientId }).Distinct().ToList());
    }

    /// <summary>The client id.</summary>
    public ReadOnlyCollection<string> ClientIds { get; }
}
