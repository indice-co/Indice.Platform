namespace Indice.Features.Identity.UI;

/// <summary>Used to specify a page override for a specific client id.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class ClientThemeAttribute : Attribute
{
    /// <summary>Creates a new instance of <see cref="ClientThemeAttribute"/>.</summary>
    /// <param name="clientId">The client id.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ClientThemeAttribute(string clientId) {
        ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
    }

    /// <summary>The client id.</summary>
    public string ClientId { get; }
}
