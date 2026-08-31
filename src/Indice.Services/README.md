# Indice.Services

A comprehensive library of infrastructure service abstractions and implementations for .NET applications. This package provides production-ready integrations with Azure services and popular third-party providers for common application needs.

[![NuGet](https://img.shields.io/nuget/v/Indice.Services.svg)](https://www.nuget.org/packages/Indice.Services)

## Installation

```bash
dotnet add package Indice.Services
```

## Features

### 📁 File Storage

Store and retrieve files using different backing stores:

| Implementation | Description |
|----------------|-------------|
| `FileServiceAzureStorage` | Azure Blob Storage |
| `FileServiceLocal` | Local file system |
| `FileServiceInMemory` | In-memory (for testing) |

```csharp
// Azure Blob Storage
services.AddFilesAzure(options => {
    options.ConnectionStringName = "StorageConnection";
    options.ContainerName = "my-container";
});

// Local file system
services.AddFilesLocal(options => {
    options.Path = "uploads";
});
```

#### 🔧 Configuration (Azure Storage)

`AzureClientFactory` resolves the connection by `ConnectionStringName` (`StorageConnection` by default):

- First from `ConnectionStrings:{ConnectionStringName}` (or a direct value with the same key)
- Otherwise from managed-identity settings under `{ConnectionStringName}`

Standard connection string:
```json
{
  "ConnectionStrings": {
    "StorageConnection": "DefaultEndpointsProtocol=https;AccountName=<NAME>;AccountKey=xxx;EndpointSuffix=core.windows.net"
  }
}
```

Managed Identity (system-assigned):
```json
{
  "StorageConnection": {
    "accountName": "<NAME>",
    "serviceUri": "<SERVICE_URI>"
  }
}
```

Managed Identity (user-assigned):
```json
{
  "StorageConnection": {
    "accountName": "<NAME>",
    "serviceUri": "<SERVICE_URI>",
    "clientId": "<CLIENT_ID>"
  }
}
```

For Azure Functions host, you need to provide these extra values too:
```json
AzureWebJobsStorage__accountName = <NAME>
AzureWebJobsStorage__serviceUri = <BASE_SERVICE_URL>
```

For user-assigned Managed Identity, add this too:
```json
AzureWebJobsStorage__clientId = <CLIENT_ID>
```

Proper RBAC roles to be assigned to the Managed Identity:

- Storage Account Contributor
- Storage Blob Data Owner
- Storage Queue Data Contributor
- Storage Queue Data Message Processor
- Storage Queue Data Message Sender
- Storage Queue Data Reader

### 📧 Email Services

Send emails through various providers with template rendering support:

| Provider | Service Name |
|----------|--------------|
| SMTP | `EmailServiceSmtp` |
| SendGrid | `EmailServiceSendGrid` |
| SparkPost | `EmailServiceSparkpost` |
| Brevo (Sendinblue) | `EmailServiceBrevo` |
| Azure Communication Services | `AzureCommunicationServicesEmailService` |

```csharp
// Auto-discover provider from configuration
services.AddEmailService(configuration);

// Or register specific provider
services.AddEmailServiceAzureCommunicationServices(configuration);
```

Configuration (`appsettings.json`):
```json
{
  "Email": {
    "Provider": "smtp",
    "Sender": "noreply@example.com",
    "SenderName": "My App",
    "SmtpHost": "smtp.example.com",
    "SmtpPort": 587
  }
}
```

#### Configuration for Azure Communication Services

```json
{
  "Email": {
    "Provider": "azurecommunicationservices"
  },
  "AzureCommunicationServices": {
    "Sender": "no-reply@yourdomain.com",
    "ClientId": "your-azure-ad-app-id",
    "ClientSecret": "your-azure-ad-app-secret",
    "TenantId": "your-azure-ad-tenant-id",
    "ResourceEndpoint": "https://your-acs-resource.communication.azure.com",
    "BccRecipients": "optional-bcc@example.com",
    "WaitUntilCompleted": false
  }
}
```

**Key Configuration Options:**
- `ClientId`, `ClientSecret`, `TenantId`: Azure AD authentication credentials
- `ResourceEndpoint`: Your Azure Communication Services resource endpoint
- `Sender`: The sender email address (must be configured in Azure Portal)
- `BccRecipients`: Optional semicolon or comma-separated list of BCC recipients
- `WaitUntilCompleted`: Wait for completion before returning (default: `false`, recommended to keep false as it takes ~12 seconds)

**Note**: Sender name must be configured in the Azure Portal on the ACS resource itself.

### 📱 SMS Services

Send SMS messages through multiple gateway providers:

| Provider | Implementation |
|----------|----------------|
| Apifon | `SmsServiceApifon` |
| Apifon IM | `SmsServiceApifonIM` |
| KapaTEL | `SmsServiceKapaTEL` |
| Konecta | `SmsServiceKonecta` |
| Mstat | `SmsServiceMstat` |
| SmsUP | `SmsServiceSmsUP` |
| Twilio | `SmsServiceTwilio` |
| Vonage | `SmsServiceVonage` |
| Yuboto | `SmsServiceYuboto` |
| Yuboto Omni | `SmsServiceYubotoOmni` |
| Yuboto Viber | `SmsServiceYubotoOmniViber` |

```csharp
// Auto-discover provider from configuration
services.AddSmsService(configuration);

// Or register specific provider
services.AddSmsServiceKonecta(configuration);
```

#### Configuration for Konecta

Konecta uses Basic Authentication and requires specific configuration:

```json
{
  "Sms": {
    "Provider": "konecta",
    "Username": "your-username",
    "Password": "your-password",
    "Sender": "YourSender",
    "BaseUrl": "https://service.comdatagroup.fr/",
    "Operation": "campaign",
    "Site": "default"
  }
}
```

**Key Configuration Options:**
- `Username` & `Password`: Required for Basic Authentication
- `BaseUrl`: API endpoint (defaults to `https://service.comdatagroup.fr/`)
- `Operation`: Operation identifier (defaults to `"campaign"`)
- `Site`: Site identifier (defaults to `"default"`)
- `Sender`: The sender ID visible to recipients

**Note**: Konecta does not support multiple recipients in a single request.

### 🔔 Push Notifications

Send push notifications via Azure Notification Hubs:

```csharp
services.AddPushNotificationServiceAzure((sp, options) => {
    options.ConnectionString = "your-connection-string";
    options.NotificationHubPath = "your-hub-name";
});
```

### 📤 Event Dispatching

Dispatch events asynchronously using queue-based messaging:

| Implementation | Backing Store |
|----------------|---------------|
| `EventDispatcherAzure` | Azure Queue Storage |
| `EventDispatcherAzureServiceBus` | Azure Service Bus |
| `EventDispatcherInMemory` | In-memory (for testing) |

```csharp
services.AddEventDispatcherAzure(configuration);

// Raise events
await eventDispatcher.RaiseEventAsync(new OrderCreatedEvent { OrderId = orderId });
```

### 🔒 Distributed Locking

Coordinate distributed operations using lease-based locking:

| Implementation | Backing Store |
|----------------|---------------|
| `LockManagerAzure` | Azure Blob Storage leases |
| `LockManagerInMemory` | In-memory (for testing) |

```csharp
services.AddLockManagerAzure(configuration);

// Acquire and use a lock
await using var lease = await lockManager.AcquireLock("my-resource", TimeSpan.FromSeconds(30));
if (lease.IsAcquired) {
    // Perform exclusive operation
}
```

### 📡 SignalR Proxy

Proxy SignalR connections through Azure SignalR Service:

```csharp
services.AddSignalRProxyServices(configuration);
```

## Configuration

Most services auto-configure from `IConfiguration`. Common connection string names:

| Service | Connection String Name |
|---------|------------------------|
| File Storage | `StorageConnection` |
| Event Dispatcher | `StorageConnection` |
| Lock Manager | `StorageConnection` |
| Service Bus | `ServiceBusConnection` |

## Dependencies

- [Azure.Storage.Blobs](https://www.nuget.org/packages/Azure.Storage.Blobs) - Blob storage operations
- [Azure.Storage.Queues](https://www.nuget.org/packages/Azure.Storage.Queues) - Queue storage operations
- [Azure.Messaging.ServiceBus](https://www.nuget.org/packages/Azure.Messaging.ServiceBus) - Service Bus messaging
- [Azure.Communication.Email](https://www.nuget.org/packages/Azure.Communication.Email) - Azure Communication Services email sending
- [MailKit](https://www.nuget.org/packages/MailKit) - SMTP email sending
- [Microsoft.Azure.NotificationHubs](https://www.nuget.org/packages/Microsoft.Azure.NotificationHubs) - Push notifications
- [Microsoft.Azure.SignalR.Management](https://www.nuget.org/packages/Microsoft.Azure.SignalR.Management) - SignalR proxy

## Target Frameworks

- .NET 8.0
- .NET 9.0
- .NET 10.0

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/indice-co/Indice.Platform/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/indice-co/Indice.Platform)
- [NuGet Package](https://www.nuget.org/packages/Indice.Services)
