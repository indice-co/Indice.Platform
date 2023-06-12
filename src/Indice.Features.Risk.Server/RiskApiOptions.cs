using System.ComponentModel.DataAnnotations;
using Indice.Features.Risk.Core.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Risk.Server;

/// <summary>Options for configuring the API for risk engine.</summary>
public class RiskApiOptions
{
    private string _apiPrefix = "/api";
    private string _apiScope = RiskApiEndpoints.DefaultScope;
    private string _authenticationScheme = RiskApiEndpoints.AuthenticationScheme;

    internal IServiceCollection? Services { get; set; }
    internal Type TransactionType { get; set; } = typeof(Transaction);

    /// <summary>Specifies a prefix for the risk API endpoints.</summary>
    public PathString ApiPrefix {
        get => _apiPrefix;
        set { _apiPrefix = string.IsNullOrWhiteSpace(value) ? string.Empty : value; }
    }

    /// <summary>The default scope name to be used for risk API. Defaults to <i>risk</i>.</summary>
    public string ApiScope {
        get => _apiScope;
        set { _apiScope = !string.IsNullOrWhiteSpace(value) ? value : throw new ValidationException("Please specify an API scope for Risk API."); }
    }

    /// <summary>The default scope name to be used for risk API. Defaults to <i>Bearer</i>.</summary>
    public string AuthenticationScheme {
        get => _authenticationScheme;
        set { _authenticationScheme = !string.IsNullOrWhiteSpace(value) ? value : throw new ValidationException("Please specify an authentication scheme for Risk API."); }
    }
}
