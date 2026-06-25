using Microsoft.AspNetCore.Identity;

namespace Microsoft.Extensions.Configuration;

/// <summary>Extensions to configure the <see cref="IConfiguration"/> of an ASP.NET Core application.</summary>
public static class IConfigurationExtensions
{
    /// <summary>Reads application settings for <see cref="IdentityOptions"/> section.</summary>
    /// <typeparam name="T">The type to return.</typeparam>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="section">The sub section to look for.</param>
    /// <param name="key">The key of the configuration section's value to convert.</param>
    public static T? GetIdentityOption<T>(this IConfiguration configuration, string section, string key) =>
        configuration.GetSection($"{nameof(IdentityOptions)}:{section}").GetValue<T>(key) ?? configuration.GetSection(section).GetValue<T>(key);

    /// <summary>Reads application settings for <see cref="IdentityOptions"/> section.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="section">The sub section to look for.</param>
    /// <param name="key">The key of the configuration section's value to convert.</param>
    public static string? GetIdentityOption(this IConfiguration configuration, string section, string key) =>
        configuration.GetIdentityOption<string>(section, key);

    /// <summary>
    ///  Reads application settings for <see cref="IdentityOptions"/> section.
    /// </summary>
    /// <typeparam name="T">The type to return.</typeparam>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="section">The sub section to look for.</param>
    /// <param name="key">The key of the configuration section's value to convert.</param>
    /// <returns>The value of the configuration section's key converted to the specified type.</returns>
public static T? GetIdentitySection<T>(this IConfiguration configuration, string section, string key) {
    var identitySection = configuration.GetSection($"{nameof(IdentityOptions)}:{section}").GetSection(key);
    if (identitySection.Exists()) {
        return identitySection.Get<T>();
    }
    var fallbackSection = configuration.GetSection(section).GetSection(key);
    return fallbackSection.Exists() ? fallbackSection.Get<T>() : default;
}
