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

### 📧 Email Services

Send emails through various providers with template rendering support:

| Provider | Service Name |
|----------|--------------|
| SMTP | `EmailServiceSmtp` |
| SendGrid | `EmailServiceSendGrid` |
| SparkPost | `EmailServiceSparkpost` |
| Brevo (Sendinblue) | `EmailServiceBrevo` |

```csharp
// Auto-discover provider from configuration
services.AddEmailService(configuration);
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

### 📱 SMS Services

Send SMS messages through multiple gateway providers:

| Provider | Implementation |
|----------|----------------|
| Apifon | `SmsServiceApifon` |
| Apifon IM | `SmsServiceApifonIM` |
| KapaTEL | `SmsServiceKapaTEL` |
| Mstat | `SmsServiceMstat` |
| SmsUP | `SmsServiceSmsUP` |
| Twilio | `SmsServiceTwilio` |
| Vonage | `SmsServiceVonage` |
| Yuboto | `SmsServiceYuboto` |
| Yuboto Omni | `SmsServiceYubotoOmni` |
| Yuboto Viber | `SmsServiceYubotoOmniViber` |

```csharp
services.AddSmsServiceApifon(configuration);
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
