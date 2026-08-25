#if NET9_0_OR_GREATER
using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Stores;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Grants;
using Indice.Features.Identity.Core.ImpossibleTravel;
using Indice.Features.Identity.Core.ResponseHandling;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TokenResponse = Duende.IdentityModel.Client.TokenResponse;


namespace Indice.Features.Identity.Tests;

public class MobileSessionIdServerSideSessionsTests : IAsyncLifetime
{
    private readonly HttpClient _httpClient;
    private readonly TestServer _server;
    private readonly ITestOutputHelper _output;
    private IServiceProvider _serviceProvider;
    private string _identityDatabaseName = $"IdentityDb.Test_{Environment.Version.Major}_{Guid.NewGuid()}";
    private string _signInLogDatabaseName = $"SignInLogDb.Test_{Environment.Version.Major}_{Guid.NewGuid()}";
    
    private const string BASE_URL = "https://server";
    private const string CLIENT_ID = "ppk-client";
    private const string CLIENT_SECRET = "JUEKX2XugFv5XrX3";
    public User TestUser { get; set; } = null!;

    public async ValueTask InitializeAsync() {
        TestUser = await InitTestUserAsync();
    }

    public ValueTask DisposeAsync() {
        _httpClient.Dispose();
        _server.Dispose();
        return ValueTask.CompletedTask;
    }
    
    public MobileSessionIdServerSideSessionsTests(ITestOutputHelper output) {
        _output = output;
        var builder = new WebHostBuilder();
        builder.ConfigureAppConfiguration(builder => {
            builder.AddInMemoryCollection(new Dictionary<string, string?> {
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
                    .AddDbContext<ExtendedIdentityDbContext<User, Role>>(builder => builder.UseInMemoryDatabase(_identityDatabaseName));
            services.AddTransient<IUserRequirementProvider<User>, UserRequirementProviderNoOp>();
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
            .AddServerSideSessions()
.AddDeviceAuthentication(options => options.AddUserDeviceStoreEntityFrameworkCore())
.AddDelegationGrantValidator()
.AddExtensionGrantValidator<TotpGrantValidator>()
.AddOtpAuthenticateGrantValidator()
            .AddDeveloperSigningCredential(persistKey: false)
            .AddSignInLogs(options => {
                options.UseEntityFrameworkCoreStore(dbBuilder => dbBuilder.UseInMemoryDatabase(_signInLogDatabaseName));
                options.Enable = true;
                options.ImpossibleTravel.Guard = true;
                options.ImpossibleTravel.AcceptableSpeed = 90d;
                options.ImpossibleTravel.FlowType = ImpossibleTravelFlowType.PromptMfa;
            });
            services.AddTransient<ITokenResponseGenerator, ExtendedTokenResponseGenerator>();
        });
        builder.Configure(app => {
            app.UseForwardedHeaders(new() {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor
            });
            app.UseIdentityServer();
            app.IdentityStoreSetup();
        });
        var server = new TestServer(builder);
        _server = server;
        var handler = server.CreateHandler();
        _serviceProvider = server.Services;
        _httpClient = new HttpClient(handler) {
            BaseAddress = new Uri(BASE_URL)
        };
    }
    
    [Fact]
    public async Task Password_Grant_Issues_MobileSessionId_When_ServerSideSessions_Enabled() {
        var tokenResponse = await LoginWithPasswordGrant(userName: "someone@indice.gr", password: "xxxxxxx");
        Assert.False(string.IsNullOrWhiteSpace(GetSessionId(tokenResponse)));
    }

    [Fact]
    public async Task Mobile_Logins_Do_Not_Create_ServerSide_Sessions() {
        await LoginWithPasswordGrant(userName: "someone@indice.gr", password: "xxxxxxx");
        await LoginWithPasswordGrant(userName: "someone@indice.gr", password: "xxxxxxx");
        var sessionStore = _serviceProvider.GetRequiredService<IServerSideSessionStore>();
        var sessions = await sessionStore.GetSessionsAsync(new SessionFilter { SubjectId = TestUser.Id });
        Assert.Empty(sessions);
    }

    [Fact]
    public async Task Refresh_Preserves_MobileSessionId_When_ServerSideSessions_Enabled() {
        var loginResponse = await LoginWithPasswordGrant(userName: "someone@indice.gr", password: "xxxxxxx", requestOfflineAccess: true);
        var loginSessionId = GetSessionId(loginResponse);
        
        var discoveryDocument = await _httpClient.GetDiscoveryDocumentAsync();
        var refreshResponse = await _httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest {
            Address = discoveryDocument.TokenEndpoint,
            ClientId = CLIENT_ID,
            ClientSecret = CLIENT_SECRET,
            RefreshToken = loginResponse.RefreshToken!
        });
        
        Assert.Equal(loginSessionId, GetSessionId(refreshResponse));
    }

    [Fact]
    public async Task Refresh_Fails_For_Mobile_Client_That_Coordinates_Lifetime_With_User_Session() {
        var loginResponse = await LoginWithPasswordGrant(userName: "someone@indice.gr", password: "xxxxxxx", requestOfflineAccess: true, clientId: "mobile-coordinating-client");
        Assert.False(loginResponse.IsError);
        
        var discoveryDocument = await _httpClient.GetDiscoveryDocumentAsync();
        var refreshResponse = await _httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest {
            Address = discoveryDocument.TokenEndpoint,
            ClientId = "mobile-coordinating-client",
            ClientSecret = CLIENT_SECRET,
            RefreshToken = loginResponse.RefreshToken!
        });
        
        Assert.True(refreshResponse.IsError);
        Assert.Equal("invalid_grant", refreshResponse.Error);
    }
    
    private async Task<TokenResponse> LoginWithPasswordGrant(string userName, string password, string? deviceId = null, string? ipAddress = null, bool requestOfflineAccess = false, string clientId = CLIENT_ID) {
        var discoveryDocument = await _httpClient.GetDiscoveryDocumentAsync();
        var scope = $"{IdentityServerConstants.StandardScopes.OpenId} {IdentityServerConstants.StandardScopes.Phone} scope1";
        if (requestOfflineAccess) {
            scope = $"{scope} {IdentityServerConstants.StandardScopes.OfflineAccess}";
        }
        var request = new PasswordTokenRequest {
            Address = discoveryDocument.TokenEndpoint,
            ClientId = clientId,
            ClientSecret = CLIENT_SECRET,
            Scope = scope,
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
    
    private static string? GetSessionId(TokenResponse tokenResponse) {
        Assert.False(tokenResponse.IsError, tokenResponse.ErrorDescription ?? tokenResponse.Error);
        var accessToken = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(tokenResponse.AccessToken);
        return accessToken.TryGetClaim(BasicClaimTypes.SessionId, out var sessionId) ? sessionId.Value : null;
    }
    
    private static List<Client> GetClients() => new() {
        new Client {
                ClientId = "mobile-coordinating-client",
                CoordinateLifetimeWithUserSession = true, // incompatible with mobile sessions
                ClientName = "Public/Private key client",
                AccessTokenType = AccessTokenType.Jwt,
                AllowAccessTokensViaBrowser = false,
                AllowedGrantTypes = {
                    GrantType.ResourceOwnerPassword,
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
            },
        new Client {
            ClientId = CLIENT_ID,
            ClientName = "Public/Private key client",
            AccessTokenType = AccessTokenType.Jwt,
            AllowAccessTokensViaBrowser = false,
            AllowedGrantTypes = {
                CustomGrantTypes.DeviceAuthentication,
                GrantType.ClientCredentials,
                GrantType.ResourceOwnerPassword,
                CustomGrantTypes.Mfa,
                CustomGrantTypes.Delegation,
                TotpConstants.GrantType.Totp
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
    
    private async Task<User> InitTestUserAsync(string email = "someone@indice.gr", string password = "xxxxxxx", string developerOtp = "123456", bool isAdmin = false) {
        var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var user = new User {
            CreateDate = DateTimeOffset.UtcNow,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            Id = Guid.NewGuid().ToString(),
            PhoneNumber = "69XXXXXXXX",
            PhoneNumberConfirmed = true,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Admin = isAdmin,
        };
        // 1. Create a new user.
        var result = await userManager.CreateAsync(user, password: password, validatePassword: false);
        if (!result.Succeeded) {
            Assert.Fail("User could not be created.");
        }
        await userManager.AddToRoleAsync(user, BasicRoleNames.Developer);
        await userManager.AddClaimAsync(user, new Claim(BasicClaimTypes.DeveloperTotp, developerOtp));
        return user;
    }
}

#endif