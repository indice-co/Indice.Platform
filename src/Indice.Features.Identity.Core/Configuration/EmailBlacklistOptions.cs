
namespace Indice.Features.Identity.Core.Configuration;

/// <summary>Options for configuring email domain blacklist validation.</summary>
public class EmailBlacklistOptions
{
    /// <summary>The name is used to mark the section found inside a configuration file.</summary>
    public const string Name = "IdentityOptions:Email:Blacklist";
    /// <summary>Indicates whether the domain blacklist is enabled.</summary>
    public bool EnableDomainBlacklist { get; set; } = true;
    /// <summary>Comma separated domains to be blacklisted.</summary>
    public string? Domain { get; set; }

    
}