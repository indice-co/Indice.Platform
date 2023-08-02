using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Indice.Features.Messages.Core.Hosting;
/// <summary>Background service for seeding db data.</summary>
public class StartupSeedHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    /// <summary>
    /// Creates a new instance of <see cref="StartupSeedHostedService"/>
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public StartupSeedHostedService(IServiceProvider provider) {
        _serviceProvider = provider ?? throw new ArgumentNullException(nameof(provider));
    }
    /// <summary>
    /// Executes the background service's logic.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        using var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var campaignsDbContext = serviceScope.ServiceProvider.GetRequiredService<CampaignsDbContext>();
        var defaultEmailProviderSelector = serviceScope.ServiceProvider.GetRequiredService<Func<EmailProviderInfo>>();
        var defaultSender = await campaignsDbContext.MessageSenders.FirstOrDefaultAsync(x => x.IsDefault);
        if (defaultSender is not null) {
            return;
        }
        var defaultEmailProviderInfo = defaultEmailProviderSelector();
        campaignsDbContext.MessageSenders.Add(new Data.Models.DbMessageSender {
            Kind = MessageChannelKind.Email,
            DisplayName = defaultEmailProviderInfo.DisplayName,
            Sender = defaultEmailProviderInfo.Sender,
            IsDefault = true
        });
        await campaignsDbContext.SaveChangesAsync();
    }
}
