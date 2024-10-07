using Indice.Security;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Server.Options;

/// <summary>Indice Identity Server endpoints configuration options.</summary>
public class ExtendedEndpointOptions
{
    private string _apiPrefix = "/api";
    
    /// <summary>Configuration section name</summary>
    public const string Name = "IdentityServer:Endpoints";

    /// <summary>Disables the cache for all the endpoints in the IdentityServer API. Defaults to false.</summary>
    public bool DisableCache { get; set; } = false;
    /// <summary>The claim type used to identify the user. Defaults to <strong>sub</strong>.</summary>
    public string UserClaimType { get; set; } = BasicClaimTypes.Subject;
    /// <summary>API default resource scope. Defaults to <strong>identity</strong>.</summary>
    public string? ApiScope { get; set; } = IdentityEndpoints.Scope;
    /// <summary>API sub-scope delimiter. Defaults to <strong>.</strong> dot.</summary>
    public string ApiSubScopeDelimiter { get; set; } = ".";
    /// <summary>Specifies a prefix for the API endpoints.</summary>
    public PathString ApiPrefix {
        get => _apiPrefix;
        set { _apiPrefix = string.IsNullOrWhiteSpace(value) ? string.Empty : value; }
    }
    /// <summary>Options for the SMS sent when a user updates his phone number.</summary>
    public PhoneNumberOptions PhoneNumber { get; set; } = new PhoneNumberOptions();
    /// <summary>Options for the email sent when a user updates his email address.</summary>
    public EmailOptions Email { get; set; } = new EmailOptions();

    /// <summary>Options for the avatar feature. By default the feature is disabled.</summary>
    public AvatarOptions AvatarOptions { get; set; } = new AvatarOptions();
    
}
