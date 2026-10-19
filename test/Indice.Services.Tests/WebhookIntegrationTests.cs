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
            services.GetRequiredService<IWebhookHttpHandler>());

        Assert.Contains(
            services.GetServices<IHostedService>(),
            service => service.GetType() == typeof(WebhookEventSink));


        var request = GetValidApifonPayload(webhookSecret);
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
}
