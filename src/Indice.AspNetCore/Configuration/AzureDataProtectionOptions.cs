using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Configuration;

/// <summary>Options for configuring ASP.NET Core DataProtection API using Azure Blob Storage infrastructure.</summary>
public class AzureDataProtectionOptions
{
    internal IServiceCollection Services = null!;
    /// <summary>The name of section when saving settings in configuration.</summary>
    public const string Name = "DataProtectionOptions";
    /// <summary>Stops the process of automatically rolling keys (create new keys) as they approach expiration.</summary>
    /// <remarks>https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-3.1#disableautomatickeygeneration</remarks>
    public bool DisableAutomaticKeyGeneration { get; set; }
    /// <summary>The connection string name to your Azure storage account. Defaults to "StorageConnection".</summary>
    public string ConnectionStringName { get; set; } = "StorageConnection";
    /// <summary>
    /// Obsolete raw Azure Storage connection string kept for backward compatibility with callers
    /// that still configure <c>StorageConnectionString</c>. When set, this value should take
    /// precedence over <see cref="ConnectionStringName"/> in the options consumption path.
    /// </summary>
    [Obsolete("Use ConnectionStringName instead.")]
    public string StorageConnectionString { get; set; } = null!;
    /// <summary>The name of the container that will be used within the data protection system.</summary>
    public string ContainerName { get; set; } = null!;
    /// <summary>Sets the unique name of this application within the data protection system.</summary>
    public string ApplicationName { get; set; } = null!;
    /// <summary>Controls the lifetime (in days) of the private key. Defaults to 90 days. It gets rolled automatically, except if option <see cref="DisableAutomaticKeyGeneration"/> is set to true.</summary>
    public int KeyLifetime { get; set; }
    /// <summary>
    /// Configures the data protection system to use the specified cryptographic algorithms by default when generating protected payloads. By default, <see cref="EncryptionAlgorithm.AES_256_GCM"/>
    /// is used for encryption and <see cref="ValidationAlgorithm.HMACSHA512"/> for validation.
    /// </summary>
    public AuthenticatedEncryptorConfiguration CryptographicAlgorithms { get; set; } = new AuthenticatedEncryptorConfiguration {
        EncryptionAlgorithm = EncryptionAlgorithm.AES_256_GCM,
        ValidationAlgorithm = ValidationAlgorithm.HMACSHA512
    };
}
