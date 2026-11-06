using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Indice.AspNetCore.Http.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Indice.AspNetCore.Tests;

public class OnBehalfOfHttpHandlerTests
{
    private const string TokenClientName = "token-client";
    private const string ApiClientName = "api-client";

    [Fact]
    public async Task DelegationTokens_AreCachedPerSession_ForTheSameSubject() {
        string? currentAccessToken = null;
        string? currentSubjectId = null;
        var tokenRequests = new List<string>();
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddSingleton<OnBehalfOfAccessTokenSelector>(() => currentAccessToken);
        services.AddSingleton<OnBehalfOfSubjectIdSelector>(() => currentSubjectId);
        services.AddHttpClient(TokenClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(async request => {
                    var form = await request.Content!.ReadAsStringAsync();
                    tokenRequests.Add(form);
                    var body = JsonSerializer.Serialize(new {
                        access_token = $"delegated-{tokenRequests.Count}",
                        token_type = "Bearer",
                        expires_in = 3600
                    });
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
                }));
        services.AddHttpClient(ApiClientName, client => client.BaseAddress = new Uri("https://api.example.com"))
                .AddOnBehalfOfTokenHandler(TokenClientName, options => {
                    options.ClientId = "api-client-id";
                    options.ClientSecret = "secret";
                    options.Scope = "api";
                    options.TokenEndpoint = "https://identity.example.com/connect/token";
                })
                .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(request => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent(request.Headers.Authorization?.Parameter ?? string.Empty)
                })));
        using var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        async Task<string> GetDelegatedToken(string? accessToken, string? subjectId) {
            currentAccessToken = accessToken;
            currentSubjectId = subjectId;
            var response = await httpClientFactory.CreateClient(ApiClientName).GetAsync("/resource");
            return await response.Content.ReadAsStringAsync();
        }

        var firstSessionToken = await GetDelegatedToken(CreateUserToken("alice", "session-1"), "alice");
        var secondSessionToken = await GetDelegatedToken(CreateUserToken("alice", "session-2"), "alice");
        var firstSessionTokenAgain = await GetDelegatedToken(CreateUserToken("alice", "session-1"), "alice");

        Assert.NotEqual(firstSessionToken, secondSessionToken);
        Assert.Equal(firstSessionToken, firstSessionTokenAgain);
        Assert.Equal(2, tokenRequests.Count);
        Assert.All(tokenRequests, form => Assert.Contains("grant_type=delegation", form));

        var clientCredentialsToken = await GetDelegatedToken(null, null);
        var clientCredentialsTokenAgain = await GetDelegatedToken(null, null);

        Assert.Equal(clientCredentialsToken, clientCredentialsTokenAgain);
        Assert.Equal(3, tokenRequests.Count);
        Assert.Contains("grant_type=client_credentials", tokenRequests[2]);
    }

    private static string CreateUserToken(string subjectId, string sessionId) => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor {
        Subject = new ClaimsIdentity([new Claim("sub", subjectId), new Claim("sid", sessionId)]),
        Issuer = "https://identity.example.com",
        Expires = DateTime.UtcNow.AddHours(1)
    });

    private class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => handler(request);
    }
}
