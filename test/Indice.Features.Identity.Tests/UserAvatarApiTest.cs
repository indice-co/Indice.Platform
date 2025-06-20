using IdentityModel;
using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.ResponseHandling;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Server;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.HttpOverrides;
using Xunit;
using Indice.Security;
using IdentityModel.Client;
using System.Security.Claims;
using System.Net.Http.Headers;
#if NET9_0_OR_GREATER
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Services;
#else
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using Indice.Features.Identity.Core.TokenCreation;
#endif
using TokenResponse = IdentityModel.Client.TokenResponse;

namespace Indice.Features.Identity.Tests;
public class UserAvatarApiTest : IAsyncLifetime
{
    // Private fields
    private readonly HttpClient _httpClient;
    private IServiceProvider _serviceProvider;
    private const string BASE_URL = "https://server";
    private const string CLIENT_ID = "api-user-client";
    private const string CLIENT_SECRET = "JUEKX2XugFv5XrX3";
    private string _identityDatabaseName = $"IdentityDb.Test_{Environment.Version.Major}_{Guid.NewGuid()}";
    private string _signInLogDatabaseName = $"SignInLogDb.Test_{Environment.Version.Major}_{Guid.NewGuid()}";

    public UserAvatarApiTest() {
        var builder = new WebHostBuilder();
        builder.ConfigureAppConfiguration(builder => {
            builder.AddInMemoryCollection(new Dictionary<string, string> {
                ["test:key"] = "20"
            });
        });

        builder.ConfigureServices((ctx, services) => {
            services.AddDbContext<ExtendedIdentityDbContext<User, Role>>(builder => builder.UseInMemoryDatabase(_identityDatabaseName));
            services.AddDbContext<ExtendedConfigurationDbContext>(builder => builder.UseInMemoryDatabase(_identityDatabaseName));
            services.AddTransient<IUserRequirementProvider<User>, UserRequirementProviderNoOp>();
            services.AddIdentity<User, Role>()
                    .AddExtendedUserManager()
                    .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                    .AddExtendedSignInManager()
                    .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                       .AddIdentityMessageDescriber();

            services.AddIdentityServer()
             .AddInMemoryIdentityResources(GetIdentityResources())
                    .AddInMemoryApiScopes(GetApiScopes())
                    .AddInMemoryApiResources(GetApiResources())
                    .AddInMemoryClients(GetClients())
            .AddAspNetIdentity<User>()
            .AddInMemoryPersistedGrants()
            .AddExtendedResourceOwnerPasswordValidator()
            .AddDeveloperSigningCredential(persistKey: false)
            .AddSignInLogs(options => {
                options.UseEntityFrameworkCoreStore(dbBuilder => dbBuilder.UseInMemoryDatabase(_signInLogDatabaseName));
                options.Enable = true;
            });
            services.AddEmailServiceNoop();
            services.AddSmsServiceNoop();
            services.AddSingleton<CallingCodesProvider>();
            services.AddEndpointParameterFluentValidation();
            services.AddTotpServiceFactory(ctx.Configuration);
            services.AddOutputCache();
            services.AddLogging();
            services.AddDefaultPlatformEventService();
            services.AddLocalization()
            .AddRouting()
            .AddAuthorization(authOptions =>
                authOptions.AddPolicy(IdentityEndpoints.Policies.BeUsersWriter, policy => {
                    policy.AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                            .RequireAuthenticatedUser()
                            .RequireAssertion(x => x.User.HasScope(IdentityEndpoints.SubScopes.Users) && x.User.CanReadUsers());
                }))
            .AddAuthentication()
            .AddLocalApi("IdentityServerApiAccessToken", options => options.ExpectedScope = "identity");
            
            services.AddTransient<ITokenResponseGenerator, ExtendedTokenResponseGenerator>();
#if !NET9_0_OR_GREATER
            services.AddTransient<ITokenCreationService, ExtendedTokenCreationService>();
#endif
        });
        builder.Configure(app => {
            app.UseForwardedHeaders(new() {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor
            });
            app.UseIdentityServer();
            app.IdentityStoreSetup();
            _serviceProvider = app.ApplicationServices as ServiceProvider;
            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();
            app.UseOutputCache();
            app.UseEndpoints(routes => {
                var idbuilder = new IdentityServerEndpointRouteBuilder(routes);
                idbuilder.MapMyAccount();
                idbuilder.MapManageUsers();
                idbuilder.MapProfilePictures();
            });   
        });
        var server = new TestServer(builder);
        var handler = server.CreateHandler();
        _serviceProvider = server.Services;
        _httpClient = new HttpClient(handler) {
            BaseAddress = new Uri(BASE_URL)
        };
    }

    [Fact]
    public async Task Upload_Profile_Image_Test() {
        var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var user = new User {
            CreateDate = DateTimeOffset.UtcNow,
            Email = "someone@indice.gr",
            EmailConfirmed = true,
            Id = Guid.NewGuid().ToString(),
            PhoneNumber = "69XXXXXXXX",
            PhoneNumberConfirmed = true,
            UserName = "someone@indice.gr"
        };
        // 1. Create a new user.
        var result = await userManager.CreateAsync(user, password: "xxxxxxx", validatePassword: false);
        if (!result.Succeeded) {
            Assert.Fail($"User could not be created. {string.Join(", ", result.Errors)}");
        }

        using var client = new HttpClient();
        var tokenResponse = await LoginWithPasswordGrant("someone@indice.gr", "xxxxxxx");

        var multipartContent = new MultipartFormDataContent();
        var stream = File.OpenRead("./Images/Profile.jpg");
        multipartContent.Add(new StreamContent(stream), "File", "Profile.jpg");

        _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + tokenResponse.AccessToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));

        var response = await _httpClient.PutAsync("/api/my/account/picture", multipartContent);
        Assert.True(response.IsSuccessStatusCode);
    }



    private async Task<TokenResponse> LoginWithPasswordGrant(string userName, string password, string deviceId = null, string ipAddress = null) {
        var discoveryDocument = await _httpClient.GetDiscoveryDocumentAsync();
        var request = new PasswordTokenRequest {
            Address = discoveryDocument.TokenEndpoint,
            ClientId = CLIENT_ID,
            ClientSecret = CLIENT_SECRET,
            Scope = $"{IdentityServerConstants.StandardScopes.OpenId} {IdentityServerConstants.StandardScopes.Phone} identity",
            UserName = userName,
            Password = password
        };
        if (!string.IsNullOrWhiteSpace(deviceId)) {
            request.Parameters.Add("device_id", deviceId);
        }
        if (!string.IsNullOrWhiteSpace(ipAddress)) {
            request.Headers.Add("X-Forwarded-For", ipAddress);
        }
        return await _httpClient.RequestPasswordTokenAsync(request);
    }
    public class UserCreatedAssetionHanbdler : IPlatformEventHandler<UserCreatedEvent>
    {
        public Task Handle(UserCreatedEvent @event, PlatformEventArgs args) {
            args.ThrowOnError = true;
            Assert.Equal(4, @event.User.Claims.Count);
            return Task.CompletedTask;
        }
    }

    #region IdentityServer Configuration
    private static List<IdentityResource> GetIdentityResources() => new() {
        new IdentityResources.OpenId(),
        new IdentityResources.Phone(),
        new IdentityResources.Email(),
        new IdentityResources.Profile(),
        new IdentityResources.Address()
    };

    private static List<Client> GetClients() => new() {
        new Client {
            ClientId = CLIENT_ID,
            ClientName = "Public/Private key client",
            AccessTokenType = AccessTokenType.Jwt,
            AllowAccessTokensViaBrowser = false,
            AllowedGrantTypes = {
                CustomGrantTypes.DeviceAuthentication,
                GrantType.ClientCredentials,
                GrantType.ResourceOwnerPassword
            },
            ClientSecrets = {
                new (CLIENT_SECRET.ToSha256())
            },
            AllowedScopes = {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Phone,
                "identity"
            },
            RequireConsent = false,
            RequirePkce = false,
            RequireClientSecret = true,
            AllowOfflineAccess = true,
            AlwaysSendClientClaims = true,
            Claims = {
                new ClientClaim(BasicClaimTypes.TrustedDevice, "true", ClaimValueTypes.Boolean),
                new ClientClaim(BasicClaimTypes.MobileClient, "true", ClaimValueTypes.Boolean)
            }
        }
    };


    private static List<ApiScope> GetApiScopes() => new() {
        new ApiScope(name: "identity", displayName: "Scope No. 1", userClaims: new string[] {
            JwtClaimTypes.Email,
            JwtClaimTypes.EmailVerified,
            JwtClaimTypes.FamilyName,
            JwtClaimTypes.GivenName,
            JwtClaimTypes.PhoneNumber,
            JwtClaimTypes.PhoneNumberVerified,
            JwtClaimTypes.Subject
        })

    };

    private static List<ApiResource> GetApiResources() => new() {
        new ApiResource(name: "identity", displayName: "API No. 1") {
            Scopes = { "identity" }
        }
    };
    #endregion

    public async Task DisposeAsync() {
        await Task.CompletedTask;
        //await _serviceProvider.dispo;
    }

    public Task InitializeAsync() {
        var dbContext = _serviceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();
        dbContext.SeedInitialData();
        return Task.CompletedTask;
    }
}