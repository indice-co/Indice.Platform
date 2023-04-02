using Indice.AspNetCore.Hosting;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.SignInLogs.Hosting;

internal class LogCleanupHostedService : TimedHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SignInLogOptions _signInLogOptions;

    public LogCleanupHostedService(
        ILogger<LogCleanupHostedService> logger,
        IServiceProvider serviceProvider,
        IOptions<SignInLogOptions> signInLogOptions
    ) : base(logger) {
        _signInLogOptions = signInLogOptions?.Value ?? throw new ArgumentNullException(nameof(signInLogOptions));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public override TimeSpan Interval => TimeSpan.FromSeconds(_signInLogOptions.Cleanup.IntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        try {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
                var signInLogStore = serviceScope.ServiceProvider.GetRequiredService<ISignInLogStore>();
                await signInLogStore.Cleanup(stoppingToken);
            }
        } catch (Exception exception) {
            Logger.LogError("Exception while removing expired logs: {Exception}", exception.Message);
        }
    }
}
