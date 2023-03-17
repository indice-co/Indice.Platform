using Microsoft.Extensions.DependencyInjection;

namespace Indice.Configuration;

/// <summary>Options for configuring ASP.NET Core DataProtection API using Azure Blob Storage infrastructure.</summary>
public class AzureDataProtectionOptions
{
    internal IServiceCollection Services;
    /// <summary>The name of section when saving settings in configuration.</summary>
    public const string Name = "DataProtectionOptions";
    /// <summary>Stops the process of automatically rolling keys (create new keys) as they approach expiration.</summary>
    /// <remarks>https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-3.1#disableautomatickeygeneration</remarks>
    public bool DisableAutomaticKeyGeneration { get; set; }
    /// <summary>The connection string to your Azure storage account.</summary>
    public string StorageConnectionString { get; set; }
    /// <summary>The name of the container that will be used within the data protection system.</summary>
    public string ContainerName { get; set; }
    /// <summary>Sets the unique name of this application within the data protection system.</summary>
    public string ApplicationName { get; set; }
    /// <summary>Controls the lifetime (in days) of the private key. Defaults to 90 days. It gets rolled automatically, except if option <see cref="DisableAutomaticKeyGeneration"/> is set to true.</summary>
    public int KeyLifetime { get; set; }
}
