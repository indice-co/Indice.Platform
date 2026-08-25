using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Indice.AspNetCore.Authorization;
using Indice.Extensions;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Tests.Mocks;
using Indice.Features.Messages.Tests.Security;
using Indice.Serialization;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Tests;

public class MessagesIntegrationTests : IAsyncLifetime
{
    // Constants
    private const string BASE_URL = "https://server";
    // Private fields
    private readonly HttpClient _httpClient;
    private readonly ITestOutputHelper _output;
    private ServiceProvider _serviceProvider = null!;

    public MessagesIntegrationTests(ITestOutputHelper output) {
        _output = output;
        var builder = new WebHostBuilder();
        builder.ConfigureAppConfiguration(builder => {
            builder.AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:MessagesDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=MessagesDb.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true",
                ["ConnectionStrings:StorageConnection"] = "UseDevelopmentStorage=true"
            });
        });
        builder.ConfigureServices(services => {
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            services.AddTransient<IEventDispatcherFactory, DefaultEventDispatcherFactory>();
            services.AddRouting();
            services.AddMessaging(options => {
                        options.PathPrefix = "api";
                        options.ConfigureDbContext = (serviceProvider, dbbuilder) => dbbuilder.UseSqlServer(configuration.GetConnectionString("MessagesDb"));
                        options.DatabaseSchema = "cmp";
                        options.RequiredScope = MessagesApi.Scope;
                        options.UseFilesLocal();
                        options.UseContactResolver<MockContactResolver>();
                    });
            services.AddAuthentication(MockAuthenticationDefaults.AuthenticationScheme)
                    .AddJwtBearer((options) => {
                        options.ForwardDefaultSelector = (httpContext) => MockAuthenticationDefaults.AuthenticationScheme;
                    })
                    .AddMock(() => DummyPrincipals.IndiceUser);
        });
        builder.Configure(app => {
            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(e => e.MapMessaging());
        });
        var server = new TestServer(builder);
        var handler = server.CreateHandler();
        _httpClient = new HttpClient(handler) {
            BaseAddress = new Uri(BASE_URL)
        };
        _serviceProvider = (ServiceProvider)server.Services;
    }

    [Fact]
    public async Task Create_Template_And_Create_Campaign__No_Channels_Specified__Success() {
        //arrange
        string templateAlias = "my dummy template";
        var createTemplateRequest = new CreateTemplateRequest {
            Name = "My Welcome Email", Alias = templateAlias,
            Content = new MessageContentDictionary(
                new Dictionary<MessageChannelKind, MessageContent> {
                    [MessageChannelKind.Email] = new MessageContent("Email Test Message", "Test Message Content: {{data.localization.description_key}}"),
                    [MessageChannelKind.PushNotification] = new MessageContent("Push Test Message", "Test Message Content: {{data.localization.description_key}}"),
                    [MessageChannelKind.SMS] = new MessageContent("SMS Test Message", "Test Message Content: {{data.localization.description_key}}")
                }
            ),
            Data = new {
                localization = new {
                    description_key = "This is a description"
                }
            }
        };
        var createTemplatePayload = JsonSerializer.Serialize(createTemplateRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        using var createTemplateContent = new StringContent(createTemplatePayload, Encoding.UTF8, "application/json");
        var createTemplateResponse = await _httpClient.PostAsync("/api/templates", createTemplateContent, TestContext.Current.CancellationToken);
        createTemplateResponse.EnsureSuccessStatusCode();

        //action
        var createCampaignRequest = new CreateCampaignRequest {
            Title = "Test Campaign",
            ActivePeriod = new Types.Period {
                From = DateTimeOffset.UtcNow,
                To = DateTimeOffset.UtcNow.AddDays(1)
            },
            Published = false,
            RecipientIds = ["6c9fa6dd-ede4-486b-bf91-6de18542da4a"],
            MessageTemplateId = new GuidOrAlias(templateAlias)
        };
        var createCampaignPayload = JsonSerializer.Serialize(createCampaignRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        using var createCampaignContent = new StringContent(createCampaignPayload, Encoding.UTF8, "application/json");
        var createCampaignResponse = await _httpClient.PostAsync("/api/campaigns", createCampaignContent, TestContext.Current.CancellationToken);
        var createCampaignResponseJson = await createCampaignResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!createCampaignResponse.IsSuccessStatusCode) {
            _output.WriteLine(createCampaignResponseJson);
        }

        //assert
        var getCampaignResponse = await _httpClient.GetAsync(createCampaignResponse.Headers.Location?.PathAndQuery, TestContext.Current.CancellationToken);
        var getCampaignResponseJson = await getCampaignResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!getCampaignResponse.IsSuccessStatusCode) {
            _output.WriteLine(getCampaignResponseJson);
        }

        Assert.True(createCampaignResponse.IsSuccessStatusCode);
        Assert.True(getCampaignResponse.IsSuccessStatusCode);

        var serializationOptions = JsonSerializerOptionDefaults.GetDefaultSettings();
        serializationOptions.Converters.Insert(0, new JsonStringArrayEnumFlagsConverterFactory());
        var campaignDetails = JsonSerializer.Deserialize<CampaignDetails>(getCampaignResponseJson, serializationOptions);

        Assert.NotNull(campaignDetails);
        var actualContentMessageKinds = campaignDetails.Content.Select(cnt => cnt.Key);
        var expectedContentMessageKinds = createTemplateRequest.Content.Select(cnt => cnt.Key);
        Assert.Equal(expectedContentMessageKinds.Count(), actualContentMessageKinds.Count());
        Assert.Equal(expectedContentMessageKinds.Count(), expectedContentMessageKinds.Intersect(actualContentMessageKinds).Count());
        Assert.Equal(expectedContentMessageKinds.Count(), campaignDetails.MessageChannelKind.Count());
    }

    [Fact]
    public async Task Create_Template_And_Create_Campaign__Channels_Specified__Success() {
        //arrange
        string templateAlias = "my dummy template";
        var createTemplateRequest = new CreateTemplateRequest {
            Name = "My Welcome Email", Alias = templateAlias,
            Content = new MessageContentDictionary(
                new Dictionary<MessageChannelKind, MessageContent> {
                    [MessageChannelKind.Email] = new MessageContent("Email Test Message", "Test Message Content: {{data.localization.description_key}}"),
                    [MessageChannelKind.PushNotification] = new MessageContent("Push Test Message", "Test Message Content: {{data.localization.description_key}}"),
                    [MessageChannelKind.SMS] = new MessageContent("SMS Test Message", "Test Message Content: {{data.localization.description_key}}")
                }
            ),
            Data = new {
                localization = new {
                    description_key = "This is a description"
                }
            }
        };
        var createTemplatePayload = JsonSerializer.Serialize(createTemplateRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        using var createTemplateContent = new StringContent(createTemplatePayload, Encoding.UTF8, "application/json");
        var createTemplateResponse = await _httpClient.PostAsync("/api/templates", createTemplateContent, TestContext.Current.CancellationToken);
        createTemplateResponse.EnsureSuccessStatusCode();

        //action
        var createCampaignRequest = new CreateCampaignRequest {
            Title = "Test Campaign",
            ActivePeriod = new Types.Period {
                From = DateTimeOffset.UtcNow,
                To = DateTimeOffset.UtcNow.AddDays(1)
            },
            Published = false,
            RecipientIds = ["6c9fa6dd-ede4-486b-bf91-6de18542da4a"],
            MessageTemplateId = new GuidOrAlias(templateAlias),
            MessageTemplateChannels = [MessageChannelKind.Email, MessageChannelKind.PushNotification]
        };
        var createCampaignPayload = JsonSerializer.Serialize(createCampaignRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        using var createCampaignContent = new StringContent(createCampaignPayload, Encoding.UTF8, "application/json");
        var createCampaignResponse = await _httpClient.PostAsync("/api/campaigns", createCampaignContent, TestContext.Current.CancellationToken);
        var createCampaignResponseJson = await createCampaignResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!createCampaignResponse.IsSuccessStatusCode) {
            _output.WriteLine(createCampaignResponseJson);
        }

        //assert
        var getCampaignResponse = await _httpClient.GetAsync(createCampaignResponse.Headers.Location?.PathAndQuery, TestContext.Current.CancellationToken);
        var getCampaignResponseJson = await getCampaignResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!getCampaignResponse.IsSuccessStatusCode) {
            _output.WriteLine(getCampaignResponseJson);
        }

        Assert.True(createCampaignResponse.IsSuccessStatusCode);
        Assert.True(getCampaignResponse.IsSuccessStatusCode);

        var serializationOptions = JsonSerializerOptionDefaults.GetDefaultSettings();
        serializationOptions.Converters.Insert(0, new JsonStringArrayEnumFlagsConverterFactory());
        var campaignDetails = JsonSerializer.Deserialize<CampaignDetails>(getCampaignResponseJson, serializationOptions);

        Assert.NotNull(campaignDetails);
        var actualContentMessageKinds = campaignDetails.Content.Select(cnt => cnt.Key);
        var expectedContentMessageKinds = createCampaignRequest.MessageTemplateChannels.Select(v => v.ToString());
        Assert.Equal(expectedContentMessageKinds.Count(), actualContentMessageKinds.Count());
        Assert.Equal(expectedContentMessageKinds.Count(), expectedContentMessageKinds.Intersect(actualContentMessageKinds).Count());
        Assert.Equal(expectedContentMessageKinds.Count(), campaignDetails.MessageChannelKind.Count());
    }

    [Fact]
    public async Task Create_Template_And_Create_Campaign__Channels_Specified__Subset_Success() {
        //arrange
        string templateAlias = "my dummy template";
        var createTemplateRequest = new CreateTemplateRequest {
            Name = "My Welcome Email", Alias = templateAlias,
            Content = new MessageContentDictionary(
                new Dictionary<MessageChannelKind, MessageContent> {
                    [MessageChannelKind.Email] = new MessageContent("Email Test Message", "Test Message Content: {{data.localization.description_key}}"),
                    [MessageChannelKind.PushNotification] = new MessageContent("Push Test Message", "Test Message Content: {{data.localization.description_key}}"),
                    [MessageChannelKind.SMS] = new MessageContent("SMS Test Message", "Test Message Content: {{data.localization.description_key}}")
                }
            ),
            Data = new {
                localization = new {
                    description_key = "This is a description"
                }
            }
        };
        var createTemplatePayload = JsonSerializer.Serialize(createTemplateRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        using var createTemplateHttpContent = new StringContent(createTemplatePayload, Encoding.UTF8, "application/json");
        var createTemplateResponse = await _httpClient.PostAsync("/api/templates", createTemplateHttpContent, TestContext.Current.CancellationToken);
        createTemplateResponse.EnsureSuccessStatusCode();

        //action
        var createCampaignRequest = new CreateCampaignRequest {
            Title = "Test Campaign",
            ActivePeriod = new Types.Period {
                From = DateTimeOffset.UtcNow,
                To = DateTimeOffset.UtcNow.AddDays(1)
            },
            Published = false,
            RecipientIds = ["6c9fa6dd-ede4-486b-bf91-6de18542da4a"],
            MessageTemplateId = new GuidOrAlias(templateAlias),
            MessageTemplateChannels = [MessageChannelKind.Email, MessageChannelKind.Inbox]
        };
        var createCampaignPayload = JsonSerializer.Serialize(createCampaignRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        using var createCampaignHttpContent = new StringContent(createCampaignPayload, Encoding.UTF8, "application/json");
        var createCampaignResponse = await _httpClient.PostAsync("/api/campaigns", createCampaignHttpContent, TestContext.Current.CancellationToken);
        var createCampaignResponseJson = await createCampaignResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!createCampaignResponse.IsSuccessStatusCode) {
            _output.WriteLine(createCampaignResponseJson);
        }

        //assert
        var getCampaignResponse = await _httpClient.GetAsync(createCampaignResponse.Headers.Location?.PathAndQuery, TestContext.Current.CancellationToken);
        var getCampaignResponseJson = await getCampaignResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!getCampaignResponse.IsSuccessStatusCode) {
            _output.WriteLine(getCampaignResponseJson);
        }

        Assert.True(createCampaignResponse.IsSuccessStatusCode);
        Assert.True(getCampaignResponse.IsSuccessStatusCode);

        var serializationOptions = JsonSerializerOptionDefaults.GetDefaultSettings();
        serializationOptions.Converters.Insert(0, new JsonStringArrayEnumFlagsConverterFactory());
        var campaignDetails = JsonSerializer.Deserialize<CampaignDetails>(getCampaignResponseJson, serializationOptions);

        Assert.NotNull(campaignDetails);
        var actualContentMessageKinds = campaignDetails.Content.Select(cnt => cnt.Key);
        var expectedContentMessageKinds = new string[] { MessageChannelKind.Email.ToString() };
        Assert.Equal(expectedContentMessageKinds.Count(), actualContentMessageKinds.Count());
        Assert.Equal(expectedContentMessageKinds.Count(), expectedContentMessageKinds.Intersect(actualContentMessageKinds).Count());
        Assert.Equal(expectedContentMessageKinds.Count(), campaignDetails.MessageChannelKind.Count());
    }

    [Fact]
    public async Task Create_Template_And_Create_Campaign__Channels_Specified__No_Intersection_Failure() {
        //arrange
        string templateAlias = "my dummy template";
        var createTemplateRequest = new CreateTemplateRequest {
            Name = "My Welcome Email", Alias = templateAlias,
            Content = new MessageContentDictionary(
                new Dictionary<MessageChannelKind, MessageContent> {
                    [MessageChannelKind.Email] = new MessageContent("Email Test Message", "Test Message Content: {{data.localization.description_key}}"),
                    [MessageChannelKind.PushNotification] = new MessageContent("Push Test Message", "Test Message Content: {{data.localization.description_key}}")
                }
            ),
            Data = new {
                localization = new {
                    description_key = "This is a description"
                }
            }
        };
        var createTemplatePayload = JsonSerializer.Serialize(createTemplateRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        using var createTemplateHttpContent = new StringContent(createTemplatePayload, Encoding.UTF8, "application/json");
        var createTemplateResponse = await _httpClient.PostAsync("/api/templates", createTemplateHttpContent, TestContext.Current.CancellationToken);
        createTemplateResponse.EnsureSuccessStatusCode();

        //action
        var createCampaignRequest = new CreateCampaignRequest {
            Title = "Test Campaign",
            ActivePeriod = new Types.Period {
                From = DateTimeOffset.UtcNow,
                To = DateTimeOffset.UtcNow.AddDays(1)
            },
            Published = false,
            RecipientIds = ["6c9fa6dd-ede4-486b-bf91-6de18542da4a"],
            MessageTemplateId = new GuidOrAlias(templateAlias),
            MessageTemplateChannels = [MessageChannelKind.Inbox, MessageChannelKind.SMS]
        };
        var createCampaignPayload = JsonSerializer.Serialize(createCampaignRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        using var createCampaignHttpContent = new StringContent(createCampaignPayload, Encoding.UTF8, "application/json");
        var createCampaignResponse = await _httpClient.PostAsync("/api/campaigns", createCampaignHttpContent, TestContext.Current.CancellationToken);
        var createCampaignResponseJson = await createCampaignResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!createCampaignResponse.IsSuccessStatusCode) {
            _output.WriteLine(createCampaignResponseJson);
        }

        //assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, createCampaignResponse.StatusCode);
        Assert.Contains($"Content was empty after applying the messageTemplateChannels to the selected Template with Id:({templateAlias})", createCampaignResponseJson);
    }

    [Fact]
    public async Task Create_And_Retrieve_Campaign_By_Location_Header_Success() {
        //Create the Campaign
        var createCampaignRequest = new CreateCampaignRequest {
            Title = "Test Campaign",
            ActivePeriod = new Types.Period {
                From = DateTimeOffset.UtcNow,
                To = DateTimeOffset.UtcNow.AddDays(1)
            },
            Published = false,
            RecipientIds = [ "6c9fa6dd-ede4-486b-bf91-6de18542da4a" ],
            Content = new MessageContentDictionary(
                new Dictionary<MessageChannelKind, MessageContent> {
                    [MessageChannelKind.Inbox] = new MessageContent("Test Message", "Test Message Content")
                }
            )
        };
        var payload = JsonSerializer.Serialize(createCampaignRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        var createCampaignResponse = await _httpClient.PostAsync("/api/campaigns", new StringContent(payload, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        var createCampaignResponseJson = await createCampaignResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!createCampaignResponse.IsSuccessStatusCode) {
            _output.WriteLine(createCampaignResponseJson);
        }

        //Retrieve the Created Campaign
        var getCampaignResponse = await _httpClient.GetAsync(createCampaignResponse.Headers.Location?.PathAndQuery, TestContext.Current.CancellationToken);
        var getCampaignResponseJson = await getCampaignResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!getCampaignResponse.IsSuccessStatusCode) {
            _output.WriteLine(getCampaignResponseJson);
        }

        Assert.True(createCampaignResponse.IsSuccessStatusCode);
        Assert.True(getCampaignResponse.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Create_Campaign_With_No_Content_Fail() {
        //Create Campaign
        var createCampaignRequest = new CreateCampaignRequest {
            Title = "Test Campaign",
            ActivePeriod = new Types.Period {
                From = DateTimeOffset.UtcNow,
                To = DateTimeOffset.UtcNow.AddDays(1)
            },
            Published = false,
            RecipientIds = ["6c9fa6dd-ede4-486b-bf91-6de18542da4a"]
        };
        var payload = JsonSerializer.Serialize(createCampaignRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        var createCampaignResponse = await _httpClient.PostAsync("/api/campaigns", new StringContent(payload, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        Assert.False(createCampaignResponse.IsSuccessStatusCode);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, createCampaignResponse.StatusCode);
    }

    [Fact]
    public async Task BulkAddToDistributionList_ExistingContactByRecipientId_AddsIfNotExists() {
        //Create distribution list
        var createDistributionListRequest = new CreateDistributionListRequest {
            Name = "Test-Distribution-List"
        };
        var createDistributionListPayload = JsonSerializer.Serialize(createDistributionListRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        var createDistributionListResponse = await _httpClient.PostAsync("/api/distribution-lists", new StringContent(createDistributionListPayload, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        var createDistributionListResponseJson = await createDistributionListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!createDistributionListResponse.IsSuccessStatusCode) {
            _output.WriteLine(createDistributionListResponseJson);
        }

        var distributionListLocation = createDistributionListResponse.Headers.Location;
        var distributionListId = Guid.Parse(distributionListLocation!.Segments.Last());

        // Generate import request
        var csvLines = new[]
        {
            "RecipientId,Salutation,FirstName,LastName,FullName,Email,PhoneNumber,Locale",
            "ABC123,Mr,John,Doe,John Doe,test@example.com,1234567890,en-US"
        };
        var csvContent = string.Join("\n", csvLines);
        var csvBytes = Encoding.UTF8.GetBytes(csvContent);
        var byteArrayContent = new ByteArrayContent(csvBytes);
        byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        var form = new MultipartFormDataContent {
            { byteArrayContent, "File", "contacts.csv" }
        };

        var response = await _httpClient.PostAsync($"{createDistributionListResponse.Headers.Location?.PathAndQuery}/import", form, TestContext.Current.CancellationToken);
        Assert.True(response.IsSuccessStatusCode);

        var context = _serviceProvider.GetRequiredService<CampaignsDbContext>();
        var contactInDb = await context.Contacts.FirstOrDefaultAsync(c => c.RecipientId == "ABC123", cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(contactInDb);
        Assert.Equal("test@example.com", contactInDb!.Email);

        var link = await context.ContactDistributionLists
            .FirstOrDefaultAsync(x => x.DistributionListId == distributionListId && x.ContactId == contactInDb.Id, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(link);
    }

    [Fact]
    public async Task BulkAddToDistributionList_ExistingContactByEmailWithoutRecipientId_UpdatesContact() {
        var context = _serviceProvider.GetRequiredService<CampaignsDbContext>();
        var list = new DbDistributionList { Id = Guid.NewGuid(), Name = "test-list", CreatedBy = "user1" };
        var contact = new DbContact {
            Email = "match@example.com",
            FirstName = "Old",
            LastName = "Name",
            RecipientId = null
        };
        context.DistributionLists.Add(list);
        context.Contacts.Add(contact);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var link = new DbDistributionListContact {
            ContactId = contact.Id,
            DistributionListId = list.Id
        };
        context.ContactDistributionLists.Add(link);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var requests = new List<CreateDistributionListContactRequest>
        {
            new()
            {
                RecipientId = null,
                Email = "match@example.com",
                FirstName = "Updated",
                LastName = "Name",
                PhoneNumber = "1234567890"
            }
        };

        var service = _serviceProvider.GetRequiredService<IContactService>();
        await service.BulkAddToDistributionList(list.Id, requests);

        var updated = await context.Contacts.FirstOrDefaultAsync(c => c.Email == "match@example.com", cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated!.FirstName);
        Assert.Equal("1234567890", updated.PhoneNumber);
    }

    [Fact]
    public async Task BulkAddToDistributionList_ExistingContactByRecipientId_AlreadyLinked_SkipsAdd() {
        var context = _serviceProvider.GetRequiredService<CampaignsDbContext>();
        var list = new DbDistributionList { Id = Guid.NewGuid(), Name = "test-list2", CreatedBy = "user1" };
        var contact = new DbContact {
            RecipientId = "EXIST123",
            Email = "exist@example.com",
            FirstName = "Already",
            LastName = "There"
        };
        context.DistributionLists.Add(list);
        context.Contacts.Add(contact);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var link = new DbDistributionListContact {
            ContactId = contact.Id,
            DistributionListId = list.Id
        };
        context.ContactDistributionLists.Add(link);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var requests = new List<CreateDistributionListContactRequest>
        {
            new()
            {
                RecipientId = "EXIST123",
                Email = "exist@example.com",
                FirstName = "Should",
                LastName = "BeIgnored"
            }
        };

        var service = _serviceProvider.GetRequiredService<IContactService>();
        await service.BulkAddToDistributionList(list.Id, requests);

        var duplicates = await context.ContactDistributionLists
            .CountAsync(x => x.ContactId == contact.Id && x.DistributionListId == list.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, duplicates);
    }

    [Fact]
    public async Task Create_Distribution_List_And_Add_Contacts_With_Comminication_Preferences_Success() {
        //Create Distribution List
        var createDistributionListRequest = new CreateDistributionListRequest {
            Name = "Test Distribution List"
        };
        var createDistributionListPayload = JsonSerializer.Serialize(createDistributionListRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        var createDistributionListResponse = await _httpClient.PostAsync("/api/distribution-lists", new StringContent(createDistributionListPayload, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        var createDistributionListResponseJson = await createDistributionListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!createDistributionListResponse.IsSuccessStatusCode) {
            _output.WriteLine(createDistributionListResponseJson);
        }

        //Add Contact to Distribution List
        var addContactRequest = new CreateDistributionListContactRequest {
            FirstName = "First Name",
            LastName = "Last Name",
            FullName = "Full Name",
            Email = "test@email.gr",
            PhoneNumber = "1234567890",
            Salutation = "Mr",
            //CommunicationPreferences = ContactChannelKind.Any | ContactChannelKind.Email
        };
        var addContactPayload = JsonSerializer.Serialize(addContactRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        var addContactResponse = await _httpClient.PostAsync($"{createDistributionListResponse.Headers.Location?.PathAndQuery}/contacts", new StringContent(addContactPayload, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        var addContactResponseJson = await addContactResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!addContactResponse.IsSuccessStatusCode) {
            _output.WriteLine(addContactResponseJson);
        }

        //Retrieve the Distribution List

        var serializationOptions = JsonSerializerOptionDefaults.GetDefaultSettings();
        serializationOptions.Converters.Insert(0, new JsonStringArrayEnumFlagsConverterFactory());
        var getDistributionListResponse = await _httpClient.GetAsync($"{createDistributionListResponse.Headers.Location?.PathAndQuery}/contacts", TestContext.Current.CancellationToken);
        var getDistributionListResponseJson = await getDistributionListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!getDistributionListResponse.IsSuccessStatusCode) {
            _output.WriteLine(getDistributionListResponseJson);
        }
        var distributionListContacts = JsonSerializer.Deserialize<ResultSet<Contact>>(getDistributionListResponseJson, serializationOptions)!;

        Assert.True(createDistributionListResponse.IsSuccessStatusCode);
        Assert.True(addContactResponse.IsSuccessStatusCode);
        Assert.NotEmpty(distributionListContacts.Items);
        Assert.Single(distributionListContacts.Items, i => i.Email == addContactRequest.Email);
    }

    [Fact]
    public async Task Create_Distribution_List_And_Add_Contacts_Success() {
        //Create Distribution List
        var createDistributionListRequest = new CreateDistributionListRequest {
            Name = "Test Distribution List"
        };
        var createDistributionListPayload = JsonSerializer.Serialize(createDistributionListRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        var createDistributionListResponse = await _httpClient.PostAsync("/api/distribution-lists", new StringContent(createDistributionListPayload, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        var createDistributionListResponseJson = await createDistributionListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!createDistributionListResponse.IsSuccessStatusCode) {
            _output.WriteLine(createDistributionListResponseJson);
        }

        //Add Contact to Distribution List
        var addContactRequest = new CreateDistributionListContactRequest {
            FirstName = "First Name",
            LastName = "Last Name",
            FullName = "Full Name",
            Email = "test@email.gr",
            PhoneNumber = "1234567890",
            Salutation = "Mr"
        };
        var addContactPayload = JsonSerializer.Serialize(addContactRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        var addContactResponse = await _httpClient.PostAsync($"{createDistributionListResponse.Headers.Location?.PathAndQuery}/contacts", new StringContent(addContactPayload, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        var addContactResponseJson = await addContactResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!addContactResponse.IsSuccessStatusCode) {
            _output.WriteLine(addContactResponseJson);
        }

        //Retrieve the Distribution List

        var serializationOptions = JsonSerializerOptionDefaults.GetDefaultSettings();
        serializationOptions.Converters.Insert(0, new JsonStringArrayEnumFlagsConverterFactory());
        var getDistributionListResponse = await _httpClient.GetAsync($"{createDistributionListResponse.Headers.Location?.PathAndQuery}/contacts", TestContext.Current.CancellationToken);
        var getDistributionListResponseJson = await getDistributionListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!getDistributionListResponse.IsSuccessStatusCode) {
            _output.WriteLine(getDistributionListResponseJson);
        }
        var distributionListContacts = JsonSerializer.Deserialize<ResultSet<Contact>>(getDistributionListResponseJson, serializationOptions)!;

        Assert.True(createDistributionListResponse.IsSuccessStatusCode);
        Assert.True(addContactResponse.IsSuccessStatusCode);
        Assert.NotEmpty(distributionListContacts.Items);
        Assert.Single(distributionListContacts.Items, i => i.Email == addContactRequest.Email);
    }

    [Fact]
    public async Task Create_And_Retrieve_Template_Success() {
        //Create the Campaign
        var createTemplateRequest = new CreateTemplateRequest {
            Name = "My Welcome Email",
            Content = new MessageContentDictionary(
                new Dictionary<MessageChannelKind, MessageContent> {
                    [MessageChannelKind.Email] = new MessageContent("Test Message", "Test Message Content: {{data.localization.description_key}}")
                }
            ),
            Data = new {
                localization = new {
                    description_key = "This is a description"
                }
            }
        };
        var payload = JsonSerializer.Serialize(createTemplateRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        var createTemplateResponse = await _httpClient.PostAsync("/api/templates", new StringContent(payload, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        var createCampaignResponseJson = await createTemplateResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!createTemplateResponse.IsSuccessStatusCode) {
            _output.WriteLine(createCampaignResponseJson);
        }

        //Retrieve the Created Campaign
        var getTemplateResponse = await _httpClient.GetAsync(createTemplateResponse.Headers.Location?.PathAndQuery, TestContext.Current.CancellationToken);
        var getTemplateResponseJson = await getTemplateResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!getTemplateResponse.IsSuccessStatusCode) {
            _output.WriteLine(getTemplateResponseJson);
        }

        Assert.True(createTemplateResponse.IsSuccessStatusCode);
        Assert.True(getTemplateResponse.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Create_Campaign_Without_Title_Uses_Template_Name_As_Title() {
        // arrange: create a template whose Name will be used as the campaign Title
        const string templateAlias = "title-fallback-template";
        const string templateName = "My Welcome Email Template";
        var createTemplateRequest = new CreateTemplateRequest {
            Name = templateName,
            Alias = templateAlias,
            Content = new MessageContentDictionary(
                new Dictionary<MessageChannelKind, MessageContent> {
                    [MessageChannelKind.Email] = new MessageContent("Subject", "Body")
                }
            )
        };
        var createTemplatePayload = JsonSerializer.Serialize(createTemplateRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        using var createTemplateContent = new StringContent(createTemplatePayload, Encoding.UTF8, "application/json");
        using var createTemplateResponse = await _httpClient.PostAsync("/api/templates", createTemplateContent, TestContext.Current.CancellationToken);
        Assert.True(createTemplateResponse.IsSuccessStatusCode);

        // act: create a campaign WITHOUT a Title but with a valid MessageTemplateId
        var createCampaignRequest = new CreateCampaignRequest {
            // Title intentionally omitted
            Published = false,
            ActivePeriod = new Period { From = DateTimeOffset.UtcNow },
            RecipientIds = ["6c9fa6dd-ede4-486b-bf91-6de18542da4a"],
            MessageTemplateId = new GuidOrAlias(templateAlias)
        };
        var createCampaignPayload = JsonSerializer.Serialize(createCampaignRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        using var createCampaignContent = new StringContent(createCampaignPayload, Encoding.UTF8, "application/json");
        using var createCampaignResponse = await _httpClient.PostAsync("/api/campaigns", createCampaignContent, TestContext.Current.CancellationToken);
        var createCampaignResponseJson = await createCampaignResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (!createCampaignResponse.IsSuccessStatusCode) {
            _output.WriteLine(createCampaignResponseJson);
        }
        Assert.True(createCampaignResponse.IsSuccessStatusCode);

        // assert: the created campaign's Title is derived from the template Name
        using var getCampaignResponse = await _httpClient.GetAsync(createCampaignResponse.Headers.Location?.PathAndQuery, TestContext.Current.CancellationToken);
        var getCampaignResponseJson = await getCampaignResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(getCampaignResponse.IsSuccessStatusCode);

        var serializationOptions = JsonSerializerOptionDefaults.GetDefaultSettings();
        serializationOptions.Converters.Insert(0, new JsonStringArrayEnumFlagsConverterFactory());
        var campaignDetails = JsonSerializer.Deserialize<CampaignDetails>(getCampaignResponseJson, serializationOptions);

        Assert.NotNull(campaignDetails);
        Assert.Equal(templateName, campaignDetails!.Title);
    }

    [Fact]
    public async Task Create_Campaign_Without_Title_And_Without_Template_Fails() {
        // act: no Title and no MessageTemplateId
        var createCampaignRequest = new CreateCampaignRequest {
            Published = false,
            ActivePeriod = new Period { From = DateTimeOffset.UtcNow },
            RecipientIds = ["6c9fa6dd-ede4-486b-bf91-6de18542da4a"]
        };
        var createCampaignPayload = JsonSerializer.Serialize(createCampaignRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        using var createCampaignContent = new StringContent(createCampaignPayload, Encoding.UTF8, "application/json");
        using var createCampaignResponse = await _httpClient.PostAsync("/api/campaigns", createCampaignContent, TestContext.Current.CancellationToken);

        // assert: the API must NOT crash; it must return a non-success status code
        Assert.False(createCampaignResponse.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Create_Campaign_Without_Title_With_Invalid_Template_Fails() {
        // act: no Title and an unknown MessageTemplateId
        var createCampaignRequest = new CreateCampaignRequest {
            Published = false,
            ActivePeriod = new Period { From = DateTimeOffset.UtcNow },
            RecipientIds = ["6c9fa6dd-ede4-486b-bf91-6de18542da4a"],
            MessageTemplateId = new GuidOrAlias("non-existent-template-alias")
        };
        var createCampaignPayload = JsonSerializer.Serialize(createCampaignRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        using var createCampaignContent = new StringContent(createCampaignPayload, Encoding.UTF8, "application/json");
        using var createCampaignResponse = await _httpClient.PostAsync("/api/campaigns", createCampaignContent, TestContext.Current.CancellationToken);

        // assert: the API must NOT crash; it must return a non-success status code
        Assert.False(createCampaignResponse.IsSuccessStatusCode);
    }

    public async ValueTask InitializeAsync() {
        var db = _serviceProvider.GetRequiredService<CampaignsDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync() {
        var db = _serviceProvider.GetRequiredService<CampaignsDbContext>();
        await db.Database.EnsureDeletedAsync();
        await _serviceProvider.DisposeAsync();
    }
}