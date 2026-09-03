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

| Provider key (`Email:Provider`) | Service Name | Add method | Configuration section |
|---------------------------------|--------------|------------|------------------------|
| `Smtp` | `EmailServiceSmtp` | `services.AddEmailServiceSmtp(configuration)` | `Email` |
| `SendGrid` | `EmailServiceSendGrid` | `services.AddEmailServiceSendGrid(configuration)` | `SendGrid` |
| `SparkPost` | `EmailServiceSparkPost` | `services.AddEmailServiceSparkPost(configuration)` | `SparkPost` |
| `Brevo` | `EmailServiceBrevo` | `services.AddEmailServiceBrevo(configuration)` | `Brevo` |
| `AzureCommunicationServices` | `AzureCommunicationServicesEmailService` | `services.AddEmailServiceAzureCommunicationServices(configuration)` | `AzureCommunicationServices` |
| `Noop` / `none` | `EmailServiceNoop` | `services.AddEmailServiceNoop()` | - |

```csharp
// Auto-discover provider from configuration
services.AddEmailService(configuration);
```

Configuration examples (`appsettings.json`):

SMTP:
```json
{
  "Email": {
    "Provider": "Smtp",
    "Sender": "noreply@example.com",
    "SenderName": "My App",
    "SmtpHost": "smtp.example.com",
    "SmtpPort": 587,
    "Username": "smtp-user",
    "Password": "smtp-password"
  }
}
```

SendGrid:
```json
{
  "Email": {
    "Provider": "SendGrid"
  },
  "SendGrid": {
    "Sender": "noreply@example.com",
    "SenderName": "My App",
    "ApiKey": "<sendgrid-api-key>",
    "Api": "https://api.sendgrid.com/v3/"
  }
}
```

Azure Communication Services:
```json
{
  "Email": {
    "Provider": "AzureCommunicationServices"
  },
  "AzureCommunicationServices": {
    "Sender": "DoNotReply@<your-domain>",
    "ClientId": "<app-client-id>",
    "ClientSecret": "<app-client-secret>",
    "TenantId": "<tenant-id>",
    "ResourceEndpoint": "https://<resource-name>.<region>.communication.azure.com",
    "WaitUntilCompleted": false
  }
}
```

### 📱 SMS Services

Send SMS messages through multiple gateway providers:

| Provider key (`Sms:Provider`) | Implementation | Add method | Required configuration keys (under `Sms`) |
|--------------------------------|----------------|------------|--------------------------------------------|
| `apifon` | `SmsServiceApifon` | `services.AddSmsServiceApifon(configuration)` | `ApiKey`, `Token`, `Sender` (or `SenderName`) |
| `apifon_im` / `apifonim` | `SmsServiceApifonIM` | `services.AddSmsServiceApifonIM(configuration)` | `ApiKey`, `Token`, `Sender` (or `SenderName`) |
| `kapatel` / `kapa_tel` | `SmsServiceKapaTEL` | `services.AddSmsServiceKapaTEL(configuration)` | `Username`, `Password`, `From` |
| `mstat` | `SmsServiceMstat` | `services.AddSmsServiceMstat(configuration)` | `ApiKey`, `SenderName` |
| `smsup` | `SmsServiceSmsUP` | `services.AddSmsServiceSmsUp(configuration)` | `ApiKey`, `Sender` |
| `twilio` | `SmsServiceTwilio` | `services.AddSmsServiceTwilio(configuration)` | `AccountSid`, plus (`ApiKey` + `Secret`) or (`AuthToken`), and `SenderPhoneNumber` or `MessagingServiceSid` |
| `vonage` | `SmsServiceVonage` | `services.AddSmsServiceVonage(configuration)` | `ApiKey`, `SignatureSecret`, `Sender` (or `SenderName`) |
| `yuboto` / `yuboto_omni` | `SmsServiceYubotoOmni` | `services.AddSmsServiceYubotoOmni(configuration)` | `ApiKey`, `Sender` (or `SenderName`) |
| `yuboto_viber` / `yubotoviber` / `yuboto_omni_viber` | `SmsServiceYubotoOmniViber` | `services.AddSmsServiceYubotoOmniViber(configuration)` | `ApiKey`, `Sender` (or `SenderName`), optional `ViberFallbackEnabled` |
| `noop` / `none` | `SmsServiceNoop` | `services.AddSmsServiceNoop()` | - |

```csharp
// Auto-discover provider(s) from Sms:Provider
services.AddSmsService(configuration);
```

```json
{
  "Sms": {
    "Provider": "twilio",
    "Sender": "MyApp",
    "AccountSid": "<account-sid>",
    "AuthToken": "<auth-token>",
    "SenderPhoneNumber": "+15551234567"
  }
}
```

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
- [MailKit](https://www.nuget.org/packages/MailKit) - SMTP email sending
- [Microsoft.Azure.NotificationHubs](https://www.nuget.org/packages/Microsoft.Azure.NotificationHubs) - Push notifications
- [Microsoft.Azure.SignalR.Management](https://www.nuget.org/packages/Microsoft.Azure.SignalR.Management) - SignalR proxy

## Target Frameworks

- .NET 8.0
- .NET 9.0

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/indice-co/Indice.Platform/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/indice-co/Indice.Platform)
- [NuGet Package](https://www.nuget.org/packages/Indice.Services)
