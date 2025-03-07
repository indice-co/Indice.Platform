namespace Indice.Hosting;

/// <summary>Options for configuring the worker host lifetime options.</summary>
public class WorkerLifetimeOptions
{
    /// <summary>If set to true, the worker host will wait for all jobs to finish before shutting down. Defaults to false.</summary>
    public bool WaitJobsToCompleteOnShutdown { get; set; }
}