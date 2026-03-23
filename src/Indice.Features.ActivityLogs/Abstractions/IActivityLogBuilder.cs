using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.ActivityLogs;

/// <summary>
/// Defines a builder for configuring the activity log feature.
/// </summary>
public interface IActivityLogBuilder
{
    /// <summary>
    /// Gets the collection of services for the activity log feature.
    /// </summary>
    IServiceCollection Services { get; }
    /// <summary>
    /// Gets the configuration for the activity log feature.
    /// </summary>
    IConfiguration Configuration { get; }
}
