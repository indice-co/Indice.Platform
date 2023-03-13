using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting;

/// <summary>A helper class to configure the work publisher.</summary>
public class WorkPublisherBuilder
{
    internal WorkPublisherBuilder(IServiceCollection services, WorkPublisherOptions options) {
        Services = services;
        Options = options;
    }

    /// <summary>Specifies the contract for a collection of service descriptors.</summary>
    internal IServiceCollection Services { get; }
    /// <summary>Options for configuring the work publisher.</summary>
    internal WorkPublisherOptions Options { get; }
}
