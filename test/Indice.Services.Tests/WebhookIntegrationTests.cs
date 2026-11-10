using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using Indice.AspNetCore.Features.Webhooks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Indice.Services.Tests;

public sealed class WebhookIntegrationTests
{
    [Fact]
    public async Task ApifonWebhook_WithWebhooksEnabled() {
        var webhookSecret = "Tx43Joi7HFw8k9K2Yqa1DPy5t6W0Zsm7";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Webhooks:Enabled"] = true.ToString(),
                ["Webhooks:ChannelCapacity"] = "100",
                ["Sms:Provider"] = "apifon",
                ["Sms:WebhookSecret"] = webhookSecret
            })
            .Build();

        using var host = await StartHostAsync(configuration);

        var services = host.Services;

        Assert.NotNull(
            services.GetRequiredService<Channel<List<WebhookEvent>>>());

        Assert.NotNull(
            services.GetRequiredKeyedService<IWebhookHttpHandler>("apifon"));

        Assert.Contains(
            services.GetServices<IHostedService>(),
            service => service.GetType() == typeof(WebhookEventSink));


        var request = GetValidApifonPayload(webhookSecret);
        using var response = await host.GetTestClient().SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task VonageWebhook_WithWebhooksEnabled() {
        var webhookSecret = "Tx43Joi7HFw8k9K2Yqa1DPy5t6W0Zsm7";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Webhooks:Enabled"] = true.ToString(),
                ["Webhooks:ChannelCapacity"] = "100",
                ["Sms:Provider"] = "vonage",
                ["Sms:SignatureSecret"] = webhookSecret,
                ["Sms:SignatureMethod"] = "hmac-sha256"
            })
            .Build();

        using var host = await StartHostAsync(configuration);
        var services = host.Services;

        Assert.NotNull(
            services.GetRequiredService<Channel<List<WebhookEvent>>>());

        Assert.NotNull(
            services.GetRequiredKeyedService<IWebhookHttpHandler>("vonage"));

        Assert.Contains(
            services.GetServices<IHostedService>(),
            service => service.GetType() == typeof(WebhookEventSink));

        var request = GetValidVonagePayload(webhookSecret);
        using var response = await host.GetTestClient().SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
    [Fact]
    public async Task TwilioWebhook_WithWebhooksEnabled() {
        const string webhookAuthToken = "twilio-webhook-token";
        const string webhookUrl = "https://example.com/webhooks/twilio";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Webhooks:Enabled"] = true.ToString(),
                ["Webhooks:ChannelCapacity"] = "100",
                ["Sms:Provider"] = "twilio",
                ["Sms:WebhookAuthToken"] = webhookAuthToken,
                ["Sms:WebhookUrl"] = webhookUrl
            })
            .Build();

        using var host = await StartHostAsync(configuration);

        var services = host.Services;

        Assert.NotNull(
            services.GetRequiredService<Channel<List<WebhookEvent>>>());

        Assert.NotNull(
            services.GetRequiredKeyedService<IWebhookHttpHandler>("twilio"));

        Assert.Contains(
            services.GetServices<IHostedService>(),
            service => service.GetType() == typeof(WebhookEventSink));

        var request = GetValidTwilioPayload(webhookAuthToken, webhookUrl);
        using var response = await host.GetTestClient().SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task KapaTELWebhook_WithCredentialsEmbeddedInWebhookUrl() {
        const string webhookUsername = "kapatel-user";
        const string webhookPassword = "kapatel-pass";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Webhooks:Enabled"] = true.ToString(),
                ["Webhooks:ChannelCapacity"] = "100",
                ["Sms:Provider"] = "kapatel",
                ["Sms:WebhookUrl"] = $"https://{webhookUsername}:{webhookPassword}@example.com/webhooks/kapatel"
            })
            .Build();

        using var host = await new HostBuilder()
            .ConfigureWebHost(webHost => {
                webHost.UseTestServer();
                webHost.ConfigureServices(services => {
                    services.AddRouting();
                    services.AddSmsService(configuration);
                    services.AddSmsWebhooks<NoOpWebhookHandler>(configuration);
                });
                webHost.Configure(app => {
                    app.UseRouting();

                    app.UseEndpoints(endpoints => {
                        endpoints.MapWebhooks();
                    });
                });
            })
            .StartAsync(cancellationToken: TestContext.Current.CancellationToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/webhooks/kapatel?id=3f8df07f349ea6c2dbeb5d90cc4de275&foreign_id=KAPATEL3&notification_status=1&notification_datetime_stamp=2024-11-21%2010:13:21&msisdn=306900000000");
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{webhookUsername}:{webhookPassword}"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

        using var response = await host.GetTestClient().SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task YubotoWebhook_WithWebhooksEnabled() {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Webhooks:Enabled"] = true.ToString(),
                ["Webhooks:ChannelCapacity"] = "100",
                ["Sms:Provider"] = "yuboto"
            })
            .Build();

        using var host = await StartHostAsync(configuration);

        var services = host.Services;

        Assert.NotNull(
            services.GetRequiredService<Channel<List<WebhookEvent>>>());

        Assert.NotNull(
            services.GetRequiredKeyedService<IWebhookHttpHandler>("yuboto"));

        Assert.Contains(
            services.GetServices<IHostedService>(),
            service => service.GetType() == typeof(WebhookEventSink));

        using var response = await host.GetTestClient().PostAsync(
            "/webhooks/yuboto?smsid=msg-test-123&receiver=306900000000&sender=TestSender&status=6&statusDescription=Delivered&channel=sms&dlrDate=2024-11-21%2010:13:21&option1=optA&option2=optB",
            content: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task OmniMessagingWebhook_WithWebhooksEnabled() {
        const string apiKeyName = "X-API-Key";
        const string apiKeyValue = "omni-webhook-test-key";

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Webhooks:Enabled"] = true.ToString(),
                ["Webhooks:ChannelCapacity"] = "100",
                ["Sms:Provider"] = "omnimessaging",
                ["Sms:WebhookAuthenticationMethod"] = "ApiKey",
                ["Sms:WebhookApiKeyHeaderName"] = apiKeyName,
                ["Sms:WebhookSecret"] = apiKeyValue
            })
            .Build();

        using var host = await StartHostAsync(configuration);

        var services = host.Services;

        Assert.NotNull(
            services.GetRequiredService<Channel<List<WebhookEvent>>>());

        Assert.NotNull(
            services.GetRequiredKeyedService<IWebhookHttpHandler>("omnimessaging"));

        Assert.Contains(
            services.GetServices<IHostedService>(),
            service => service.GetType() == typeof(WebhookEventSink));

        var payload = new {
            id = 1735689600000000,
            campaign_id = 2501000000000001,
            campaign_reference = "ORDER-12345",
            message_id = 2501010000000001,
            contact_id = 123,
            recipient_id = (long?)null,
            unique_id = "CUSTOMER-100",
            to = "306900000000",
            type = "sms",
            delivery_report = new {
                status = "delivered",
                date = "2025-01-01T00:00:00Z"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/omnimessaging") {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add(apiKeyName, apiKeyValue);

        using var response = await host.GetTestClient().SendAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responsePayload = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>(TestContext.Current.CancellationToken);
        Assert.NotNull(responsePayload);
        Assert.True(responsePayload["success"]);
    }

    [Fact]
    public async Task KapaTELWebhook_WithWebhooksEnabled_AndBasicAuth() {
        const string webhookUsername = "kapatel-user";
        const string webhookPassword = "kapatel-pass";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Webhooks:Enabled"] = true.ToString(),
                ["Webhooks:ChannelCapacity"] = "100",
                ["Sms:Provider"] = "kapatel",
                ["Sms:WebhookUsername"] = webhookUsername,
                ["Sms:WebhookPassword"] = webhookPassword
            })
            .Build();

        using var host = await new HostBuilder()
            .ConfigureWebHost(webHost => {
                webHost.UseTestServer();
                webHost.ConfigureServices(services => {
                    services.AddRouting();
                    services.AddSmsService(configuration);
                    services.AddSmsWebhooks<NoOpWebhookHandler>(configuration);
                });
                webHost.Configure(app => {
                    app.UseRouting();

                    app.UseEndpoints(endpoints => {
                        endpoints.MapWebhooks();
                    });
                });
            })
            .StartAsync(cancellationToken: TestContext.Current.CancellationToken);

        var services = host.Services;

        Assert.NotNull(
            services.GetRequiredService<Channel<List<WebhookEvent>>>());

        Assert.NotNull(
            services.GetRequiredKeyedService<IWebhookHttpHandler>("kapatel"));

        Assert.Contains(
            services.GetServices<IHostedService>(),
            service => service.GetType() == typeof(WebhookEventSink));

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/webhooks/kapatel?id=3f8df07f349ea6c2dbeb5d90cc4de275&foreign_id=KAPATEL3&notification_status=1&notification_datetime_stamp=2024-11-21%2010:13:21&msisdn=306900000000");
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{webhookUsername}:{webhookPassword}"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

        using var response = await host.GetTestClient().SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static HttpRequestMessage GetValidApifonPayload(string webhookSecret) {
        var timestamp = DateTimeOffset.UtcNow.ToString("O");
        var reference = Guid.NewGuid().ToString("N");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));

        var signature = Convert.ToHexString(
            hmac.ComputeHash(
                Encoding.UTF8.GetBytes($"{reference}:{timestamp}")));

        var requestUrl =
            $"/webhooks/apifon" +
            $"?ref={Uri.EscapeDataString(reference)}" +
            $"&ts={Uri.EscapeDataString(timestamp)}" +
            $"&hmac={Uri.EscapeDataString(signature)}";

        var payload = new {
            url = "https://api.apifon.com/v1/sms",
            request_id = "req-test-123",
            reference_id = reference,
            data = new[]
            {
                new
                {
                    from = "Mock",
                    to = "Mock",
                    message_id = "msg-test-123",
                    custom_id = "integration-test",
                    status = new
                    {
                        code = 1,
                        text = "DELIVERED"
                    },
                    error_code = "",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    price = "0.05",
                    vat = "0.00",
                    mccmnc = "20201"
                }
            },
            account_id = 12345,
            type = "delivery"
        };

        return new HttpRequestMessage(HttpMethod.Post, requestUrl) {
            Content = JsonContent.Create(payload)
        };
    }

    private static HttpRequestMessage GetValidVonagePayload(string webhookSecret) {
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal) {
            ["msisdn"] = "306900000000",
            ["to"] = "Indice",
            ["messageId"] = "msg-test-123",
            ["status"] = "delivered",
            ["message-timestamp"] = "2024-11-21 10:13:21"
        };

        var queryToSign = string.Join("&", parameters.Select(kvp =>
            $"{kvp.Key.Replace('=', '_').Replace('&', '_')}={kvp.Value.Replace('=', '_').Replace('&', '_')}"));

        var query = $"&{queryToSign}";

        var signature = Convert.ToHexString(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(webhookSecret),
                Encoding.UTF8.GetBytes(query)));

        var requestUrl =
            $"/webhooks/vonage" +
            $"?msisdn={Uri.EscapeDataString(parameters["msisdn"])}" +
            $"&to={Uri.EscapeDataString(parameters["to"])}" +
            $"&messageId={Uri.EscapeDataString(parameters["messageId"])}" +
            $"&status={Uri.EscapeDataString(parameters["status"])}" +
            $"&message-timestamp={Uri.EscapeDataString(parameters["message-timestamp"])}" +
            $"&sig={Uri.EscapeDataString(signature)}";

        return new HttpRequestMessage(HttpMethod.Post, requestUrl);
    }
    private static HttpRequestMessage GetValidTwilioPayload(string authToken, string webhookUrl) {
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal) {
            ["Body"] = "Hello from Twilio",
            ["From"] = "+306900000001",
            ["MessageSid"] = "SM00000000000000000000000000000001",
            ["MessageStatus"] = "delivered",
            ["To"] = "+306900000002"
        };

        var signedPayload = new StringBuilder(webhookUrl);
        foreach (var parameter in parameters) {
            signedPayload.Append(parameter.Key).Append(parameter.Value);
        }

        var signature = Convert.ToBase64String(
            HMACSHA1.HashData(
                Encoding.UTF8.GetBytes(authToken),
                Encoding.UTF8.GetBytes(signedPayload.ToString())));

        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/twilio") {
            Content = new FormUrlEncodedContent(parameters)
        };
        request.Headers.Add("X-Twilio-Signature", signature);
        return request;
    }

    private static async Task<IHost> StartHostAsync(IConfiguration configuration) {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddConfiguration(configuration);
        builder.Services.AddRouting();
        builder.Services.AddSmsService(builder.Configuration);
        builder.AddWebhooks<NoOpWebhookHandler>().AddSmsWebhooks();
        var app = builder.Build();
        app.UseRouting();
        app.MapWebhooks();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }
}
