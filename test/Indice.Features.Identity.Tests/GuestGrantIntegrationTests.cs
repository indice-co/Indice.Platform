using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Duende.IdentityModel.Client;
#if NET9_0_OR_GREATER
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
#else
using IdentityServer4.Models;
using IdentityServer4.Validation;
#endif
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Grants;
using Indice.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Indice.Features.Identity.Tests;

public class GuestGrantIntegrationTests
{
    private const string BASE_URL = "https://server";
    private const string CLIENT_ID = "guest-client";
    private const string CLIENT_SECRET = "GbjHTsbpsVcJQZE3";

    private static HttpClient CreateServer(Action<IIdentityServerBuilder>? configureIdentityServer = null) {
        var builder = new WebHostBuilder();
        builder.ConfigureServices(services => {
            var identityServerBuilder = services.AddIdentityServer()
            .AddInMemoryApiScopes([new ApiScope("chat", "Chat API") { UserClaims = { "guest_channel", "given_name", "family_name", "email", "phone_number" } }])
            .AddInMemoryApiResources([new ApiResource("agents", "Agents API") { Scopes = { "chat" }, UserClaims = { "guest_channel", "given_name", "family_name", "email", "phone_number" } }])
                .AddInMemoryClients([
                    new Client {
                        ClientId = CLIENT_ID,
                        ClientName = "Guest client",
                        AllowedGrantTypes = { CustomGrantTypes.Guest },
                        AllowedScopes = { "chat" },
                        ClientSecrets = { new Secret(HashSecret(CLIENT_SECRET)) },
                        AccessTokenLifetime = 600,
                        RequireConsent = false
                    }
                ])
                .AddInMemoryPersistedGrants()
                .AddDeveloperSigningCredential(persistKey: false);
            if (configureIdentityServer is not null) {
                configureIdentityServer(identityServerBuilder);
            } else {
                identityServerBuilder.AddGuestGrantValidator();
            }
            services.AddPushNotificationServiceNoop();
        });
        builder.Configure(app => app.UseIdentityServer());
        var server = new TestServer(builder);
        return new HttpClient(server.CreateHandler()) {
            BaseAddress = new Uri(BASE_URL)
        };
    }

    private static Task<TokenResponse> RequestGuestTokenAsync(HttpClient httpClient, IDictionary<string, string>? extraParameters = null) {
        var parameters = new Parameters {
            { "scope", "chat" }
        };
        if (extraParameters is not null) {
            foreach (var parameter in extraParameters) {
                parameters.Add(parameter.Key, parameter.Value);
            }
        }
        using var tokenRequest = new TokenRequest {
            Address = $"{BASE_URL}/connect/token",
            GrantType = CustomGrantTypes.Guest,
            ClientId = CLIENT_ID,
            ClientSecret = CLIENT_SECRET,
            Parameters = parameters
        };
        return httpClient.RequestTokenAsync(tokenRequest);
    }

    private static string HashSecret(string secret) {
        var bytes = System.Text.Encoding.UTF8.GetBytes(secret);
        return Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(bytes));
    }

    [Fact]
    public async Task Guest_Grant_Without_Sub_Generates_New_Guid_Subject() {
        using var httpClient = CreateServer();
        var response = await RequestGuestTokenAsync(httpClient);
        Assert.False(response.IsError, response.Error);
        var subject = response.Json?.TryGetString("sub");
        Assert.NotNull(subject);
        Assert.True(Guid.TryParse(subject, out _));
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);
        Assert.Equal(subject, token.Claims.First(claim => claim.Type == "sub").Value);
        Assert.Equal(GuestGrantValidator.IdentityProviderName, token.Claims.First(claim => claim.Type == "idp").Value);
        Assert.Contains(token.Claims, claim => claim.Type == "scope" && claim.Value == "chat");
    }

    [Fact]
    public async Task Guest_Grant_Subclass_Validator_Adds_Custom_Claims() {
        using var httpClient = CreateServer(identityServerBuilder => identityServerBuilder.AddGuestGrantValidator<IIdentityServerBuilder, OpinionatedGuestGrantValidator>());
        var response = await RequestGuestTokenAsync(httpClient);
        Assert.False(response.IsError, response.Error);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);
        Assert.Contains(token.Claims, claim => claim.Type == "guest_channel" && claim.Value == "web");
    }

    [Fact]
    public async Task Guest_Grant_Parses_Profile_Parameters() {
        using var httpClient = CreateServer();
        var response = await RequestGuestTokenAsync(httpClient, extraParameters: new Dictionary<string, string> {
            ["given_name"] = "John",
            ["family_name"] = "Doe",
            ["email"] = "john.doe@example.com",
            ["phone_number"] = "+306900000000"
        });
        Assert.False(response.IsError, response.Error);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);
        Assert.Equal("John", token.Claims.First(claim => claim.Type == "given_name").Value);
        Assert.Equal("Doe", token.Claims.First(claim => claim.Type == "family_name").Value);
        Assert.Equal("john.doe@example.com", token.Claims.First(claim => claim.Type == "email").Value);
        Assert.Equal("+306900000000", token.Claims.First(claim => claim.Type == "phone_number").Value);
    }

    private class OpinionatedGuestGrantValidator(IPushNotificationService pushNotificationService, ILogger<GuestGrantValidator> logger) 
        : GuestGrantValidator(pushNotificationService, logger)
    {

        protected override Task<IEnumerable<Claim>> GetClaimsAsync(ExtensionGrantValidationContext context, string subject) =>
            Task.FromResult<IEnumerable<Claim>>([new Claim("guest_channel", "web")]);
    }
}
