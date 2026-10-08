using Indice.Features.ActivityLogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.Server.ActivityLog;

/// <summary>
/// Options for configuring the activity logs feature in Indice Identity Server.
/// </summary>
public class ActivityLogIdentityOptions
{
    /// <summary>
    /// Constructs the <see cref="ActivityLogIdentityOptions"/> passing the service collection and configuration.
    /// </summary>
    /// <param name="services">The service collection used to register services for the activity logs feature.</param>
    /// <param name="configuration">The configuration used to configure the activity logs feature.</param>
    public ActivityLogIdentityOptions(IServiceCollection services, IConfiguration configuration) {
        Services = services;
        Configuration = configuration;
    }

    /// <summary>
    /// The service collection used to register services for the activity logs feature.
    /// </summary>
    public IServiceCollection Services { get; set; }

    /// <summary>
    /// The configuration used to configure the activity logs feature.
    /// </summary>
    public IConfiguration Configuration { get; set; }

    /// <summary>
    /// The option used to configure whether events without subject should be discarded.
    /// </summary>
    public bool EnableSubjectFilter { get; set; } = false;

    /// <summary>
    /// Additional options for configuring the activity logs feature.
    /// </summary>
    public Action<ActivityLogOptions>? Configure { get; set; } = null;

}
