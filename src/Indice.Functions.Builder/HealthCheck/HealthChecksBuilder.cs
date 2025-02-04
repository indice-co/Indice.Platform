using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Indice.Functions.Builder.HealthCheck;

internal class HealthChecksBuilder : IHealthChecksBuilder
{
    public HealthChecksBuilder(IServiceCollection services) {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public IHealthChecksBuilder Add(HealthCheckRegistration registration) {
        ArgumentNullException.ThrowIfNull(registration);

        Services.Configure<HealthCheckServiceOptions>(options => {
            options.Registrations.Add(registration);
        });

        return this;
    }
}
