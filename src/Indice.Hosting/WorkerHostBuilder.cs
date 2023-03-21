using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting;

/// <summary>A helper class to configure the worker host.</summary>
public class WorkerHostBuilder
{
    /// <summary>Creates a new instance of <see cref="WorkerHostBuilder"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public WorkerHostBuilder(IServiceCollection services) : this(services, null) { }

    internal WorkerHostBuilder(IServiceCollection services, WorkerHostOptions options) {
        Services = services;
        Options = options;
    }

    /// <summary>Specifies the contract for a collection of service descriptors.</summary>
    public IServiceCollection Services { get; protected set; }
    /// <summary>Options for configuring the worker host.</summary>
    public WorkerHostOptions Options { get; protected set; }
}

/// <summary>A helper class to configure the worker host. This variation enables the just added queue triggered job to change it's qmessage store.</summary>
public class WorkerHostBuilderForQueue : WorkerHostBuilder
{
    internal WorkerHostBuilderForQueue(IServiceCollection services, WorkerHostOptions options, Type workItemType) : base(services, options) {
        WorkItemType = workItemType;
        Services = services;
        Options = options;
    }

    internal Type WorkItemType { get; }
}
