using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting;

/// <summary>Options for configuring the worker host.</summary>
public class WorkerHostOptions
{
    /// <summary>Creates a new instance of <see cref="WorkerHostOptions"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    internal WorkerHostOptions(IServiceCollection services) {
        Services = services;
        JsonOptions = new WorkerJsonOptions();
    }

    internal IServiceCollection Services { get; }
    internal Type? QueueStoreType { get; set; }
    internal Type? ScheduledTaskStoreType { get; set; }
    internal Type? LockStoreType { get; set; }

    /// <summary>Gets the <see cref="JsonSerializerOptions"/> used internally whenever a payload needs to be persisted. </summary>
    public WorkerJsonOptions JsonOptions { get; }
    /// <summary>If set to true, the worker host will wait for all jobs to finish before shutting down. Defaults to false.</summary>
    public bool WaitJobsToCompleteOnShutdown { get; set; }
    /// <summary>
    /// How often the worker checks for "run now" requests of its scheduled tasks (e.g. from the workers dashboard). Defaults to 15 seconds.
    /// Only supported by the relational store. Set to <see langword="null"/> or <see cref="TimeSpan.Zero"/> to disable.
    /// </summary>
    public TimeSpan? RunNowPollingInterval { get; set; } = TimeSpan.FromSeconds(15);
    /// <summary>Run now requests older than this are discarded instead of executed, e.g. when no worker was running at the time. Defaults to 10 minutes.</summary>
    public TimeSpan RunNowExpiration { get; set; } = TimeSpan.FromMinutes(10);
}
