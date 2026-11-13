# Messaging Webhooks

The messaging webhook integration receives delivery and engagement notifications from supported SMS and email providers, processes them asynchronously, and persists them as message events for reporting and analytics.

## Requirements

- `Indice.Features.Messages.AspNetCore`
- `Indice.Features.Messages.Core`
- A configured and supported SMS or email provider
- A configured `CampaignsDbContext` database
- A publicly reachable HTTPS endpoint for provider callbacks

## 1. Register webhook services

Register the messaging webhook services on the application builder. This registers the webhook processing pipeline, the default `MessagesWebhookHandler`, and the HTTP handlers for the configured default SMS and email providers.

```csharp
using Indice.Features.Messages.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddMessagesWebhookServices();

var app = builder.Build();

app.MapWebhooks();

app.Run();
```

`AddMessagesWebhookServices()` uses the built-in `MessagesWebhookHandler`. It requires the messaging database dependencies to be registered, including `CampaignsDbContext`.

`MapWebhooks()` maps the provider callback endpoints under `/webhooks` by default.

## 2. Configure webhook processing

Add the following section to `appsettings.json`:

```json
{
  "Webhooks": {
    "Enabled": true,
    "ChannelCapacity": 100
  }
}
```

| Setting | Default | Description |
| --- | --- | --- |
| `Enabled` | `true` | Enables or disables webhook endpoint processing. When disabled, webhook endpoints return `404 Not Found`. |
| `ChannelCapacity` | `100` | Maximum number of webhook event batches waiting in the processing channel. |

The channel uses bounded capacity and applies backpressure when full. Choose a capacity appropriate for the expected callback volume and database processing rate.

The same settings can be configured directly during registration:

```csharp
builder.AddMessagesWebhookServices(options =>
{
    options.Enabled = true;
    options.ChannelCapacity = 250;
});
```

The delegate customizes the options bound from the `Webhooks` configuration section.

## 3. Configure the messaging providers

The registration method discovers HTTP handlers from the configured default SMS and email providers. Configure the providers using the existing `Sms` and `Email` configuration sections and their respective provider-specific settings.

For example, a configuration using Apifon for SMS and SendGrid for email must specify those providers and their required credentials using the settings supported by each service implementation.

The integration registers the corresponding HTTP handlers automatically for supported providers.

**Important:** The webhook registration uses the default SMS and email providers. Configuring multiple sending providers does not automatically register webhook handlers for every provider in a multi-provider configuration.

Only providers with a corresponding webhook HTTP handler are supported by this registration path.

## 4. Register a custom webhook event handler

To replace the default persistence behavior, implement `IWebhookHandler` and use the generic registration method.

```csharp
using Indice.Services;

public sealed class CustomMessagesWebhookHandler : IWebhookHandler
{
    public async Task HandleAsync(
        List<WebhookEvent> events,
        CancellationToken cancellationToken)
    {
        foreach (var webhookEvent in events)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Process the event using the application's own logic.
        }

        await Task.CompletedTask;
    }
}
```

Register the custom handler:

```csharp
builder.AddMessagesWebhookServices<CustomMessagesWebhookHandler>();
```

The custom handler replaces the default `MessagesWebhookHandler`. The SMS and email HTTP handlers are still registered by the same extension method.

The custom handler is registered with a scoped lifetime and receives batches of normalized `WebhookEvent` instances.

## 5. Register a custom provider HTTP handler

Provider-specific request parsing and validation are implemented through `IWebhookHttpHandler`. Register a custom implementation when integrating a provider that is not supported by the built-in provider registration.

```csharp
using Indice.Services;

public sealed class CustomProviderWebhookHttpHandler : IWebhookHttpHandler
{
    public Task<WebhookParseResult> ParseAsync(
        IHeaderDictionary headers,
        IQueryCollection query,
        Stream requestBody,
        CancellationToken cancellationToken)
    {
        
    }
}
```

Register the handler against the provider route key:

```csharp
builder
    .AddMessagesWebhookServices()
    .AddHttpHandler<CustomProviderWebhookHttpHandler>("custom-provider");
```

Implement `ParseAsync` using the actual return type and validation requirements defined by the version of `IWebhookHttpHandler` in use. The implementation must validate the provider request and produce normalized webhook events.

The corresponding callback endpoint is:

```text
POST /webhooks/custom-provider
```

The provider key must match the registered key. Implement authentication or signature verification as required by the provider; the endpoint itself allows anonymous access.

## 6. Endpoint configuration

The webhook endpoints can be mapped using a custom prefix:

```csharp
app.MapWebhooks("/api/webhooks");
```

By default, webhook endpoints are excluded from the generated OpenAPI description. To include them:

```csharp
app.MapWebhooks("/api/webhooks", excludeFromOpenApi: false);
```

The endpoint mapping supports `POST` and `GET` requests. The configured HTTP handler is responsible for parsing and validating each request.

## 7. Default processing behavior

The built-in `MessagesWebhookHandler` persists normalized provider events in the messaging database.

It:

- Correlates provider receipt IDs with previously recorded sent message events.
- Associates matched events with their campaign, message, and contact.
- Persists unmatched events as orphan events.
- Records the event type, channel, recipient, success status, provider receipt ID, and event timestamp.
- Allows repeated webhook deliveries to be recorded.

Webhook batches are processed asynchronously by the registered `WebhookEventSink`. Provider HTTP handlers parse incoming requests and enqueue their events; the sink invokes the configured `IWebhookHandler`.

## 8. Operational considerations

- Publish the callback URL using HTTPS and configure the provider to send notifications to the matching route.
- Keep provider credentials and signing secrets in an appropriate secrets store.
- Ensure the configured database is available to the background processing service.
- Monitor application logs for webhook parsing failures and database persistence errors.
- Size `ChannelCapacity` according to expected traffic. The in-memory channel is not a durable message broker, so queued batches can be lost if the process terminates before processing them.
- Ensure the deployed application exposes only the providers that are configured and supported.
