using System;
using System.Text;
using System.Text.Json;
using Indice.AspNetCore.Authorization;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Tests.Mocks;
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
using Xunit;
using Xunit.Abstractions;

namespace Indice.Features.Messages.Tests;

public class MessagesIntegrationTests : IAsyncLifetime
{
    // Constants
    private const string BASE_URL = "https://server";
    // Private fields
    private readonly HttpClient _httpClient;
    private readonly ITestOutputHelper _output;
    private ServiceProvider _serviceProvider;

    public MessagesIntegrationTests(ITestOutputHelper output) {
        _output = output;
        var builder = new WebHostBuilder();
        builder.ConfigureAppConfiguration(builder => {
            builder.AddInMemoryCollection(new Dictionary<string, string> {
                ["ConnectionStrings:MessagesDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=MessagesDb.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true",
                ["ConnectionStrings:StorageConnection"] = "UseDevelopmentStorage=true"
            });
        });
        builder.ConfigureServices(services => {
            var configuration = services.BuildServiceProvider().GetService<IConfiguration>();
            services.AddTransient<IEventDispatcherFactory, DefaultEventDispatcherFactory>();
            services.AddControllers()
                    .AddMessageEndpoints(options => {
                        options.ApiPrefix = "api";
                        options.ConfigureDbContext = (serviceProvider, dbbuilder) => dbbuilder.UseSqlServer(configuration.GetConnectionString("MessagesDb"));
                        options.DatabaseSchema = "cmp";
                        options.RequiredScope = MessagesApi.Scope;
                        options.UseFilesLocal();
                        options.UseContactResolver<MockContactResolver>();
                    });
            services.AddAuthentication(DummyAuthDefaults.AuthenticationScheme)
                    .AddJwtBearer((options) => {
                        options.ForwardDefaultSelector = (httpContext) => DummyAuthDefaults.AuthenticationScheme;
                    })
                    .AddDummy(() => DummyPrincipals.IndiceUser);
            _serviceProvider = services.BuildServiceProvider();
        });
        builder.Configure(app => {
            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(e => e.MapControllers());
        });
        var server = new TestServer(builder);
        var handler = server.CreateHandler();
        _httpClient = new HttpClient(handler) {
            BaseAddress = new Uri(BASE_URL)
        };
    }

    #region Facts
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
            RecipientIds = new List<string> { "6c9fa6dd-ede4-486b-bf91-6de18542da4a" },
            Content = new MessageContentDictionary(
                new Dictionary<MessageChannelKind, MessageContent> {
                    [MessageChannelKind.Inbox] = new MessageContent("Test Message", "Test Message Content")
                }
            )
        };
        var payload = JsonSerializer.Serialize(createCampaignRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        var createCampaignResponse = await _httpClient.PostAsync("/api/campaigns", new StringContent(payload, Encoding.UTF8, "application/json"));
        var createCampaignResponseJson = await createCampaignResponse.Content.ReadAsStringAsync();
        if (!createCampaignResponse.IsSuccessStatusCode) {
            _output.WriteLine(createCampaignResponseJson);
        }

        //Retrieve the Created Campaign
        var getCampaignResponse = await _httpClient.GetAsync(createCampaignResponse.Headers.Location.PathAndQuery);
        var getCampaignResponseJson = await getCampaignResponse.Content.ReadAsStringAsync();
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
        var createCampaignResponse = await _httpClient.PostAsync("/api/campaigns", new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.False(createCampaignResponse.IsSuccessStatusCode);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, createCampaignResponse.StatusCode);
    }

    [Fact]
    public async Task Create_Distribution_List_And_Add_Contacts_Success() {
        //Create Distribution List
        var createDistributionListRequest = new CreateDistributionListRequest {
            Name = "Test Distribution List"
        };
        var createDistributionListPayload = JsonSerializer.Serialize(createDistributionListRequest, JsonSerializerOptionDefaults.GetDefaultSettings());
        var createDistributionListResponse = await _httpClient.PostAsync("/api/distribution-lists", new StringContent(createDistributionListPayload, Encoding.UTF8, "application/json"));
        var createDistributionListResponseJson = await createDistributionListResponse.Content.ReadAsStringAsync();
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
        var addContactResponse = await _httpClient.PostAsync($"{createDistributionListResponse.Headers.Location.PathAndQuery}/contacts", new StringContent(addContactPayload, Encoding.UTF8, "application/json"));
        var addContactResponseJson = await addContactResponse.Content.ReadAsStringAsync();
        if (!addContactResponse.IsSuccessStatusCode) {
            _output.WriteLine(addContactResponseJson);
        }

        //Retrieve the Distribution List
        var getDistributionListResponse = await _httpClient.GetAsync($"{createDistributionListResponse.Headers.Location.PathAndQuery}/contacts");
        var getDistributionListResponseJson = await getDistributionListResponse.Content.ReadAsStringAsync();
        if (!getDistributionListResponse.IsSuccessStatusCode) {
            _output.WriteLine(getDistributionListResponseJson);
        }
        var distributionListContacts = JsonSerializer.Deserialize<ResultSet<Contact>>(getDistributionListResponseJson, JsonSerializerOptionDefaults.GetDefaultSettings());

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
        var createTemplateResponse = await _httpClient.PostAsync("/api/templates", new StringContent(payload, Encoding.UTF8, "application/json"));
        var createCampaignResponseJson = await createTemplateResponse.Content.ReadAsStringAsync();
        if (!createTemplateResponse.IsSuccessStatusCode) {
            _output.WriteLine(createCampaignResponseJson);
        }

        //Retrieve the Created Campaign
        var getTemplateResponse = await _httpClient.GetAsync(createTemplateResponse.Headers.Location.PathAndQuery);
        var getTemplateResponseJson = await getTemplateResponse.Content.ReadAsStringAsync();
        if (!getTemplateResponse.IsSuccessStatusCode) {
            _output.WriteLine(getTemplateResponseJson);
        }

        Assert.True(createTemplateResponse.IsSuccessStatusCode);
        Assert.True(getTemplateResponse.IsSuccessStatusCode);
    }
    #endregion

    public async Task InitializeAsync() {
        var db = _serviceProvider.GetRequiredService<CampaignsDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() {
        var db = _serviceProvider.GetRequiredService<CampaignsDbContext>();
        await db.Database.EnsureDeletedAsync();
        await _serviceProvider.DisposeAsync();
    }
}
