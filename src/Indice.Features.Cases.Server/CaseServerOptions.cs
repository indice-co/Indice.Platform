using Indice.AspNetCore.Configuration;
using Indice.Features.Cases.Core;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Cases.Server;

/// <summary>
/// Case server options
/// </summary>
public class CaseServerOptions : CasesOptions
{
    /// <summary>
    /// The path prefix for the endpoints registered
    /// </summary>
    public PathString PathPrefix { get; set; } = "api";
    /// <summary>
    /// The Open API spcification for the set of endpoints registered. Different groups mean different openapi specs.
    /// </summary>
    /// <remarks>defaults to <strong>cases</strong></remarks>
    public string GroupName  { get; set; } = "manager";

    /// <summary>
    /// Configuration for overriding the default <see cref="LimitUploadOptions"/>
    /// </summary>
    public Action<LimitUploadOptions>? ConfigureLimitUpload { get; set; }
}
