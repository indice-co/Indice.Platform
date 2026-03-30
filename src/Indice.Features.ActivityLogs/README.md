# Indice.Features.ActivityLogs

A comprehensive .NET library for tracking and managing user activity logs in ASP.NET Core applications. This feature provides detailed audit trails of user actions, including enriched context data such as device information, geolocation, request details, and more.

## Features

- **User Activity Tracking**: Capture and persist detailed records of user actions within your application
- **Asynchronous Processing**: Queue-based processing with background persistence for minimal performance impact
- **Enriched Logging**: Automatically augment logs with contextual information (device, location, user info, application info, request details)
- **Flexible Storage**: Entity Framework Core integration for database persistence with support for customizable cleanup policies
- **Anonymization**: Optional automatic anonymization of personal data (IP addresses, etc.)
- **Extensible Architecture**: Custom enrichers, filters, and event converters for tailored logging behavior
- **Automatic Cleanup**: Configurable retention policies with background cleanup service
- **Event Integration**: Convert and adapt domain events into activity log entries
- **Multi-Target Support**: Compatible with .NET 8, .NET 9, and .NET 10

## Installation

Add the NuGet package to your project:

```bash
dotnet add package Indice.Features.ActivityLogs
```

## Quick Start

### 1. Register the Activity Logs Feature

In your `Program.cs` file, add the activity logs service:

```csharp
services
    .AddActivityLogs(configuration)
    .UseEntityFrameworkCoreStore(optionsBuilder => 
    {
        optionsBuilder.UseSqlServer(connectionString);
    });
```

### 2. Configure Options (Optional)

You can customize the activity logs behavior by providing configuration:

```csharp
services
    .AddActivityLogs(configuration, options =>
    {
        options.Enable = true;
        options.AnonymizePersonalData = false;
        options.QueueChannelCapacity = 100;
        options.DequeueBatchSize = 10;
        options.DequeueTimeoutInMilliseconds = 1000;
        options.Cleanup.Enable = true;
        options.Cleanup.RetentionDays = 90;
        options.Cleanup.IntervalSeconds = 3600; // 1 hour
        options.Cleanup.BatchSize = 4000;
        options.Categories.Add("Authentication");
        options.Categories.Add("Authorization");
    })
    .UseEntityFrameworkCoreStore(optionsBuilder => 
    {
        optionsBuilder.UseSqlServer(connectionString);
    });
```

### 3. Publish Activity Logs

Inject `IActivityLogPublisher` and publish log entries:

```csharp
public class MyService
{
    private readonly IActivityLogPublisher _activityLogPublisher;

    public MyService(IActivityLogPublisher activityLogPublisher)
    {
        _activityLogPublisher = activityLogPublisher;
    }

    public async Task DoSomethingAsync()
    {
        var logEntry = new ActivityLogEntry
        {
            ActionName = "UpdateProfile",
            EventType = "UserAction",
            Category = "UserManagement",
            ApplicationId = "my-app",
            ApplicationName = "My Application",
            SubjectId = "user-123",
            SubjectName = "john.doe@example.com",
            ResourceId = "profile-456",
            ResourceType = "UserProfile",
            Description = "User updated their profile information",
            Succeeded = true
        };

        await _activityLogPublisher.PublishAsync(logEntry);
    }
}
```

## Configuration Options

### `ActivityLogOptions`

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enable` | `bool` | `true` | Determines whether activity logging is enabled |
| `AnonymizePersonalData` | `bool` | `false` | Anonymizes personal data (IP addresses) when persisted |
| `QueueChannelCapacity` | `int` | `100` | Maximum number of items the internal queue can store |
| `DequeueBatchSize` | `int` | `10` | Number of items processed in each batch |
| `DequeueTimeoutInMilliseconds` | `long` | `1000` | Timeout in milliseconds for the queue to reach batch size |
| `ApiPrefix` | `string` | `/api` | Prefix for API endpoints |
| `DatabaseSchema` | `string` | `auth` | Schema name for the database (relational providers) |
| `Categories` | `HashSet<string>` | Empty | Set of activity log categories |
| `ExcludedEnrichers` | `List<Type>` | Empty | List of default enrichers to exclude |
| `Cleanup` | `LogCleanupOptions` | - | Configuration for automatic log cleanup |

### `LogCleanupOptions`

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enable` | `bool` | `false` | Determines whether automatic cleanup is enabled |
| `RetentionDays` | `ushort` | `90` | Number of days to retain log entries |
| `IntervalSeconds` | `ushort` | `3600` | Seconds between cleanup executions (1 hour) |
| `BatchSize` | `ushort` | `4000` | Number of items to delete in each cleanup iteration |

## Core Services

### `IActivityLogPublisher`

The main interface for publishing activity log entries.

```csharp
public interface IActivityLogPublisher
{
    Task PublishAsync(ActivityLogEntry entry);
}
```

### `IActivityLogStore`

Interface for persisting and retrieving activity log entries.

```csharp
public interface IActivityLogStore
{
    Task InsertAsync(DbActivityLogEntry logEntry, CancellationToken cancellationToken = default);
    Task<IEnumerable<ActivityLogEntry>> GetAllAsync(ActivityLogEntryFilter filter, CancellationToken cancellationToken = default);
    Task<long> CountAsync(ActivityLogEntryFilter filter, CancellationToken cancellationToken = default);
    Task DeleteAsync(ActivityLogEntryFilter filter, CancellationToken cancellationToken = default);
}
```

## Activity Log Entry Model

### `ActivityLogEntry`

Represents a single user activity log entry:

```csharp
public class ActivityLogEntry
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? ActionName { get; set; }
    public string EventType { get; set; }
    public string? Category { get; set; }
    public string? ApplicationId { get; set; }
    public string? ApplicationName { get; set; }
    public string? Source { get; set; }
    public string? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public bool SubjectUnknown { get; set; }
    public string? ResourceId { get; set; }
    public string? ResourceType { get; set; }
    public string? Description { get; set; }
    public bool Succeeded { get; set; }
    public string? IpAddress { get; set; }
    public string? RequestId { get; set; }
    public string? Location { get; set; }
    public string? SessionId { get; set; }
    public bool Review { get; set; }
    public string? CountryIsoCode { get; set; }
}
```

## Enrichers

The library includes several built-in enrichers that automatically augment log entries with additional context:

### Default Enrichers

- **`ApplicationInfoEnricher`**: Adds application identification information
- **`UserInfoEnricher`**: Enriches with user/subject information
- **`RequestInfoEnricher`**: Captures HTTP request details (IP address, session ID)
- **`DeviceEnricher`**: Adds device information from request headers
- **`DeviceIdEnricher`**: Extracts device identifier
- **`LocationEnricher`**: Adds geolocation data based on IP address
- **`AnonymizationEnricher`**: Anonymizes personal data when configured
- **`CategoryFilter`**: Filters logs based on configured categories

### Excluding Default Enrichers

To exclude specific enrichers:

```csharp
services.AddActivityLogs(configuration, options =>
{
    options.ExcludedEnrichers.Add(typeof(LocationEnricher));
    options.ExcludedEnrichers.Add(typeof(AnonymizationEnricher));
})
```

### Creating Custom Enrichers

Implement `IActivityLogEntryEnricher` to create custom enrichers:

```csharp
public class CustomEnricher : IActivityLogEntryEnricher
{
    public async Task EnrichAsync(ActivityLogEntry entry, ActivityLogEnricherRunType runType)
    {
        if (runType == ActivityLogEnricherRunType.Synchronous)
        {
            entry.Description += " [Custom Info]";
        }
        await Task.CompletedTask;
    }
}
```

Register the custom enricher:

```csharp
services
    .AddActivityLogs(configuration)
    .AddEnricher<CustomEnricher>()
    .UseEntityFrameworkCoreStore(optionsBuilder => 
    {
        optionsBuilder.UseSqlServer(connectionString);
    });
```

## Event Conversion

Convert domain events into activity log entries using `IActivityLogFromEventConverter`:

```csharp
public interface IActivityLogFromEventConverter
{
    bool CanConvert(DomainEvent domainEvent);
    ActivityLogEntry Convert(DomainEvent domainEvent);
}
```

The default implementation (`DefaultActivityLogFromEventConverter`) can be customized via the `ActivityLogAdapterEventHandler`.

## Hosted Services

### `PersistLogsHostedService`

Background service that processes queued log entries and persists them to the configured store. Operates in batches for efficient database operations.

### `LogCleanupHostedService`

When enabled, automatically removes log entries that exceed the configured retention period. Operates on a configurable interval with configurable batch sizes to avoid table locks.

## Using Entity Framework Core

The library provides built-in Entity Framework Core integration:

```csharp
services
    .AddActivityLogs(configuration)
    .UseEntityFrameworkCoreStore((serviceProvider, options) =>
    {
        var connectionString = serviceProvider
            .GetRequiredService<IConfiguration>()
            .GetConnectionString("DefaultConnection");
        
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.MigrationsAssembly("YourMigrationsAssembly");
        });
    });
```

### Database Context

The library uses `ActivityLogDbContext` which includes:

- `DbSet<DbActivityLogEntry> ActivityLogEntries { get; }`
- Configured mappings via `DbActivityLogEntryMap`

### Migrations

Create and apply migrations for the activity logs tables:

```bash
dotnet ef migrations add InitialActivityLogs --context ActivityLogDbContext
dotnet ef database update --context ActivityLogDbContext
```

## Filtering Activity Logs

Query activity logs using `ActivityLogEntryFilter`:

```csharp
var filter = new ActivityLogEntryFilter
{
    Subject = "user-123",
    Category = "Authentication",
    FromDate = DateTime.Now.AddDays(-7),
    ToDate = DateTime.Now,
    IncludeSuccessful = true,
    IncludeFailed = true
};

var entries = await store.GetAllAsync(filter);
```

## No-Op Store

For scenarios where you want to disable logging without changing code:

```csharp
services.AddActivityLogs(configuration, options =>
{
    options.Enable = false;
});
// Uses ActivityLogStoreNoop by default when Enable = false
```

## Performance Considerations

- **Asynchronous Processing**: Log entries are queued and processed asynchronously to minimize impact on request handling
- **Batch Processing**: Logs are persisted in configurable batches to optimize database operations
- **Queue Capacity**: Configure `QueueChannelCapacity` based on your application's load
- **Cleanup Operations**: Stagger cleanup with appropriate `IntervalSeconds` to avoid peak hour lock contention

## Examples

### Log Authentication Events

```csharp
var loginLog = new ActivityLogEntry
{
    ActionName = "Login",
    Category = "Authentication",
    EventType = "SecurityEvent",
    SubjectId = user.Id,
    SubjectName = user.Email,
    Succeeded = loginSuccessful,
    Description = loginSuccessful ? "User logged in successfully" : "Login failed"
};

await _activityLogPublisher.PublishAsync(loginLog);
```

### Log Resource Changes

```csharp
var resourceLog = new ActivityLogEntry
{
    ActionName = "UpdateResource",
    Category = "ResourceManagement",
    EventType = "DataModification",
    SubjectId = currentUser.Id,
    ResourceId = resource.Id,
    ResourceType = "Document",
    Description = $"Resource '{resource.Name}' was updated",
    Succeeded = true
};

await _activityLogPublisher.PublishAsync(resourceLog);
```

## Architecture

The activity logs feature follows a clean architecture with several key components:

- **Publisher**: Main entry point for creating log entries
- **Enricher Pipeline**: Sequential enrichment of log entries with additional context
- **Queue**: Buffering layer for asynchronous processing
- **Persistence Layer**: Abstracted store interface with Entity Framework Core implementation
- **Cleanup Service**: Background worker for retention policy enforcement
- **Event Adapter**: Integration point for domain event conversion

## Troubleshooting

### Logs Not Being Persisted

1. Verify `Enable` is set to `true` in options
2. Check that `PersistLogsHostedService` is running
3. Verify database connectivity and migrations have been applied
4. Check `QueueChannelCapacity` isn't being exceeded

### High Memory Usage

1. Reduce `QueueChannelCapacity` to limit buffered entries
2. Decrease `DequeueTimeoutInMilliseconds` for faster processing
3. Increase `DequeueBatchSize` for fewer, larger batches

### Performance Impact

1. Consider excluding unnecessary enrichers
2. Increase batch sizes for persistence operations
3. Implement filtering to log only important events
4. Enable anonymization to reduce data volume

## License

This library is part of the Indice.Platform and is licensed under the applicable open-source license.
