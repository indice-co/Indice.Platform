using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Models;
using Indice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Indice.Features.Messages.Core.Hosting;

/// <summary>Background service for seeding db data.</summary>
public class StartupSeedHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Creates a new instance of <see cref="StartupSeedHostedService"/></summary>
    /// <param name="provider">The service provider.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public StartupSeedHostedService(IServiceProvider provider) {
        _serviceProvider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>Executes the background service's logic.</summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        using var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var campaignsDbContext = serviceScope.ServiceProvider.GetRequiredService<CampaignsDbContext>();
        var defaultSender = await campaignsDbContext.MessageSenders.FirstOrDefaultAsync(x => x.IsDefault, stoppingToken);
        if (defaultSender is not null) {
            return;
        }
        var emailProviderFinder = serviceScope.ServiceProvider.GetRequiredService<EmailProviderFinder>();
        var defaultEmailProviderInfo = emailProviderFinder().FirstOrDefault();
        if (defaultEmailProviderInfo is null) {
            return;
        }
        campaignsDbContext.MessageSenders.Add(new Data.Models.DbMessageSender {
            Kind = MessageChannelKind.Email,
            DisplayName = defaultEmailProviderInfo.DefaultSender.DisplayName,
            Sender = defaultEmailProviderInfo.DefaultSender.Address,
            IsDefault = true
        });
        await campaignsDbContext.SaveChangesAsync(stoppingToken);
    }
}
