using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Xunit;

namespace Indice.Features.Messages.Tests;

public class CampaignDatabaseCleanUpTests : IAsyncLifetime
{
    // Retention days from MessagingDatabaseCleanUpOptions defaults
    private const int RetentionDaysForInbox = 180;
    private const int RetentionDaysForOther = 120;

    public ServiceProvider ServiceProvider { get; }

    public CampaignDatabaseCleanUpTests() {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:MessagesDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=MessagesDb.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true"
            })
            .Build();
        var services = new ServiceCollection()
            .AddLogging()
            .AddTransient<IHostEnvironment>(serviceProvider => new HostingEnvironment {
                ApplicationName = typeof(MessageManagerTests).Assembly.GetName().Name!,
                EnvironmentName = Environments.Development,
                ContentRootPath = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\"),
                ContentRootFileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\"))
            })
            .AddDbContext<CampaignsDbContext>(builder => builder.UseSqlServer(configuration.GetConnectionString("MessagesDb")))
            .AddSingleton(configuration)
            .AddTransient<ICampaignService, CampaignService>()
            .AddTransient<IContactService, ContactService>()
            .AddTransient(serviceProvider => new DatabaseSchemaNameResolver("cmp"))
            .AddTransient<IUserNameAccessor, UserNameAccessorNoOp>()
            .AddTransient<UserNameAccessorAggregate>()
            .AddSingleton<IFileServiceFactory, DefaultFileServiceFactory>()
            .AddKeyedSingleton<IFileService, FileServiceInMemory>("Messages:FileServiceKey")
            .Configure<MessageWorkerOptions>(options => {
                options.DatabaseCleanUpOptions.Enabled = true;
            })
            .AddTransient<IMessagingDatabaseCleanUpService, MessagingDatabaseCleanUpService>()
            .AddOptions()
            .Configure<MessageManagementOptions>(configuration);
        ServiceProvider = services.BuildServiceProvider();
    }

    public async ValueTask InitializeAsync() {
        var db = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync() {
        var db = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        await db.Database.EnsureDeletedAsync();
        await ServiceProvider.DisposeAsync();
    }

    [Fact]
    public async Task CleanUpCampaignsWithInboxAsync_DeletesOldCampaigns() {
        // Arrange
        var db = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        var cleanupService = ServiceProvider.GetRequiredService<IMessagingDatabaseCleanUpService>();

        // Create distribution list
        var distributionLists = new List<DbDistributionList>();
        for (int i = 0; i < 5; i++) {
            distributionLists.Add(new DbDistributionList {
                Id = Guid.NewGuid(),
                Name = $"Test List {i}",
                CreatedBy = "system",
                ContactDistributionLists = new List<DbDistributionListContact>()
            });
        }
        db.DistributionLists.AddRange(distributionLists);

        var oldCampaign1 = CreateCampaign(MessageChannelKind.Inbox, RetentionDaysForInbox + 5, true, distributionLists[0].Id);
        var oldCampaign2 = CreateCampaign(MessageChannelKind.Inbox, RetentionDaysForInbox + 10, true, distributionLists[1].Id);
        var oldCampaign3 = CreateCampaign(MessageChannelKind.Inbox | MessageChannelKind.Email, RetentionDaysForInbox + 20, true, distributionLists[2].Id);

        var recentCampaign = CreateCampaign(MessageChannelKind.Inbox, RetentionDaysForInbox - 170, true, distributionLists[3].Id);
        var unpublishedCampaign = CreateCampaign(MessageChannelKind.Inbox, RetentionDaysForInbox + 20, false, distributionLists[4].Id);

        db.Campaigns.AddRange(oldCampaign1, oldCampaign2, oldCampaign3, recentCampaign, unpublishedCampaign);

        // Add some message events for old campaigns
        db.MessageEvents.Add(new DbMessageEvent {
            CampaignId = oldCampaign1.Id,
            ContactId = Guid.NewGuid(),
            Type = "Sent",
            Channel = "Inbox",
            Recipient = "user1"
        });

        db.SaveChanges();
        await cleanupService.CleanUpCampaignsWithInboxAsync();
        var remainingCampaigns = await db.Campaigns.ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, remainingCampaigns.Count); // Only recent and unpublished should remain
        Assert.Contains(remainingCampaigns, c => c.Id == recentCampaign.Id);
        Assert.Contains(remainingCampaigns, c => c.Id == unpublishedCampaign.Id);

        var remainingEvents = await db.MessageEvents.Where(e => e.CampaignId == oldCampaign1.Id).ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(remainingEvents);

        var remainingLists = await db.DistributionLists.ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, remainingLists.Count);
    }

    [Fact]
    public async Task CleanUpCampaignsWithoutInboxAsync_DeletesOldCampaigns() {
        // Arrange
        var db = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        var cleanupService = ServiceProvider.GetRequiredService<IMessagingDatabaseCleanUpService>();

        // Create distribution list
        var distributionLists = new List<DbDistributionList>();
        for (int i = 0; i < 5; i++) {
            distributionLists.Add(new DbDistributionList {
                Id = Guid.NewGuid(),
                Name = $"Test List {i}",
                CreatedBy = "system",
                ContactDistributionLists = new List<DbDistributionListContact>()
            });
        }
        db.DistributionLists.AddRange(distributionLists);

        var oldCampaign1 = CreateCampaign(MessageChannelKind.Email, RetentionDaysForOther + 4, true, distributionLists[0].Id);
        var oldCampaign2 = CreateCampaign(MessageChannelKind.Email, RetentionDaysForOther + 50, true, distributionLists[1].Id);
        var oldCampaign3 = CreateCampaign(MessageChannelKind.Email, RetentionDaysForOther + 9, true, distributionLists[2].Id);

        var recentCampaign = CreateCampaign(MessageChannelKind.Email, RetentionDaysForOther - 110, true, distributionLists[3].Id);
        var unpublishedCampaign = CreateCampaign(MessageChannelKind.Email, RetentionDaysForOther + 15, false, distributionLists[4].Id);

        db.Campaigns.AddRange(oldCampaign1, oldCampaign2, oldCampaign3, recentCampaign, unpublishedCampaign);

        // Add some message events for old campaigns
        db.MessageEvents.Add(new DbMessageEvent {
            CampaignId = oldCampaign1.Id,
            ContactId = Guid.NewGuid(),
            Type = "Sent",
            Channel = "Email",
            Recipient = "user1"
        });

        db.SaveChanges();
        await cleanupService.CleanUpCampaignsWithoutInboxAsync();
        var remainingCampaigns = await db.Campaigns.ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, remainingCampaigns.Count); // Only recent and unpublished should remain
        Assert.Contains(remainingCampaigns, c => c.Id == recentCampaign.Id);
        Assert.Contains(remainingCampaigns, c => c.Id == unpublishedCampaign.Id);
        var remainingEvents = await db.MessageEvents.Where(e => e.CampaignId == oldCampaign1.Id).ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(remainingEvents);
        var remainingLists = await db.DistributionLists.ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, remainingLists.Count);
    }

    [Fact]
    public async Task CleanUpCampaigns_HandlesEmptyDatabase() {
        var db = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        var cleanupService = ServiceProvider.GetRequiredService<IMessagingDatabaseCleanUpService>();

        await cleanupService.CleanUpCampaignsWithInboxAsync();
        await cleanupService.CleanUpCampaignsWithoutInboxAsync();

        var campaigns = await db.Campaigns.ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(campaigns);
    }

    [Fact]
    public async Task CleanUpCampaigns_RespectsActivePeriod() {
        var db = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        var cleanupService = ServiceProvider.GetRequiredService<IMessagingDatabaseCleanUpService>();

        var distributionList = new DbDistributionList {
            Id = Guid.NewGuid(),
            Name = "Test List",
            CreatedBy = "system"
        };
        db.DistributionLists.Add(distributionList);

        var campaignWithActivePeriod = CreateCampaign(MessageChannelKind.Email, RetentionDaysForOther + 15, true, distributionList.Id);
        campaignWithActivePeriod.ActivePeriod = new Types.Period {
            From = DateTimeOffset.UtcNow.AddDays(-10), // Active period started 10 days ago
            To = DateTimeOffset.UtcNow.AddDays(10)
        };

        db.Campaigns.Add(campaignWithActivePeriod);
        db.SaveChanges();

        await cleanupService.CleanUpCampaignsWithoutInboxAsync();
        var remainingCampaigns = await db.Campaigns.ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(remainingCampaigns);
        Assert.Equal(campaignWithActivePeriod.Id, remainingCampaigns[0].Id);
    }

    private DbCampaign CreateCampaign(MessageChannelKind channelKind, int daysAgo, bool published, Guid distributionListId) {
        return new DbCampaign {
            Id = Guid.NewGuid(),
            Title = $"Test Campaign {Guid.NewGuid()}",
            MessageChannelKind = channelKind,
            Published = published,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-daysAgo),
            CreatedBy = "test",
            DistributionListId = distributionListId,
            Content = new MessageContentDictionary {
                [channelKind.ToString()] = new MessageContent { Title = "Test", Body = "Test Body" }
            }
        };
    }

}
