using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Configuration;

/// <summary>Options for configuring ASP.NET Core DataProtection API using local file system.</summary>
public class LocalDataProtectionOptions
{
    internal IServiceCollection Services;
    /// <summary>The name of section when saving settings in configuration.</summary>
    public const string Name = "DataProtectionOptions";
    /// <summary>The path to the file system that will be used within the data protection system.</summary>
    public string Path { get; set; }
    /// <summary>Controls the lifetime (in days) of the private key. Defaults to 90 days. It gets rolled automatically, except if option <see cref="DisableAutomaticKeyGeneration"/> is set to true.</summary>
    public int KeyLifetime { get; set; }
    /// <summary>Stops the process of automatically rolling keys (create new keys) as they approach expiration.</summary>
    /// <remarks>https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-3.1#disableautomatickeygeneration</remarks>
    public bool DisableAutomaticKeyGeneration { get; set; }
    /// <summary>
    /// Configures the data protection system to use the specified cryptographic algorithms by default when generating protected payloads. By default, <see cref="EncryptionAlgorithm.AES_256_GCM"/>
    /// is used for encryption and <see cref="ValidationAlgorithm.HMACSHA512"/> for validation.
    /// </summary>
    public AuthenticatedEncryptorConfiguration CryptographicAlgorithms { get; set; }
    /// <summary>Sets the unique name of this application within the data protection system.</summary>
    public string ApplicationName { get; set; }
}
