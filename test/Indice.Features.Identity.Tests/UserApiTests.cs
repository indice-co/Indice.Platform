#if NET8_0_OR_GREATER
using IdentityModel;
using Indice.AspNetCore.Authorization;
using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Manager;
using Indice.Features.Identity.Tests.Security;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication.OAuth;
using Indice.Security;
using IdentityServer4;

namespace Indice.Features.Identity.Tests;
public class UserApiTests : IAsyncLifetime
{
    // Private fields
    private readonly HttpClient _httpClient;
    private ServiceProvider _serviceProvider;

    public UserApiTests() {
        var builder = new WebHostBuilder();
        builder.ConfigureAppConfiguration(configurationBuilder => {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string> {
                 ["test"] = "test"
             });
        });
        builder.ConfigureServices((context, services) => {
            // configure dependencies
            services.AddDbContext<ExtendedIdentityDbContext<User, Role>>(builder => builder.UseInMemoryDatabase("IdentityDb"));
            services.AddDbContext<ExtendedConfigurationDbContext>(builder => builder.UseInMemoryDatabase("IdentityDb"));
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
            services.AddPlatformEventHandler<UserCreatedEvent, UserCreatedAssetionHanbdler>();
            services.AddEndpointParameterFluentValidation();

            services.AddLogging();
            services.AddSession();
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
            _serviceProvider = app.ApplicationServices as ServiceProvider;
            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();
            app.UseSession();
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
    }

    [Fact(Skip = "Should find what to do with session state management")]
    public async Task CreateUserHandler_ShouldEmit_UserCreatedEvent_WithPopulatedClaims_Test() {

        var response = await _httpClient.PostAsJsonAsync("/api/users", new Server.Manager.Models.CreateUserRequest {
            UserName = "john.doe@indice.gr",
            Email = "john.doe@indice.gr",
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
        
        Assert.True(response.IsSuccessStatusCode);
    }

    public async Task DisposeAsync() {
        await _serviceProvider.DisposeAsync();
    }

    public Task InitializeAsync() {
        return Task.CompletedTask;
    }

    public class UserCreatedAssetionHanbdler : IPlatformEventHandler<UserCreatedEvent>
    {
        public Task Handle(UserCreatedEvent @event, PlatformEventArgs args) {
            args.ThrowOnError = true;
            Assert.Equal(4, @event.User.Claims.Count);
            return Task.CompletedTask;
        }
    }
}
#endif