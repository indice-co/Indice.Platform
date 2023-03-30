using Indice.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.Server.Options;
/// <summary>
/// The <see cref="IdentityServerEndpointOptions"/> is the top level container for all configuration settings of Identity Server.
/// </summary>
public class IdentityServerEndpointOptions
{

    private string _apiPrefix;

    /// <summary>The claim type used to identify the user. Defaults to <i>sub</i>.</summary>
    public string UserClaimType { get; set; } = BasicClaimTypes.Subject;

    /// <summary>Api default resource scope. Defaults to <i>cmp</i>.</summary>
    public string ApiScope { get; set; } = "identity";
    /// <summary>Api default resource scope. Defaults to <i>cmp</i>.</summary>
    public string ApiSubScopeDelimiter { get; set; } = ".";
    /// <summary>Specifies a prefix for the API endpoints.</summary>
    public PathString ApiPrefix {
        get => _apiPrefix;
        set { _apiPrefix = string.IsNullOrWhiteSpace(value) ? "" : value; }
    }
}

