using System;
using Indice.AspNetCore.Authorization;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Manager;
using Indice.Features.Messages.Core.Manager.Commands;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Core.Services.Validators;
using Indice.Features.Messages.Tests.Mocks;
using Indice.Features.Messages.Tests.Security;
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
    public MergeContactTests() {
        var inMemorySettings = new Dictionary<string, string?> {
            ["ConnectionStrings:MessagesDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=MessagesDb.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:MessagesDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=MessagesDb.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true"//,
                //["ConnectionStrings:StorageConnection"] = "UseDevelopmentStorage=true"
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
            .AddDbContext<CampaignsDbContext>(builder => builder.UseSqlServer(configuration.GetConnectionString("MessagesDb")
             /*sqlOptions => sqlOptions.EnableRetryOnFailure()*/))
            .AddSingleton(configuration)
            .AddTransient<NotificationsManager>()
            .AddTransient<ICampaignService, CampaignService>()
            .AddTransient<ICampaignAttachmentService, CampaignAttachmentService>()
            .AddTransient<IContactService, ContactService>().AddTransient<IContactResolver, ContactResolverIdentity>().AddHttpClient().AddDistributedMemoryCache().AddTransient<IMessageEventService, MessageEventService>().AddTransient<MessageEventQueue>()
            .AddTransient<IMessageService, MessageService>()
            .AddTransient<IMessageTypeService, MessageTypeService>()
            .AddTransient<IDistributionListService, DistributionListService>()
            .AddTransient<ITemplateService, TemplateService>()
            .AddTransient<CreateCampaignRequestValidator>()
            .AddTransient<CreateMessageTypeRequestValidator>()
            .AddTransient<IEventDispatcherFactory, DefaultEventDispatcherFactory>()
            .AddTransient(serviceProvider => new DatabaseSchemaNameResolver("cmp"))
            .AddTransient<IUserNameAccessor, UserNameAccessorNoOp>()
            .AddTransient<UserNameAccessorAggregate>()
            .AddFiles(x => x.AddFilesInMemory(KeyedServiceNames.FileServiceKey))
            .AddOptions()
            .Configure<MessageManagementOptions>(configuration);
        ServiceProvider = services.BuildServiceProvider();
    }

    public ServiceProvider ServiceProvider { get; }

    [Fact]
    public async Task CanGetDuplicates() {
        var contactService = ServiceProvider.GetRequiredService<IContactService>();
        List<CreateContactRequest> createContactRequests = new List<CreateContactRequest>();
        for(int i=0; i<5; i++) {
            var createContactRequest = new CreateContactRequest {
                FirstName = "John",
                LastName = "Doe",
                FullName = "John Doe",
                Email = $"j.doe@indice.gr",
                PhoneNumber = "6955555555",
                Resolved = false,
            };
            createContactRequests.Add(createContactRequest);
        }
        var createMainContactRequest = new CreateContactRequest {
            RecipientId = Guid.NewGuid().ToString(),
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Email = $"j.doe@indice.gr",
            PhoneNumber = "6955555555",
            Resolved = true,
        };
        var mainContact = await contactService.Create(createMainContactRequest);
        await contactService.CreateMany(createContactRequests);
        var duplicates =  await contactService.GetDuplicates(mainContact.RecipientId, mainContact.Email, mainContact.Id.Value);
        Assert.True(duplicates.Count == 5);
    }

    [Fact]
    public async Task CanMergeDistributionListContactTable() {
        var contactService = ServiceProvider.GetRequiredService<IContactService>();
        var distributionListService = ServiceProvider.GetRequiredService<IDistributionListService>();
        var dbContext = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        CreateDistributionListRequest distributionListRequest = new CreateDistributionListRequest() {
            Name = "TestList",
            Alias = "testList",
        };
        var distributionList = await distributionListService.Create(distributionListRequest);

        List<CreateDistributionListContactRequest> createDistributionListContactRequests = new List<CreateDistributionListContactRequest>();
        for (int i = 0; i < 5; i++) {
            var createContactRequest = new CreateDistributionListContactRequest {
                ContactId = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                FullName = "John Doe",
                Email = $"j{i}.doe@indice.gr",
                PhoneNumber = "6955555555",
                Resolved = false,
            };
            createDistributionListContactRequests.Add(createContactRequest);
        }

        var createMainContactRequest = new CreateDistributionListContactRequest {
            RecipientId = Guid.NewGuid().ToString(),
            ContactId = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Email = $"j.doe@indice.gr",
            PhoneNumber = "6955555555",
            Resolved = true,
        };
        createDistributionListContactRequests.Add(createMainContactRequest);
        await contactService.BulkAddToDistributionList(distributionList.Id, createDistributionListContactRequests);

        DbDistributionList? dbDistributionList = await dbContext.DistributionLists.Include(x => x.ContactDistributionLists).Where(x => x.Id == distributionList.Id).AsNoTracking().FirstOrDefaultAsync();

        Assert.True(dbDistributionList != null);
        Assert.True(dbDistributionList.ContactDistributionLists.Count == 6);

        createDistributionListContactRequests.Remove(createMainContactRequest);
        foreach (var contact in createDistributionListContactRequests) {
            DbContact dbContact = dbContext.Contacts.Find(contact.ContactId);
            dbContact.Email = "j.doe@indice.gr";
        }

        await dbContext.SaveChangesAsync();
        await contactService.MergeContacts(createMainContactRequest.ContactId.Value, createDistributionListContactRequests.Select(x => x.ContactId.Value).ToList());
        dbContext.ChangeTracker.Clear();

        dbDistributionList = dbContext.DistributionLists.Include(x => x.ContactDistributionLists).Where(x => x.Id == distributionList.Id).First();
        Assert.True(dbDistributionList.ContactDistributionLists.Count == 1);
    }

    [Fact]
    public async Task CanMergeDistributionListContactTableMainNotIncluded() {
        var contactService = ServiceProvider.GetRequiredService<IContactService>();
        var distributionListService = ServiceProvider.GetRequiredService<IDistributionListService>();
        var dbContext = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        CreateDistributionListRequest distributionListRequestMainNotIncluded = new CreateDistributionListRequest() {
            Name = "MainNotIncludedTestList",
            Alias = "mainNotIncludedTestList",
        };
        var distributionListMainNotIncluded = await distributionListService.Create(distributionListRequestMainNotIncluded);

        List<CreateDistributionListContactRequest> createDistributionListContactRequests = new List<CreateDistributionListContactRequest>();
        for (int i = 0; i < 5; i++) {
            var createContactRequest = new CreateDistributionListContactRequest {
                ContactId = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                FullName = "John Doe",
                Email = $"j{i}.doe@indice.gr",
                PhoneNumber = "6955555555",
                Resolved = false,
            };
            createDistributionListContactRequests.Add(createContactRequest);
        }

        var createMainContactRequest = new CreateContactRequest {
            RecipientId = Guid.NewGuid().ToString(),
            //ContactId = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Email = $"j.doe@indice.gr",
            PhoneNumber = "6955555555",
            Resolved = true,
        };
        await contactService.Create(createMainContactRequest);
        await dbContext.SaveChangesAsync();
        var mainContact = await contactService.GetByRecipientId(createMainContactRequest.RecipientId);
        await contactService.BulkAddToDistributionList(distributionListMainNotIncluded.Id, createDistributionListContactRequests);

        DbDistributionList? dbDistributionList = await dbContext.DistributionLists.Include(x => x.ContactDistributionLists).Where(x => x.Id == distributionListMainNotIncluded.Id).AsNoTracking().FirstOrDefaultAsync();

        Assert.True(dbDistributionList != null);
        Assert.True(dbDistributionList.ContactDistributionLists.Count == 5);

        foreach (var contact in createDistributionListContactRequests) {
            DbContact dbContact = dbContext.Contacts.Find(contact.ContactId);
            dbContact.Email = "j.doe@indice.gr";
        }

        await dbContext.SaveChangesAsync();
        await contactService.MergeContacts(mainContact.Id.Value, createDistributionListContactRequests.Select(x => x.ContactId.Value).ToList());
        dbContext.ChangeTracker.Clear();

        dbDistributionList = dbContext.DistributionLists.Include(x => x.ContactDistributionLists).Where(x => x.Id == distributionListMainNotIncluded.Id).First();
        Assert.True(dbDistributionList.ContactDistributionLists.Count == 1);
    }

    [Fact]
    public async Task CanMergeMessageAndMessageEvent() {

        var contactService = ServiceProvider.GetRequiredService<IContactService>();
        var messageService = ServiceProvider.GetRequiredService<IMessageService>();
        var dbContext = ServiceProvider.GetRequiredService<CampaignsDbContext>();

        Guid campaignId = Guid.NewGuid();
        dbContext.Campaigns.Add(new DbCampaign {
            Id = campaignId,
            Title = "Test Campaign",
        });
        await dbContext.SaveChangesAsync();

        List<CreateContactRequest> createContactRequests = new List<CreateContactRequest>();
        List<Contact> contacts = new List<Contact>();
        for (int i = 0; i < 5; i++) {
            var createContactRequest = CreateNumberedJohnDoeContact(i);
            Contact contact = await contactService.Create(createContactRequest);
            contacts.Add(contact);
        }
        var createMainContactRequest = CreateMainJohnDoeContact();
        var mainContact = await contactService.Create(createMainContactRequest);
        dbContext.SaveChanges();
        var messageIdList = new List<Guid>();
        var messageList = new List<DbMessage>();
        var messageEventList = new List<DbMessageEvent>();
        for (int i = 0; i < 5; i++) {
            var messageRequest = new CreateMessageRequest() {
                ContactId = contacts[i].Id,
                CampaignId = campaignId,
                Content = new MessageContentDictionary(
                new Dictionary<MessageChannelKind, MessageContent> {
                    [MessageChannelKind.Email] = new MessageContent($"Email Test Message", $"Test Message Content: {contacts[i].Email}"),
                }
            ),
            };
            var messageId = await messageService.Create(messageRequest);

            var messageEvent = new DbMessageEvent() {
                Id = new Guid(),
                ContactId = contacts[i].Id.Value,
                CampaignId = new Guid(),
                MessageId = messageId,
            };
            dbContext.MessageEvents.Add(messageEvent);
            messageEventList.Add(messageEvent);
            var message = await dbContext.Messages.FindAsync(messageId);
            messageList.Add(message);
        }
        dbContext.SaveChanges();

        await contactService.MergeContacts(mainContact.Id.Value, contacts.Select(x => x.Id.Value).ToList());
        dbContext.ChangeTracker.Clear();

        Assert.True(dbContext.Messages.All(x => x.ContactId == mainContact.Id) && dbContext.Messages.Count() == 5);
        Assert.True(dbContext.MessageEvents.All(x => x.ContactId == mainContact.Id) && dbContext.MessageEvents.Count() == 5);
    }

    public List<CreateContactRequest> CreateJohnDoeContacts(int numInstances) {
        return new List<CreateContactRequest>();
    }

    public CreateContactRequest CreateNumberedJohnDoeContact(int num) {
        return new CreateContactRequest {
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Email = $"j{num}.doe@indice.gr",
            PhoneNumber = "6955555555",
            Resolved = false,
        };
    }

    public CreateContactRequest CreateMainJohnDoeContact() {
        var createMainContactRequest = new CreateContactRequest {
            RecipientId = Guid.NewGuid().ToString(),
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Email = $"j.doe@indice.gr",
            PhoneNumber = "6955555555",
            Resolved = true,
        };
        return createMainContactRequest;
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
