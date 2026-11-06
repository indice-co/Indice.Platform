using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Rendering;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Tests.Mocks;
using Indice.Services;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.Tests;

/// <summary>Covers the denormalized <see cref="DbDistributionList.ContactCount"/> and <see cref="DbCampaign.RecipientCount"/>.</summary>
public class RecipientCountTests : IAsyncLifetime
{
    // Known to MockContactResolver.
    private const string ResolvableRecipientId = "6c9fa6dd-ede4-486b-bf91-6de18542da4a";

    public RecipientCountTests() {
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=MessagesDb.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true";
        var configuration = new ConfigurationBuilder().Build();
        ServiceProvider = new ServiceCollection()
            .AddLogging()
            .AddOptions()
            .AddSingleton<IConfiguration>(configuration)
            .Configure<MessageManagementOptions>(configuration)
            .Configure<MessageInboxOptions>(configuration)
            .Configure<AnalyticsOptions>(configuration)
            .AddDbContext<CampaignsDbContext>(builder => builder.UseSqlServer(connectionString))
            .AddTransient(serviceProvider => new DatabaseSchemaNameResolver("cmp"))
            .AddTransient<IUserNameAccessor, UserNameAccessorNoOp>()
            .AddTransient<UserNameAccessorAggregate>()
            .AddTransient<IContactService, ContactService>()
            .AddTransient<IDistributionListService, DistributionListService>()
            .AddTransient<ICampaignService, CampaignService>()
            .AddTransient<ITemplateService, TemplateService>()
            .AddTransient<IPartialTemplateResolverFactory, DbBackedPartialTemplateResolverFactory>()
            .AddTransient<IContactResolver, MockContactResolver>()
            .AddTransient<IMessageService, MessageService>()
            .AddSingleton<MessageEventQueue>()
            .AddTransient<CampaignCreatedHandler>()
            .AddEventDispatcherNoop()
            .BuildServiceProvider();
    }

    public ServiceProvider ServiceProvider { get; }
    private CampaignsDbContext DbContext => ServiceProvider.GetRequiredService<CampaignsDbContext>();

    #region Distribution list ContactCount

    [Fact]
    public async Task Create_WithContacts_SetsContactCount() {
        var service = ServiceProvider.GetRequiredService<IDistributionListService>();

        var list = await service.Create(new CreateDistributionListRequest { Name = "created-with-contacts", IsSystemGenerated = true }, [
            new Contact { Email = "a@example.com" },
            new Contact { Email = "b@example.com" },
            new Contact { Email = "c@example.com" }
        ]);

        Assert.Equal(3, list.ContactCount);
        Assert.Equal(3, await GetContactCount(list.Id));
    }

    [Fact]
    public async Task AddToDistributionList_EveryBranch_IncrementsContactCount() {
        var listId = await SeedList("add-branches", members: 0);
        var existing = await SeedContact(recipientId: "known-recipient");
        var byContactId = await SeedContact();
        var service = ServiceProvider.GetRequiredService<IContactService>();

        await service.AddToDistributionList(listId, new CreateDistributionListContactRequest { ContactId = byContactId.Id });
        Assert.Equal(1, await GetContactCount(listId));

        await service.AddToDistributionList(listId, new CreateDistributionListContactRequest { RecipientId = existing.RecipientId });
        Assert.Equal(2, await GetContactCount(listId));

        await service.AddToDistributionList(listId, new CreateDistributionListContactRequest { Email = "brand-new@example.com" });
        Assert.Equal(3, await GetContactCount(listId));
    }

    [Fact]
    public async Task RemoveFromDistributionList_DecrementsContactCount() {
        var listId = await SeedList("remove", members: 2);
        var contactId = await DbContext.ContactDistributionLists.Where(x => x.DistributionListId == listId).Select(x => x.ContactId).FirstAsync(TestContext.Current.CancellationToken);

        await ServiceProvider.GetRequiredService<IContactService>().RemoveFromDistributionList(listId, contactId);

        Assert.Equal(1, await GetContactCount(listId));
    }

    [Fact]
    public async Task BulkAddToDistributionList_CountsOnlyNewMemberships() {
        var listId = await SeedList("bulk", members: 0);
        var alreadyLinked = await SeedContact(recipientId: "already-linked");
        DbContext.ContactDistributionLists.Add(new DbDistributionListContact { DistributionListId = listId, ContactId = alreadyLinked.Id });
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await ServiceProvider.GetRequiredService<IContactService>().BulkAddToDistributionList(listId, [
            new CreateDistributionListContactRequest { RecipientId = "already-linked", Email = "linked@example.com" },
            new CreateDistributionListContactRequest { RecipientId = "new-1", Email = "new1@example.com" },
            new CreateDistributionListContactRequest { Email = "new2@example.com" }
        ]);

        Assert.Equal(3, await GetContactCount(listId));
    }

    [Fact]
    public async Task MergeContacts_RecountsEveryListOfTheDuplicates() {
        var main = await SeedContact(recipientId: "main", email: "same@example.com");
        var duplicate1 = await SeedContact(email: "same@example.com");
        var duplicate2 = await SeedContact(email: "same@example.com");
        // The main contact and both duplicates: collapses to one member.
        var withMainId = await SeedList("merge-with-main", members: 0);
        // Only the duplicates: the main contact takes their place.
        var withoutMainId = await SeedList("merge-without-main", members: 0);
        DbContext.ContactDistributionLists.AddRange(
            new DbDistributionListContact { DistributionListId = withMainId, ContactId = main.Id },
            new DbDistributionListContact { DistributionListId = withMainId, ContactId = duplicate1.Id },
            new DbDistributionListContact { DistributionListId = withMainId, ContactId = duplicate2.Id },
            new DbDistributionListContact { DistributionListId = withoutMainId, ContactId = duplicate1.Id },
            new DbDistributionListContact { DistributionListId = withoutMainId, ContactId = duplicate2.Id });
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var contactService = ServiceProvider.GetRequiredService<IContactService>();

        await contactService.MergeContacts((await contactService.GetById(main.Id))!, [duplicate1.Id, duplicate2.Id]);

        Assert.Equal(1, await GetContactCount(withMainId));
        Assert.Equal(1, await GetContactCount(withoutMainId));
    }

    [Fact]
    public async Task MarkAsRead_ByContactOutsideTheList_IncrementsContactCount() {
        var listId = await SeedList("inbox-read", members: 1);
        var campaignId = await SeedCampaign(listId, published: true);

        await ServiceProvider.GetRequiredService<IMessageService>().MarkAsRead(campaignId, ResolvableRecipientId);

        Assert.Equal(2, await GetContactCount(listId));
    }

    [Fact]
    public async Task ContactCount_IsReturnedByTheDtos() {
        var listId = await SeedList("dto", members: 2);
        await DbContext.DistributionLists.Where(x => x.Id == listId).ExecuteUpdateAsync(s => s.SetProperty(x => x.ContactCount, 2), TestContext.Current.CancellationToken);
        var campaignId = await SeedCampaign(listId, published: false);
        // The seeded list is still tracked with its pre-ExecuteUpdate value, and FindAsync would return that instance.
        DbContext.ChangeTracker.Clear();
        var listService = ServiceProvider.GetRequiredService<IDistributionListService>();
        var campaignService = ServiceProvider.GetRequiredService<ICampaignService>();

        Assert.Equal(2, (await listService.GetById(new GuidOrAlias(listId.ToString())))!.ContactCount);
        Assert.Equal(2, (await listService.GetByName("dto"))!.ContactCount);
        Assert.Equal(2, (await listService.GetList(new ListOptions(), new DistributionListFilter())).Items.Single(x => x.Id == listId).ContactCount);
        Assert.Equal(2, (await campaignService.GetById(campaignId))!.DistributionList!.ContactCount);
    }

    #endregion

    #region Campaign RecipientCount

    [Fact]
    public async Task CampaignCreated_Published_SetsRecipientCount() {
        var listId = await SeedList("published", members: 3);
        var campaignId = await SeedCampaign(listId, published: true);

        await ProcessCampaignCreated(campaignId);

        Assert.Equal(3, await GetRecipientCount(campaignId));
        var campaigns = await ServiceProvider.GetRequiredService<ICampaignService>().GetList(new ListOptions<CampaignListFilter>());
        Assert.Equal(3, campaigns.Items.Single(x => x.Id == campaignId).RecipientCount);
    }

    [Fact]
    public async Task CampaignCreated_Draft_LeavesRecipientCountNull() {
        var listId = await SeedList("draft", members: 3);
        var campaignId = await SeedCampaign(listId, published: false);

        await ProcessCampaignCreated(campaignId);

        Assert.Null(await GetRecipientCount(campaignId));
    }

    [Fact]
    public async Task CampaignCreated_Global_LeavesRecipientCountNull() {
        var listId = await SeedList("global", members: 3);
        var campaignId = await SeedCampaign(listId, published: true, isGlobal: true);

        await ProcessCampaignCreated(campaignId);

        Assert.Null(await GetRecipientCount(campaignId));
    }

    [Fact]
    public async Task CampaignCreated_DeliveredTwice_DoesNotDoubleRecipientCount() {
        var listId = await SeedList("redelivered", members: 2);
        var campaignId = await SeedCampaign(listId, published: true);

        await ProcessCampaignCreated(campaignId);
        await ProcessCampaignCreated(campaignId);

        Assert.Equal(2, await GetRecipientCount(campaignId));
    }

    [Fact]
    public async Task RecipientCount_IsASnapshot_NotChangedByLaterListEdits() {
        var listId = await SeedList("snapshot", members: 2);
        var campaignId = await SeedCampaign(listId, published: true);
        await ProcessCampaignCreated(campaignId);

        await ServiceProvider.GetRequiredService<IContactService>().AddToDistributionList(listId, new CreateDistributionListContactRequest { Email = "late@example.com" });

        Assert.Equal(3, await GetContactCount(listId));
        Assert.Equal(2, await GetRecipientCount(campaignId));
    }

    #endregion

    #region Recipient metrics

    [Fact]
    public async Task RecipientMetrics_Campaign_ReportsTheSentAudience() {
        var listId = await SeedList("metrics-sent", members: 2);
        var campaignId = await SeedCampaign(listId, published: true);
        await ProcessCampaignCreated(campaignId);
        await ServiceProvider.GetRequiredService<IContactService>().AddToDistributionList(listId, new CreateDistributionListContactRequest { Email = "late@example.com" });

        var metrics = await ServiceProvider.GetRequiredService<ICampaignService>().GetRecipientMetrics(campaignId);

        Assert.Equal(2, metrics!.Total);
    }

    [Fact]
    public async Task RecipientMetrics_DraftCampaign_TotalIsZero() {
        var listId = await SeedList("metrics-draft", members: 2);
        var campaignId = await SeedCampaign(listId, published: false);

        var metrics = await ServiceProvider.GetRequiredService<ICampaignService>().GetRecipientMetrics(campaignId);

        Assert.Equal(0, metrics!.Total);
        Assert.Equal(0, metrics.Coverage);
    }

    [Fact]
    public async Task RecipientMetrics_GlobalCampaign_TotalIsZero() {
        var campaignId = await SeedCampaign(await SeedList("metrics-global", members: 2), published: true, isGlobal: true);
        await ProcessCampaignCreated(campaignId);
        var campaignService = ServiceProvider.GetRequiredService<ICampaignService>();

        Assert.Equal(0, (await campaignService.GetRecipientMetrics(campaignId))!.Total);
        Assert.Equal(0, (await campaignService.GetRecipientMetrics())!.Total);
    }

    [Fact]
    public async Task RecipientMetrics_Overview_SumsOnlySentCampaigns() {
        var sentListId = await SeedList("overview-sent", members: 2);
        var sentCampaignId = await SeedCampaign(sentListId, published: true);
        await ProcessCampaignCreated(sentCampaignId);
        await SeedCampaign(await SeedList("overview-draft", members: 3), published: false);

        var metrics = await ServiceProvider.GetRequiredService<ICampaignService>().GetRecipientMetrics();

        Assert.Equal(2, metrics!.TotalCampaigns);
        Assert.Equal(2, metrics.Total);
    }

    [Fact]
    public async Task RecipientMetrics_Overview_NoSentCampaign_TotalIsZero() {
        await SeedCampaign(await SeedList("overview-only-draft", members: 3), published: false);

        var metrics = await ServiceProvider.GetRequiredService<ICampaignService>().GetRecipientMetrics();

        Assert.Equal(0, metrics!.Total);
        Assert.Equal(0, metrics.AvgTotal);
    }

    #endregion

    #region Helpers

    private async Task ProcessCampaignCreated(Guid campaignId) {
        var campaign = await ServiceProvider.GetRequiredService<ICampaignService>().GetById(campaignId);
        await ServiceProvider.GetRequiredService<CampaignCreatedHandler>().Process(CampaignCreatedEvent.FromCampaign(campaign!));
    }

    private async Task<Guid> SeedList(string name, int members) {
        var list = new DbDistributionList { Id = Guid.NewGuid(), Name = name, CreatedBy = "test" };
        for (var i = 0; i < members; i++) {
            list.ContactDistributionLists.Add(new DbDistributionListContact { Contact = new DbContact { Id = Guid.NewGuid(), Email = $"{name}-{i}@example.com" } });
        }
        DbContext.DistributionLists.Add(list);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return list.Id;
    }

    private async Task<DbContact> SeedContact(string? recipientId = null, string? email = null) {
        var contact = new DbContact { Id = Guid.NewGuid(), RecipientId = recipientId, Email = email ?? $"{Guid.NewGuid():N}@example.com" };
        DbContext.Contacts.Add(contact);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return contact;
    }

    private async Task<Guid> SeedCampaign(Guid distributionListId, bool published, bool isGlobal = false) {
        var campaign = new DbCampaign {
            Title = "Recipient count campaign",
            CreatedBy = "test",
            DistributionListId = distributionListId,
            Published = published,
            IsGlobal = isGlobal,
            MessageChannelKind = MessageChannelKind.Inbox,
            Content = new MessageContentDictionary(new Dictionary<MessageChannelKind, MessageContent> {
                [MessageChannelKind.Inbox] = new("Hello", "Body")
            })
        };
        DbContext.Campaigns.Add(campaign);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return campaign.Id;
    }

    // ExecuteUpdate bypasses the change tracker, so read the committed values.
    private Task<int> GetContactCount(Guid listId) =>
        DbContext.DistributionLists.AsNoTracking().Where(x => x.Id == listId).Select(x => x.ContactCount).SingleAsync(TestContext.Current.CancellationToken);

    private Task<int?> GetRecipientCount(Guid campaignId) =>
        DbContext.Campaigns.AsNoTracking().Where(x => x.Id == campaignId).Select(x => x.RecipientCount).SingleAsync(TestContext.Current.CancellationToken);

    #endregion

    public async ValueTask InitializeAsync() => await DbContext.Database.EnsureCreatedAsync();

    public async ValueTask DisposeAsync() {
        await DbContext.Database.EnsureDeletedAsync();
        await ServiceProvider.DisposeAsync();
    }
}
