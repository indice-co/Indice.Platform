using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.Security;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Server.Options;

/// <summary>
/// Endpoint parameters
/// </summary>
public class ExtendedEndpointOptions
{
    private string _apiPrefix = "/api";
    /// <summary>Configuration section name</summary>
    public const string Name = "IdentityServer:Endpoints";

    /// <summary>Disables the cache for all the endpoints in the IdentityServer API. Defaults to false.</summary>
    public bool DisableCache { get; set; } = false;

    /// <summary>The claim type used to identify the user. Defaults to <strong>sub</strong>.</summary>
    public string UserClaimType { get; set; } = BasicClaimTypes.Subject;

    /// <summary>Api default resource scope. Defaults to <strong>identity</strong>.</summary>
    public string ApiScope { get; set; } = IdentityEndpoints.Scope;
    /// <summary>Api subscope delimiter. Defaults to <strong>.</strong> dot.</summary>
    public string ApiSubScopeDelimiter { get; set; } = ".";
    /// <summary>Specifies a prefix for the API endpoints.</summary>
    public PathString ApiPrefix {
        get => _apiPrefix;
        set { _apiPrefix = string.IsNullOrWhiteSpace(value) ? "" : value; }
    }
}
