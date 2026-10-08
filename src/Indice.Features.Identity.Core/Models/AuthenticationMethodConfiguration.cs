namespace Indice.Features.Identity.Core.Models;

/// <summary>Configuration for an authentication method.</summary>
public class AuthenticationMethodConfiguration
{
    /// <summary>The type of authentication method.</summary>
    public required Type MethodType { get; init; }

    /// <summary>Determines whether this authentication method participates in the MFA step.</summary>
    public bool SupportsMfa { get; init; } = true;

    /// <summary>Determines whether this authentication method is enabled.</summary>
    public bool Enabled { get; init; } = true;
}

/// <summary>Pairs an <see cref="AuthenticationMethod"/> with its <see cref="AuthenticationMethodConfiguration"/>.</summary>
public sealed record AuthenticationMethodEntry(
    AuthenticationMethod Method,
    AuthenticationMethodConfiguration Configuration)
{
    /// <summary>Whether this method participates in the MFA step.</summary>
    public bool SupportsMfa => Configuration.SupportsMfa;

    /// <summary>Whether this method is enabled.</summary>
    public bool Enabled => Configuration.Enabled;
}