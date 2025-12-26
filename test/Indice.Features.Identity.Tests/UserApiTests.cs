using System.Net.Http.Json;
using Indice.AspNetCore.Authorization;
using Indice.Events;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Tests.Security;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Indice.Features.Identity.Tests;
public class UserApiTests : IAsyncLifetime
{
    // Private fields
    private readonly HttpClient _httpClient;
    private ServiceProvider _serviceProvider;

    public UserApiTests() {
        var builder = new WebHostBuilder();
        builder.ConfigureAppConfiguration(configurationBuilder => {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?> {
                 ["test"] = "test"
             });
        });
        builder.ConfigureServices((context, services) => {
            // configure dependencies
            services.AddDbContext<ExtendedIdentityDbContext<User, Role>>(builder => builder.UseInMemoryDatabase("IdentityDb"));
            services.AddDbContext<ExtendedConfigurationDbContext>(builder => builder.UseInMemoryDatabase("IdentityDb"));
            services.AddTransient(sp => new ExtendedIdentityDbContextSeedOptions<User, Role> { InitialUsers = [], CustomRoles = [] });
            // aspnet identity stuff
            services.AddIdentity<User, Role>()
                       .AddExtendedUserManager()
                       .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                       .AddExtendedSignInManager()
                       .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                       .AddExtendedPhoneNumberTokenProvider(context.Configuration)
                       .AddIdentityMessageDescriber();
            // identity server stuff
            services.AddIdentityServer()
                    .AddInMemoryIdentityResources([])
                    .AddInMemoryApiScopes([])
                    .AddInMemoryApiResources([])
                    .AddInMemoryClients([])
                    .AddAspNetIdentity<User>()
                    .AddInMemoryPersistedGrants();
            // indice stuff
            services.AddDefaultPlatformEventService();
            services.AddPlatformEventHandler<UserCreatedEvent, UserCreatedAssertionHanbdler>();
            services.AddEndpointParameterFluentValidation();
            services.AddOutputCache();
            services.AddLogging();
            services.AddLocalization()
                    .AddRouting()
                    .AddAuthorization(authOptions => 
                        authOptions.AddPolicy(IdentityEndpoints.Policies.BeUsersWriter, policy => {
                            policy.AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                                  .RequireAuthenticatedUser()
                                  .RequireAssertion(x => x.User.HasScope(IdentityEndpoints.SubScopes.Users) && x.User.CanReadUsers());
                        }))
                    .AddAuthentication(MockAuthenticationDefaults.AuthenticationScheme)
                    .AddJwtBearer((options) => {
                        options.ForwardDefaultSelector = (httpContext) => MockAuthenticationDefaults.AuthenticationScheme;
                    })
                    .AddMock("IdentityServerApiAccessToken", "LocalApi", () => TestPrincipals.UserWriter);
        });
        builder.Configure(app => {
            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();
            app.UseOutputCache();
            app.UseEndpoints(routes => {
                var idbuilder = new IdentityServerEndpointRouteBuilder(routes);
                idbuilder.MapManageUsers();
            });
        });
        var server = new TestServer(builder);
        var handler = server.CreateHandler();
        _httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://server")
        };
        _serviceProvider = (ServiceProvider)server.Services;
    }

    [Fact]
    public async Task CreateUserHandler_ShouldEmit_UserCreatedEvent_WithPopulatedClaims_Test() {
        var rand = new Random().Next(1, 100);
        var response = await _httpClient.PostAsJsonAsync("/api/users", new Server.Manager.Models.CreateUserRequest {
            UserName = $"john.doe{rand}@indice.gr",
            Email = $"john.doe{rand}@indice.gr",
            Password = "password",
            BypassPasswordValidation = true,
            FirstName = "John",
            LastName = "Doe",
            Claims = [
                new() { Type = "customer_code", Value = "000001" },
                new() { Type = "locale", Value = "el" }
            ],
            Roles = ["Developer"]
        }, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        var responseJson = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, responseJson);
    }

    public async Task DisposeAsync() {
        await _serviceProvider.DisposeAsync();
    }

    public Task InitializeAsync() {
        var dbContext = _serviceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();
        dbContext.SeedInitialData();
        return Task.CompletedTask;
    }

    public class UserCreatedAssertionHanbdler : IPlatformEventHandler<UserCreatedEvent>
    {
        public Task Handle(UserCreatedEvent @event, PlatformEventArgs args) {
            args.ThrowOnError = true;
            Assert.Equal(4, @event.User.Claims.Count);
            return Task.CompletedTask;
        }
    }
}