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
            .AddDbContext<TestCampaignsDbContext>(builder => builder.UseSqlServer(configuration.GetConnectionString("MessagesDb")))
            .AddSingleton(configuration)
            .AddTransient<ICampaignService, CampaignService>()
            .AddTransient<IContactService, ContactService>()
            .AddTransient(serviceProvider => new DatabaseSchemaNameResolver("cmp"))
            .AddTransient<IUserNameAccessor, UserNameAccessorNoOp>()
            .AddTransient<UserNameAccessorAggregate>()
            .AddSingleton<IFileServiceFactory, DefaultFileServiceFactory>()
            //.AddSingleton<IFileService, FileServiceNoop>()
            //.AddSingleton<IFileService, FileServiceInMemory>()
            .AddKeyedSingleton<IFileService, FileServiceInMemory>("Messages:FileServiceKey")
            //.AddSingleton<IFileService,FileServiceAzureStorage>()
            .AddTransient<IDatabaseCleanUpService, DatabaseCleanUpService>()
            .AddOptions()
            .Configure<MessageManagementOptions>(configuration);
        ServiceProvider = services.BuildServiceProvider();
    }
    public class TestCampaignsDbContext : CampaignsDbContext
    {
        public TestCampaignsDbContext(
            DbContextOptions<CampaignsDbContext> options)
            : base(options) {
        }

        /// <summary>
        /// Override SaveChangesAsync to bypass the OnBeforeSaving() method in CampaignsDbContext.
        /// This allows us to set custom CreatedAt/UpdatedAt dates for testing purposes.
        /// </summary>
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) {
            return Task.Run(() => base.SaveChanges(acceptAllChangesOnSuccess));
        }
    }


        public async Task InitializeAsync() {
        var db = ServiceProvider.GetRequiredService<TestCampaignsDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() {
        var db = ServiceProvider.GetRequiredService<TestCampaignsDbContext>();
        await db.Database.EnsureDeletedAsync();
        await ServiceProvider.DisposeAsync();
    }

    [Fact]
    public async Task CleanUpCampaignsWithInboxAsync_DeletesOldCampaigns() {
        // Arrange
        var db = ServiceProvider.GetRequiredService<TestCampaignsDbContext>();
        var cleanupService = ServiceProvider.GetRequiredService<IDatabaseCleanUpService>();

        // Create distribution list
        var distributionLists = new List<DbDistributionList>();
        for(int i=0; i<5; i++) {
            distributionLists.Add(new DbDistributionList {
                Id = Guid.NewGuid(),
                Name = $"Test List {i}",
                CreatedBy = "system",
                ContactDistributionLists = new List<DbDistributionListContact>()
            });
        }
        db.DistributionLists.AddRange(distributionLists);

        var oldCampaign1 = CreateCampaign(MessageChannelKind.Inbox, 135, true, distributionLists[0].Id);
        var oldCampaign2 = CreateCampaign(MessageChannelKind.Inbox, 140, true, distributionLists[1].Id);
        var oldCampaign3 = CreateCampaign(MessageChannelKind.Inbox | MessageChannelKind.Email, 145, true, distributionLists[2].Id);

        var recentCampaign = CreateCampaign(MessageChannelKind.Inbox, 10, true, distributionLists[3].Id);
        var unpublishedCampaign = CreateCampaign(MessageChannelKind.Inbox, 135, false, distributionLists[4].Id);

        db.Campaigns.AddRange(oldCampaign1, oldCampaign2, oldCampaign3, recentCampaign, unpublishedCampaign);

        // Add some message events for old campaigns
        db.MessageEvents.Add(new DbMessageEvent {
            CampaignId = oldCampaign1.Id,
            ContactId = Guid.NewGuid(),
            Type = "Sent",
            Channel = "Inbox",
            Recipient = "user1"
        });

        await db.SaveChangesAsync();
        await cleanupService.CleanUpCampaignsWithInboxAsync();
        var remainingCampaigns = await db.Campaigns.ToListAsync();
        Assert.Equal(2, remainingCampaigns.Count); // Only recent and unpublished should remain
        Assert.Contains(remainingCampaigns, c => c.Id == recentCampaign.Id);
        Assert.Contains(remainingCampaigns, c => c.Id == unpublishedCampaign.Id);

        var remainingEvents = await db.MessageEvents.Where(e => e.CampaignId == oldCampaign1.Id).ToListAsync();
        Assert.Empty(remainingEvents);

        var remainingLists = await db.DistributionLists.ToListAsync();
        Assert.Equal(2, remainingLists.Count);
    }

    [Fact]
    public async Task CleanUpCampaignsWithoutInboxAsync_DeletesOldCampaigns() {
        // Arrange
        var db = ServiceProvider.GetRequiredService<TestCampaignsDbContext>();
        var cleanupService = ServiceProvider.GetRequiredService<IDatabaseCleanUpService>();

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

        var oldCampaign1 = CreateCampaign(MessageChannelKind.Email, 32, true, distributionLists[0].Id);
        var oldCampaign2 = CreateCampaign(MessageChannelKind.Email, 31, true, distributionLists[1].Id);
        var oldCampaign3 = CreateCampaign(MessageChannelKind.Email, 123, true, distributionLists[2].Id);

        var recentCampaign = CreateCampaign(MessageChannelKind.Email, 10, true, distributionLists[3].Id);
        var unpublishedCampaign = CreateCampaign(MessageChannelKind.Email, 35, false, distributionLists[4].Id);

        db.Campaigns.AddRange(oldCampaign1, oldCampaign2, oldCampaign3, recentCampaign, unpublishedCampaign);

        // Add some message events for old campaigns
        db.MessageEvents.Add(new DbMessageEvent {
            CampaignId = oldCampaign1.Id,
            ContactId = Guid.NewGuid(),
            Type = "Sent",
            Channel = "Email",
            Recipient = "user1"
        });

        await db.SaveChangesAsync();
        await cleanupService.CleanUpCampaignsWithoutInboxAsync();
        var remainingCampaigns = await db.Campaigns.ToListAsync();
        Assert.Equal(2, remainingCampaigns.Count); // Only recent and unpublished should remain
        Assert.Contains(remainingCampaigns, c => c.Id == recentCampaign.Id);
        Assert.Contains(remainingCampaigns, c => c.Id == unpublishedCampaign.Id);
        var remainingEvents = await db.MessageEvents.Where(e => e.CampaignId == oldCampaign1.Id).ToListAsync();
        Assert.Empty(remainingEvents);
        var remainingLists = await db.DistributionLists.ToListAsync();
        Assert.Equal(2, remainingLists.Count);
    }

    [Fact]
    public async Task CleanUpCampaigns_HandlesEmptyDatabase() {
        var db = ServiceProvider.GetRequiredService<TestCampaignsDbContext>();
        var cleanupService = ServiceProvider.GetRequiredService<IDatabaseCleanUpService>();

        await cleanupService.CleanUpCampaignsWithInboxAsync();
        await cleanupService.CleanUpCampaignsWithoutInboxAsync();

        var campaigns = await db.Campaigns.ToListAsync();
        Assert.Empty(campaigns);
    }

    [Fact]
    public async Task CleanUpCampaigns_RespectsActivePeriod() {
        var db = ServiceProvider.GetRequiredService<TestCampaignsDbContext>();
        var cleanupService = ServiceProvider.GetRequiredService<IDatabaseCleanUpService>();

        var distributionList = new DbDistributionList {
            Id = Guid.NewGuid(),
            Name = "Test List",
            CreatedBy = "system"
        };
        db.DistributionLists.Add(distributionList);

        var campaignWithActivePeriod = CreateCampaign(MessageChannelKind.Email, 35, true, distributionList.Id);
        campaignWithActivePeriod.ActivePeriod = new Types.Period {
            From = DateTimeOffset.UtcNow.AddDays(-10), // Active period started 10 days ago
            To = DateTimeOffset.UtcNow.AddDays(10)
        };

        db.Campaigns.Add(campaignWithActivePeriod);
        await db.SaveChangesAsync();

        await cleanupService.CleanUpCampaignsWithoutInboxAsync();
        var remainingCampaigns = await db.Campaigns.ToListAsync();
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
