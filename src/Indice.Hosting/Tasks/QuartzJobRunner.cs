using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Spi;

namespace Indice.Hosting.Tasks;

/// <summary>
/// An "intermediary" <see cref="IJob"/> implementation, <see cref="QuartzJobRunner"/>, that sits between the <seealso cref="IJobFactory"/> and the <seealso cref="IJob "/> you want to run. 
/// It takes care of instantiating your real jobs while managing scope.
/// </summary>
internal class QuartzJobRunner : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QuartzJobRunner> _logger;

    /// <summary>Constructs the <see cref="QuartzJobRunner"/>.</summary>
    /// <param name="serviceProvider"></param>
    /// <param name="logger"></param>
    public QuartzJobRunner(IServiceProvider serviceProvider, ILogger<QuartzJobRunner> logger) {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context) {
        try {
            using (var scope = _serviceProvider.CreateScope()) {
                var jobType = context.JobDetail.JobType;
                var job = (IJob)scope.ServiceProvider.GetRequiredService(jobType);
                await job.Execute(context);
            }
        } catch (Exception exception) {
            _logger.LogError(exception, "An unhandled exception occurred while executing job '{Name}'.", context.JobDetail.JobType.Name);
            throw;
        }
    }
}
