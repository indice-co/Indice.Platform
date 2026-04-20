namespace Indice.Features.Identity.Core.Models;

/// <summary>Delegate for creating authentication method instances.</summary>
/// <param name="displayName">The localized display name.</param>
/// <param name="description">The localized description.</param>
/// <param name="supportsMfa">Whether this method supports MFA.</param>
/// <param name="enabled">Whether this method is enabled.</param>
/// <returns>The created authentication method instance.</returns>
public delegate AuthenticationMethod AuthenticationMethodFactoryDelegate(
    string displayName, 
    string description, 
    bool supportsMfa, 
    bool enabled);

/// <summary>Configuration for an authentication method.</summary>
public class AuthenticationMethodConfiguration
{
    /// <summary>The type of authentication method.</summary>
    public required Type MethodType { get; init; }

    /// <summary>Determines whether this authentication method participates in the MFA step.</summary>
    public bool SupportsMfa { get; init; } = true;

    /// <summary>Determines whether this authentication method is enabled.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Optional custom display name key (overrides default).</summary>
    public string? DisplayNameKey { get; init; }

    /// <summary>Optional custom description key (overrides default).</summary>
    public string? DescriptionKey { get; init; }

    /// <summary>
    /// Optional factory delegate for creating custom authentication method instances.
    /// When provided, this factory is used instead of the built-in creation logic.
    /// </summary>
    public AuthenticationMethodFactoryDelegate? Factory { get; init; }
}
