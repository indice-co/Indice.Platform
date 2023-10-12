using Indice.Features.Identity.Core;

namespace Microsoft.Extensions.Configuration;

/// <summary>Extensions to configure the <see cref="IConfiguration"/> of an ASP.NET Core application.</summary>
public static class IConfigurationExtensions
{
    /// <summary>Reads application settings for <b>IdentityServer:Features:SignInLogs</b> key.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static bool? GetSignInLogsEnabled(this IConfiguration configuration) =>
        configuration.GetValue<bool?>($"{IdentityServerFeatures.Section}:{nameof(IdentityServerFeatures.SignInLogs)}");

    /// <summary>Reads application settings for <b>IdentityServer:Features:ImpossibleTravel</b> key.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static bool? GetImpossibleTravelEnabled(this IConfiguration configuration) =>
        configuration.GetValue<bool?>($"{IdentityServerFeatures.Section}:{nameof(IdentityServerFeatures.ImpossibleTravel)}");
}