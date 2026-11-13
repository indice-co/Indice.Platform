using Indice.Hosting.Services;
using Quartz;

namespace Indice.Hosting.Tasks;

/// <summary>Refreshes the row of the current worker in the server table. See <see cref="WorkerServerRegistry"/>.</summary>
[DisallowConcurrentExecution]
internal class ServerHeartbeatJob : IJob
{
    private readonly WorkerServerRegistry _registry;

    public ServerHeartbeatJob(WorkerServerRegistry registry) {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public async Task Execute(IJobExecutionContext context) {
        try {
            await _registry.Heartbeat(context.CancellationToken);
        } catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested) {
            // The scheduler is shutting down.
        }
    }
}
