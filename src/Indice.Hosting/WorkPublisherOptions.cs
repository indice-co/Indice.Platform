using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting;

/// <summary>Options for configuring the work publisher.</summary>
public class WorkPublisherOptions
{
    /// <summary>Creates a new instance of <see cref="WorkerHostOptions"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public WorkPublisherOptions(IServiceCollection services) {
        Services = services;
        JsonOptions = new WorkerJsonOptions();
    }

    internal IServiceCollection Services { get; }
    internal Type QueueStoreType { get; set; }
    /// <summary>Gets the <see cref="JsonSerializerOptions"/> used internally whenever a payload needs to be persisted. </summary>
    public WorkerJsonOptions JsonOptions { get; }
}
