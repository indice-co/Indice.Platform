using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting.Tasks;

/// <summary>A helper class to configure the way that a job is triggered.</summary>
public class TaskTriggerBuilder
{
    /// <summary>Creates a new instance of <see cref="TaskTriggerBuilder"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public TaskTriggerBuilder(IServiceCollection services) : this(services, null, null) { }

    internal TaskTriggerBuilder(IServiceCollection services, WorkerHostOptions? options, Type? jobHandlerType) {
        Services = services;
        JobHandlerType = jobHandlerType;
        Options = options;
    }

    internal IServiceCollection Services { get; }
    internal Type? JobHandlerType { get; }
    internal WorkerHostOptions? Options { get; }
}
