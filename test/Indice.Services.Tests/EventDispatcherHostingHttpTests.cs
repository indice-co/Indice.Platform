using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.Services.Tests;

public class EventDispatcherHostingHttpTests
{
    private const string TokenEndpoint = "https://idp.test/connect/token";

    [Fact]
    public async Task RaiseEvent_Posts_Raw_Payload_To_Kebab_Case_Queue_With_Bearer_Token() {
        var handler = new FakeHandler();
        var dispatcher = BuildServiceProvider(handler).GetRequiredService<IEventDispatcher>();

        await dispatcher.RaiseEventAsync(new OrderCreated { OrderId = 42, Customer = "Γιάννης" });

        var tokenRequest = Assert.Single(handler.Requests, x => x.Uri.AbsoluteUri == TokenEndpoint);
        Assert.Contains("grant_type=client_credentials", tokenRequest.Body);
        Assert.Contains("client_id=my-client", tokenRequest.Body);
        Assert.Contains("client_secret=my-secret", tokenRequest.Body);
        Assert.Contains("scope=workers%3Aproducer", tokenRequest.Body);

        var request = Assert.Single(handler.Requests, x => x.Uri.AbsoluteUri != TokenEndpoint);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://workers.test/api/workers/queues/order-created/messages", request.Uri.AbsoluteUri);
        Assert.Equal("Bearer token-1", request.Authorization);
        using var body = JsonDocument.Parse(request.Body);
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("visibleAt").ValueKind);
        using var payload = JsonDocument.Parse(body.RootElement.GetProperty("payload").GetString()!);
        Assert.Equal(42, payload.RootElement.GetProperty("orderId").GetInt32());
        Assert.Equal("Γιάννης", payload.RootElement.GetProperty("customer").GetString());
        Assert.False(payload.RootElement.TryGetProperty("payload", out _)); // Not wrapped in an envelope.
    }

    [Fact]
    public async Task RaiseEvent_Uses_Queue_Name_And_Visibility_Timeout() {
        var handler = new FakeHandler();
        var dispatcher = BuildServiceProvider(handler).GetRequiredService<IEventDispatcher>();
        var before = DateTime.UtcNow;

        await dispatcher.RaiseEventAsync(new OrderCreated(), visibilityTimeout: TimeSpan.FromMinutes(10), queueName: "orders/créés");

        var request = Assert.Single(handler.Requests, x => x.Uri.AbsoluteUri != TokenEndpoint);
        Assert.Equal("/api/workers/queues/orders%2Fcr%C3%A9%C3%A9s/messages", request.Uri.AbsolutePath);
        using var body = JsonDocument.Parse(request.Body);
        var visibleAt = body.RootElement.GetProperty("visibleAt").GetDateTime();
        Assert.InRange(visibleAt, before.AddMinutes(10), DateTime.UtcNow.AddMinutes(10));
    }

    [Fact]
    public async Task RaiseEvent_Caches_The_Access_Token() {
        var handler = new FakeHandler();
        var dispatcher = BuildServiceProvider(handler).GetRequiredService<IEventDispatcher>();

        await dispatcher.RaiseEventAsync(new OrderCreated());
        await dispatcher.RaiseEventAsync(new OrderCreated());

        Assert.Single(handler.Requests, x => x.Uri.AbsoluteUri == TokenEndpoint);
        Assert.Equal(2, handler.Requests.Count(x => x.Uri.AbsoluteUri != TokenEndpoint));
    }

    [Fact]
    public async Task RaiseEvent_Caches_ShortLived_Access_Token() {
        var handler = new FakeHandler { TokenLifetimeSeconds = 60 };
        var dispatcher = BuildServiceProvider(handler).GetRequiredService<IEventDispatcher>();

        await dispatcher.RaiseEventAsync(new OrderCreated());
        await dispatcher.RaiseEventAsync(new OrderCreated());

        Assert.Single(handler.Requests, x => x.Uri.AbsoluteUri == TokenEndpoint);
        Assert.Equal(2, handler.Requests.Count(x => x.Uri.AbsoluteUri != TokenEndpoint));
    }

    [Fact]
    public async Task RaiseEvent_Renews_The_Token_And_Retries_Once_On_Unauthorized() {
        var handler = new FakeHandler();
        handler.ApiResponses.Enqueue(HttpStatusCode.Unauthorized);
        var dispatcher = BuildServiceProvider(handler).GetRequiredService<IEventDispatcher>();

        await dispatcher.RaiseEventAsync(new OrderCreated { OrderId = 7 });

        Assert.Equal(2, handler.Requests.Count(x => x.Uri.AbsoluteUri == TokenEndpoint));
        var requests = handler.Requests.Where(x => x.Uri.AbsoluteUri != TokenEndpoint).ToList();
        Assert.Equal(2, requests.Count);
        Assert.Equal("Bearer token-1", requests[0].Authorization);
        Assert.Equal("Bearer token-2", requests[1].Authorization);
        Assert.Equal(requests[0].Body, requests[1].Body);
    }

    [Fact]
    public async Task RaiseEvent_Throws_When_The_Workers_Api_Fails() {
        var handler = new FakeHandler();
        handler.ApiResponses.Enqueue(HttpStatusCode.Forbidden);
        var dispatcher = BuildServiceProvider(handler).GetRequiredService<IEventDispatcher>();

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => dispatcher.RaiseEventAsync(new OrderCreated()));

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
    }

    [Fact]
    public async Task RaiseEvent_Does_Nothing_When_Disabled() {
        var handler = new FakeHandler();
        var dispatcher = BuildServiceProvider(handler, configure: (_, options) => options.Enabled = false).GetRequiredService<IEventDispatcher>();

        await dispatcher.RaiseEventAsync(new OrderCreated());

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task RaiseEvent_Sends_No_Token_When_No_Client_Is_Configured() {
        var handler = new FakeHandler();
        var dispatcher = BuildServiceProvider(handler, configure: (_, options) => options.ClientId = null).GetRequiredService<IEventDispatcher>();

        await dispatcher.RaiseEventAsync(new OrderCreated());

        var request = Assert.Single(handler.Requests);
        Assert.Null(request.Authorization);
    }

    [Fact]
    public async Task Keyed_Dispatcher_Uses_Its_Own_Options() {
        var handler = new FakeHandler();
        var services = CreateServices(handler, name: null, configure: null);
        services.AddEventDispatcherHostingHttp("remote", (_, options) => {
            options.BaseAddress = new Uri("https://other.test/");
            options.PathPrefix = "/jobs/";
            options.TokenEndpoint = "https://idp.test/connect/token";
        });
        UseHandler(services, $"{EventDispatcherHostingHttp.HttpClientName}:remote", handler);
        var dispatcher = services.BuildServiceProvider().GetRequiredKeyedService<IEventDispatcher>("remote");

        await dispatcher.RaiseEventAsync(new OrderCreated());

        var request = Assert.Single(handler.Requests, x => x.Uri.AbsoluteUri != TokenEndpoint);
        Assert.Equal("https://other.test/jobs/queues/order-created/messages", request.Uri.AbsoluteUri);
    }

    private static ServiceProvider BuildServiceProvider(FakeHandler handler, Action<IServiceProvider, EventDispatcherHostingHttpOptions>? configure = null) =>
        CreateServices(handler, EventDispatcherHostingHttp.HttpClientName, configure).BuildServiceProvider();

    private static ServiceCollection CreateServices(FakeHandler handler, string? name, Action<IServiceProvider, EventDispatcherHostingHttpOptions>? configure) {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["General:Authority"] = "https://idp.test",
                ["General:Endpoints:Workers"] = "https://workers.test/api",
                ["General:Api:Secrets:ClientId"] = "my-client",
                ["General:Api:Secrets:ClientSecret"] = "my-secret"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        if (name is not null) {
            services.AddEventDispatcherHostingHttp(configure);
            UseHandler(services, name, handler);
        }
        return services;
    }

    private static void UseHandler(IServiceCollection services, string httpClientName, FakeHandler handler) {
        services.AddHttpClient(httpClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
        services.AddHttpClient($"{httpClientName}:token").ConfigurePrimaryHttpMessageHandler(() => handler);
    }

    private class OrderCreated
    {
        public int OrderId { get; set; }
        public string? Customer { get; set; }
    }

    private record RecordedRequest(HttpMethod Method, Uri Uri, string? Authorization, string Body);

    private class FakeHandler : HttpMessageHandler
    {
        private int _tokens;
        public List<RecordedRequest> Requests { get; } = [];
        public Queue<HttpStatusCode> ApiResponses { get; } = new();
        public int TokenLifetimeSeconds { get; init; } = 3600;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            lock (Requests) {
                Requests.Add(new RecordedRequest(request.Method, request.RequestUri!, request.Headers.Authorization?.ToString(), body));
            }
            if (request.RequestUri!.AbsoluteUri == TokenEndpoint) {
                var token = Interlocked.Increment(ref _tokens);
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent($$"""{"access_token":"token-{{token}}","expires_in":{{TokenLifetimeSeconds}},"token_type":"Bearer"}""")
                };
            }
            var status = ApiResponses.TryDequeue(out var queued) ? queued : HttpStatusCode.Created;
            return new HttpResponseMessage(status) { Content = new StringContent("{}") };
        }

        // The factory owns the handler pipeline; keep the shared fake alive across clients.
        protected override void Dispose(bool disposing) { }
    }
}
