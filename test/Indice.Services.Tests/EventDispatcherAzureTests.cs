using System.IO.Compression;
using System.Security.Claims;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Indice.Services.Tests;

public class EventDispatcherAzureTests
{
    private const string CONNECTION_STRING = "UseDevelopmentStorage=true;";

    public EventDispatcherAzureTests() {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:StorageConnection"] = CONNECTION_STRING
            })
            .Build();
        var factory = new AzureClientFactory(configuration);
        EventDispatcher = new EventDispatcherAzure(EventDispatcherAzure.CONNECTION_STRING_NAME, "Development", enabled: true, useCompression: true, QueueMessageEncoding.Base64, () => ClaimsPrincipal.Current!, null, factory);
    }

    public EventDispatcherAzure EventDispatcher { get; set; }

    [Fact(Skip = "Should integrate azurite on build yaml")]
    public async Task CanSendCompressedPayload() {
        await EventDispatcher.RaiseEventAsync(new { FirstName = "Giorgos", LastName = "Gavalas" }, options => options.WrapInEnvelope(false).WithQueueName("test").PrependEnvironmentInQueueName(false));
        var queueClient = await EnsureExistsAsync("test");
        var message = await queueClient.ReceiveMessageAsync();
        var messageBody = await CompressionUtils.DecompressToString(message.Value.Body.ToArray());
        var json = JsonSerializer.Deserialize<JsonElement>(messageBody);
        Assert.Equal(JsonValueKind.Object, json.ValueKind);
        Assert.Contains("Giorgos", messageBody);
    }

    private async Task<QueueClient> EnsureExistsAsync(string queueName) {
        var queueClient = new QueueClient(CONNECTION_STRING, queueName, new QueueClientOptions {
            MessageEncoding = QueueMessageEncoding.Base64
        });
        await queueClient.CreateIfNotExistsAsync();
        return queueClient;
    }
}
