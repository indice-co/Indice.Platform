using System.Security.Claims;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Events;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;
using ApiResource = IdentityServer4.Models.ApiResource;
using ApiScope = IdentityServer4.Models.ApiScope;
using Client = IdentityServer4.Models.Client;
using ClientClaim = IdentityServer4.Models.ClientClaim;
using IdentityResource = IdentityServer4.Models.IdentityResource;
using Secret = IdentityServer4.Models.Secret;

namespace Indice.Features.Identity.Tests;

public class ExtendedUserManagerTests
{
    private IServiceProvider _serviceProvider;
    private const string IDENTITY_DATABASE_NAME = "IdentityDb";
    private const string BASE_URL = "https://server";
    private const string CLIENT_ID = "ppk-client";
    private const string CLIENT_SECRET = "JUEKX2XugFv5XrX3";
    private readonly HttpClient _httpClient;
    private readonly ITestOutputHelper _output;

    private readonly Mock<IPlatformEventService> _mockPlatformEventService;

    public ExtendedUserManagerTests(ITestOutputHelper output) {
        _output = output;
        var builder = new WebHostBuilder();

        _mockPlatformEventService = new Mock<IPlatformEventService>();

        _mockPlatformEventService
            .Setup(x => x.Publish(It.IsAny<EmailChangedEvent>()))
            .Returns(Task.CompletedTask);

        _mockPlatformEventService
            .Setup(x => x.Publish(It.IsAny<PhoneNumberChangedEvent>()))
            .Returns(Task.CompletedTask);

        builder.ConfigureAppConfiguration(builder => {
            builder.AddInMemoryCollection(new Dictionary<string, string> {
                ["IdentityOptions:User:Devices:DefaultAllowedRegisteredDevices"] = "20",
                ["IdentityOptions:User:Devices:MaxAllowedRegisteredDevices"] = "40",
                ["IdentityOptions:User:Devices:RequirePasswordAfterUserUpdate"] = "true",
                ["Totp:EnableDeveloperTotp"] = "true"
            });
        });
        builder.ConfigureServices((ctx, services) => {
            services.AddTotpServiceFactory(ctx.Configuration)
                    .AddSmsServiceNoop()
                    .AddPushNotificationServiceNoop()
                    .AddLocalization()
                    .AddDbContext<ExtendedIdentityDbContext<User, Role>>(builder => builder.UseInMemoryDatabase(IDENTITY_DATABASE_NAME));
            services.AddTransient<IUserStateProvider<User>, UserStateProviderNoop>();
            services.AddTransient<IPlatformEventService>(_ => _mockPlatformEventService.Object);
            services.AddIdentity<User, Role>()
                    .AddExtendedUserManager()
                    .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                    .AddExtendedSignInManager()
                    .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                    .AddExtendedPhoneNumberTokenProvider(ctx.Configuration);
            services.AddIdentityServer(options => {
                options.EmitStaticAudienceClaim = true;
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;
            })
            .AddInMemoryIdentityResources(GetIdentityResources())
            .AddInMemoryApiScopes(GetApiScopes())
            .AddInMemoryApiResources(GetApiResources())
            .AddInMemoryClients(GetClients())
            .AddAspNetIdentity<User>()
            .AddInMemoryPersistedGrants()
            .AddExtendedResourceOwnerPasswordValidator()
            .AddDeviceAuthentication(options => options.AddUserDeviceStoreEntityFrameworkCore())
            .AddDeveloperSigningCredential(persistKey: false);
        });
        builder.Configure(app => {
            app.UseForwardedHeaders(new() {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor
            });
            app.UseIdentityServer();
            app.IdentityStoreSetup();
        });
        var server = new TestServer(builder);
        var handler = server.CreateHandler();
        _serviceProvider = server.Services;
        _httpClient = new HttpClient(handler) {
            BaseAddress = new Uri(BASE_URL)
        };
    }

    [Fact]
    public async Task EmailChangedEventPublished() {
        var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var expectedInvocations = Times.Once;

        var userId = "ab9769f1-d532-4b7d-9922-3da003157ebd";
        var newEmail = "company2@indice.gr";

        var user = await userManager.FindByIdAsync(userId);
        await userManager.SetEmailAsync(user, newEmail);

        _mockPlatformEventService.Verify(x => x.Publish(It.IsAny<EmailChangedEvent>()), expectedInvocations);
    }

    [Fact]
    public async Task PhoneNumberChangedEventPublished() {
        var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var expectedInvocations = Times.Once;

        var userId = "ab9769f1-d532-4b7d-9922-3da003157ebd";
        var newPhone = "6990000000";

        var user = await userManager.FindByIdAsync(userId);
        var x = await userManager.SetPhoneNumberAsync(user, newPhone);

        _mockPlatformEventService.Verify(x => x.Publish(It.IsAny<PhoneNumberChangedEvent>()), expectedInvocations);
    }

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
                new Secret(CLIENT_SECRET.ToSha256())
            },
            AllowedScopes = {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Phone,
                "scope1"
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

    private static List<IdentityResource> GetIdentityResources() => new() {
        new IdentityResources.OpenId(),
        new IdentityResources.Phone(),
        new IdentityResources.Email(),
        new IdentityResources.Profile(),
        new IdentityResources.Address()
    };

    private static List<ApiScope> GetApiScopes() => new() {
        new ApiScope(name: "scope1", displayName: "Scope No. 1", userClaims: new string[] {
            JwtClaimTypes.Email,
            JwtClaimTypes.EmailVerified,
            JwtClaimTypes.FamilyName,
            JwtClaimTypes.GivenName,
            JwtClaimTypes.PhoneNumber,
            JwtClaimTypes.PhoneNumberVerified,
            JwtClaimTypes.Subject
        }),
        new ApiScope(name: "scope2", displayName: "Scope No. 2", userClaims: new string[] {
            JwtClaimTypes.Email,
            JwtClaimTypes.PhoneNumber,
            JwtClaimTypes.Subject
        })
    };

    private static List<ApiResource> GetApiResources() => new() {
        new ApiResource(name: "api1", displayName: "API No. 1") {
            Scopes = { "scope1", "scope2" }
        }
    };
}
