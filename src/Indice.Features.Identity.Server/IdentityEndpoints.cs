using Indice.Features.Identity.Core;

namespace Indice.Features.Identity.Server;

/// <summary>Constants for MFA API feature.</summary>
public static partial class IdentityEndpoints
{
    /// <summary>The base name of the resx resources (under the <strong>Resources</strong> folder) with the IAM UI translations.</summary>
    public const string TranslationsBaseName = "AdminUI.Ui.TranslationApi";

    /// <summary>MFA API sub-scopes.</summary>
    public static partial class SubScopes { }

    /// <summary>Identity MFA policies.</summary>
    public static partial class Policies
    {
        /// <summary>User must be authenticated with <see cref="CustomGrantTypes.DeviceAuthentication"/> grant type.</summary>
        public const string BeDeviceAuthenticated = "BeDeviceAuthenticated";
    }

    /// <summary>Feature flags for MFA API.</summary>
    public static partial class Features { }
}
