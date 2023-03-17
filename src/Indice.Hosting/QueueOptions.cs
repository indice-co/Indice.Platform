using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting;

/// <summary>Configuration options for the queue.</summary>
public class QueueOptions
{
    /// <summary>Creates a new instance of <see cref="WorkerHostBuilder"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public QueueOptions(IServiceCollection services) => Services = services;

    internal IServiceCollection Services { get; }
    /// <summary>The name of the queue. If not specified a random GUID is assigned.</summary>
    public string QueueName { get; set; } = Guid.NewGuid().ToString();
    /// <summary>Specifies the time interval between two attempts to dequeue new items. Defaults to 300 milliseconds.</summary>
    public double PollingInterval { get; set; } = 300;
    /// <summary>Specifies the maximum time interval between two attempts to dequeue new items. Used as a back-off strategy threshold. Defaults to 5000 milliseconds.</summary>
    public double MaxPollingInterval { get; set; } = 5000;
    /// <summary>Specifies the time interval between two attempts to cleanup items. Defaults to 0 seconds. Zero meaning off/not enabled.</summary>
    public int CleanUpInterval { get; set; } = 0;
    /// <summary>Specifies the cleanup batch size.</summary>
    public int CleanUpBatchSize { get; set; } = 1000;
    /// <summary>Specifies number of concurrent instances. Defaults to one.</summary>
    public int InstanceCount { get; set; } = 1;
}
