using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Manager;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Core.Services.Validators;
using Indice.Services;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Xunit;

namespace Indice.Features.Messages.Tests;

public class MergeContactTests : IAsyncLifetime
{
    private readonly int _numDuplicates = 5;
    public MergeContactTests() {
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
            .AddTransient<IContactService, ContactService>()
            .AddTransient(serviceProvider => new DatabaseSchemaNameResolver("cmp"))
            .AddTransient<IUserNameAccessor, UserNameAccessorNoOp>()
            .AddTransient<IEventDispatcherFactory, DefaultEventDispatcherFactory>()
            .AddTransient<UserNameAccessorAggregate>()
            .AddOptions()
            .Configure<MessageManagementOptions>(configuration);
        ServiceProvider = services.BuildServiceProvider();
    }

    public ServiceProvider ServiceProvider { get; }

    [Fact]
    public async Task CanGetDuplicates() {
        var contactService = ServiceProvider.GetRequiredService<IContactService>();
        var initDBResponse = await InitDatabase();
        var duplicates = await contactService.GetDuplicates(initDBResponse.MainContact,new());
        Assert.True(duplicates.Items.Count() == _numDuplicates);
        Assert.True(duplicates.Items.All(dup => dup.Email == initDBResponse.MainContact.Email));
    }

    [Fact]
    public async Task CanMergeDistributionListContactTable() {
        var contactService = ServiceProvider.GetRequiredService<IContactService>();
        var dbContext = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        var initDBResponse = await InitDatabase();
        var dbDistributionList = await dbContext.DistributionLists.Include(x => x.ContactDistributionLists).Where(x => x.Id == initDBResponse.DistributionListId).FirstAsync();
        Assert.True(dbDistributionList.ContactDistributionLists.Count == (_numDuplicates + 1));
        await contactService.MergeContacts(initDBResponse.MainContact, initDBResponse.DuplicateContactIds);
        dbContext.ChangeTracker.Clear();
        dbDistributionList = await dbContext.DistributionLists.Include(x => x.ContactDistributionLists).Where(x => x.Id == initDBResponse.DistributionListId).FirstAsync();
        Assert.True(dbDistributionList.ContactDistributionLists.Count == 1 && dbDistributionList.ContactDistributionLists.All(u => u.ContactId == initDBResponse.MainContact.Id));
    }

    [Fact]
    public async Task CanMergeDistributionListContactTableMainContactNotIncluded() {
        var contactService = ServiceProvider.GetRequiredService<IContactService>();
        var dbContext = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        var initDBResponse = await InitDatabase();
        var dbDistributionListMainNotIncluded = await dbContext.DistributionLists.Include(x => x.ContactDistributionLists).Where(x => x.Id == initDBResponse.DistributionListMainNotIncludedId).FirstAsync();
        Assert.True(dbDistributionListMainNotIncluded.ContactDistributionLists.Count == _numDuplicates);
        await contactService.MergeContacts(initDBResponse.MainContact, initDBResponse.DuplicateContactIds);
        dbContext.ChangeTracker.Clear();
        dbDistributionListMainNotIncluded = await dbContext.DistributionLists.Include(x => x.ContactDistributionLists).Where(x => x.Id == initDBResponse.DistributionListMainNotIncludedId).FirstAsync();
        Assert.True(dbDistributionListMainNotIncluded.ContactDistributionLists.Count == 1 && dbDistributionListMainNotIncluded.ContactDistributionLists.All(u => u.ContactId == initDBResponse.MainContact.Id));
    }

    [Fact]
    public async Task CanMergeMessageAndMessageEvent() {

        var contactService = ServiceProvider.GetRequiredService<IContactService>();
        var dbContext = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        InitDBResponse initDBResponse = await InitDatabase();
        await contactService.MergeContacts(initDBResponse.MainContact, initDBResponse.DuplicateContactIds);
        dbContext.ChangeTracker.Clear();
        Assert.True(await dbContext.Messages.AllAsync(x => x.ContactId == initDBResponse.MainContact.Id) && await dbContext.Messages.CountAsync() == 5);
        Assert.True(await dbContext.MessageEvents.AllAsync(x => x.ContactId == initDBResponse.MainContact.Id) && await dbContext.MessageEvents.CountAsync() == 5);
    }

    public async Task<InitDBResponse> InitDatabase() {
        var contactService = ServiceProvider.GetRequiredService<IContactService>();
        var dbContext = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        Guid campaignId = Guid.NewGuid();
        await dbContext.Campaigns.AddAsync(new DbCampaign {
            Id = campaignId,
            Title = "Test Campaign",
        });
        var distributionList = await CreateDistributionList("TestList", "testList");
        var distributionListMainNotIncluded = await CreateDistributionList("MainNotIncludedTestList", "mainNotIncludedTestList");
        await dbContext.SaveChangesAsync();

        var dbContacts = new List<DbContact>();
        for (int i = 0; i < _numDuplicates; i++) {
            var contact = await CreateContactAndAddContactDistributionList([distributionList.Id, distributionListMainNotIncluded.Id], i);
            dbContacts.Add(contact);
            var messageId = await CreateMessage(dbContacts[i], campaignId);
            await CreateMessageEvent(dbContacts[i], campaignId, messageId);
        }
        var mainDBContact = await CreateContactAndAddContactDistributionList([distributionList.Id]);
        await dbContext.SaveChangesAsync();
        //update for getDuplicates
        foreach (var dbContact in dbContacts) {
            dbContact.Email = "j.doe@indice.gr";
        }
        await dbContext.SaveChangesAsync();
        var mainContact = await contactService.GetById(mainDBContact.Id);
        return new InitDBResponse {
            MainContact = mainContact!,
            DuplicateContactIds = dbContacts.Select(x => x.Id).ToList(),
            DistributionListId = distributionList.Id,
            DistributionListMainNotIncludedId = distributionListMainNotIncluded.Id
        };
    }

    public class InitDBResponse
    {
        public Contact MainContact { get; set; } = null!;
        public List<Guid> DuplicateContactIds { get; set; } = [];
        public Guid DistributionListId { get; set; }
        public Guid DistributionListMainNotIncludedId { get; set; }
    }

    private DbContact CreateNumberedJohnDoeContact(int num) {
        return new DbContact {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Email = $"j{num}.doe@indice.gr",
            PhoneNumber = "6955555555",
            Resolved = false,
        };
    }

    private DbContact CreateMainJohnDoeContact() {
        return new DbContact {
            Id = Guid.NewGuid(),
            RecipientId = Guid.NewGuid().ToString(),
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Email = $"j.doe@indice.gr",
            PhoneNumber = "6955555555",
            Resolved = true,
        };
    }

    private async Task<DbDistributionList> CreateDistributionList(string name, string alias) {
        var dbContext = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        var distributionList = new DbDistributionList() {
            Id = Guid.NewGuid(),
            Name = name,
            Alias = alias,
        };
        await dbContext.DistributionLists.AddAsync(distributionList);
        return distributionList;
    }

    private async Task<Guid> CreateMessage(DbContact dbContact, Guid campaignId) {
        var dbContext = ServiceProvider.GetRequiredService<CampaignsDbContext>();

        var dbMessage = new DbMessage() {
            Id = Guid.NewGuid(),
            ContactId = dbContact.Id,
            CampaignId = campaignId,
            Content = new MessageContentDictionary(
                new Dictionary<MessageChannelKind, MessageContent> {
                    [MessageChannelKind.Email] = new($"Email Test Message", $"Test Message Content: {dbContact.Email}"),
                }),
        };
        await dbContext.Messages.AddAsync(dbMessage);
        return dbMessage.Id;
    }

    private async Task CreateMessageEvent(DbContact dbContact, Guid campaignId, Guid messageId) {
        var dbContext = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        var messageEvent = new DbMessageEvent {
            Id = Guid.NewGuid(),
            ContactId = dbContact.Id,
            CampaignId = Guid.NewGuid(),
            MessageId = messageId,
        };
        await dbContext.MessageEvents.AddAsync(messageEvent);
    }

    private async Task<DbContact> CreateContactAndAddContactDistributionList(List<Guid> distributionListsIds, int i = 0) {
        var dbContext = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        DbContact dbContact;
        if (i != 0) {
            dbContact = CreateNumberedJohnDoeContact(i);
        } else {
            dbContact = CreateMainJohnDoeContact();
        }
        await dbContext.Contacts.AddAsync(dbContact);
        foreach (var distributionListId in distributionListsIds) {
            dbContext.ContactDistributionLists.Add(new() {
                ContactId = dbContact.Id,
                DistributionListId = distributionListId
            });
        }
        return dbContact;
    }

    public class UserNameAccessorNoOp : IUserNameAccessor
    {
        public int Priority => 0;

        public string Resolve() => "static";
    }

    public async Task InitializeAsync() {
        var db = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() {
        var db = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        await db.Database.EnsureDeletedAsync();
        await ServiceProvider.DisposeAsync();
    }
}
