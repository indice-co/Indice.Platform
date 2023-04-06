using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.Server.Options;

/// <summary>Options used to configure the Settings API feature.</summary>
public class SettingsApiOptions
{
    internal IServiceCollection Services { get; set; }
    /// <summary>Specifies a prefix for the API endpoints. Defaults to <i>api</i>.</summary>
    public string ApiPrefix { get; set; } = "api";
    /// <summary>The default scope name to be used for Settings API. Defaults to <i>identity</i>.</summary>
    public string ApiScope { get; set; } = "identity";
}
