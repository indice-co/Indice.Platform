# Indice.Hosting

Background jobs for ASP.NET Core, on top of Quartz. Two kinds of trigger:

- **Scheduled** — for cron expression.
- **Queue** — for handling message/events/commands.

---

## Install and configure

```csharp
services.AddWorkerHost(options => {
        options.UseStoreRelational(builder => builder.UseSqlServer(Configuration.GetConnectionString("eBankingWorkerDb")));
    })
    .AddJob<EtlPipelineHandler>()
        .WithScheduleTrigger(cronExpression: "0 0 2 * * ?", options => {
            options.Name = "etl-pipeline";
            options.Description = "A scheduled task that runs an etl pipeline every day at 2am.";
            options.Group = "chania-bank";
        })
    .AddJob<OrderCreatedHandler>()
        .WithQueueTrigger<OrderCreated>(options => {
            options.QueueName = "order-created";
            options.PollingInterval = 200;
            options.InstanceCount = 1;
        });
```

### Publish-only mode
Set `General:WorkerHostDisabled` to `true` (or a root-level `WorkerHostDisabled`) and the consumer service will not be registered. The queues are still available for publishing.

### Other stores

```csharp
options.UseScheduledTaskStoreInMemory();   // scheduled-task state is not persisted
options.UseLockManagerAzure();             // distributed locks in Azure Storage instead of SQL
options.UseLockManagerInMemory();
```

---

## Writing handlers

A handler is a plain class with a `Process` method. The queue item as well as any DI services are injected automatically:
```csharp
public class OrderCreatedHandler
{
    public async Task Process(Order order, IUserService userService, CancellationToken cancellationToken) {
        ...
    }
}
```
For Schedule triggers no queue item is injected.
```csharp
public class EtlPipelineHandler
{
    public async Task Process(DbContext dbContext, ILogger<EtlPipelineHandler> logger, CancellationToken cancellationToken) {
        ...
    }
}
```

### Scheduled jobs with state

`WithScheduleTrigger<TState>` gives the handler a persisted state between runs - e.g. watermark for etl.

### Scheduled jobs - one instance only

If you deploy to different machines, you can set `Singleton = true` per scheduled job to run only one instance of a job.

### Run now

With the relational store, a scheduled task can be run once on demand, e.g. from the workers dashboard
(`Indice.Features.Workers.UI`). The request is a message in the queue `$run-now:{taskId}`; each worker checks the requests of
the tasks it hosts every `RunNowPollingInterval` and discards the ones older than `RunNowExpiration`:

```csharp
services.AddWorkerHost(options => {
    options.RunNowPollingInterval = TimeSpan.FromSeconds(15); // default; null disables it
    options.RunNowExpiration = TimeSpan.FromMinutes(10);      // default
    options.UseStoreRelational(...);
});
```

### Server registration

With the relational store, each worker registers itself in the `work.Server` table on startup, with the queues it consumes
(and their settings) and the scheduled tasks it hosts. It refreshes its row with a heartbeat and deletes it on shutdown; every
worker also deletes rows only after both `ServerTimeout` and three of the peer's advertised heartbeat intervals have elapsed,
so a crashed worker goes away on its own without pruning a healthy worker that heartbeats less frequently. The workers
dashboard uses the table to show the live servers and the configured queues and tasks.

```csharp
services.AddWorkerHost(options => {
    options.ServerHeartbeatInterval = TimeSpan.FromSeconds(30); // default; null or zero disables registration
    options.ServerTimeout = TimeSpan.FromMinutes(5);            // default; at least three heartbeats
    options.UseStoreRelational(...);
});
```

Add a migration for the `work.Server` table (see the changelog). Until it exists the worker logs a warning and runs as before.

### Metrics

The workers measure their own work with a `System.Diagnostics.Metrics` meter named `Indice.Hosting`
(`WorkerDiagnostics.MeterName`). Messages of the run-now queues are not counted.

| Instrument | Type | Unit | Tags |
|---|---|---|---|
| `indice.worker.queue.messages.processed` | Counter | `{message}` | `worker.queue`, `worker.outcome` (`succeeded`, `retried`, `poisoned`) |
| `indice.worker.queue.messages.active` | UpDownCounter | `{message}` | `worker.queue` |
| `indice.worker.queue.message.duration` | Histogram | `s` | `worker.queue`, `worker.outcome` |
| `indice.worker.queue.message.wait_time` | Histogram | `s` | `worker.queue` |
| `indice.worker.task.runs` | Counter | `{run}` | `worker.task`, `worker.outcome` (`succeeded`, `failed`) |
| `indice.worker.task.runs.active` | UpDownCounter | `{run}` | `worker.task` |
| `indice.worker.task.run.duration` | Histogram | `s` | `worker.task`, `worker.outcome` |

Export them with OpenTelemetry, or watch them live with `dotnet-counters`:

```csharp
builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(WorkerDiagnostics.MeterName));
```

```bash
dotnet-counters monitor --name MyWorker --counters Indice.Hosting
```

On .NET 9 and later the histograms advise bucket boundaries in seconds; on .NET 8 the exporter's defaults apply, so
configure a view if you need finer buckets.

With server registration on, each worker also stores a snapshot of these numbers in `work.Server.Metrics` on every heartbeat:
totals since it started, what it is processing now and the counts and average durations of the last minute. The workers
dashboard sums them up across the live servers for its throughput and duration figures.

---

## Poison Queue
The messages exceeding the `MaxRetryCount` are moved to the poison queue. The integrator is responsible for processing them if needed.

## Publishing messages

Inject `IMessageQueue<T>` for any type registered with `WithQueueTrigger<T>`:

```csharp
public class AlertService(IMessageQueue<Alert> queue)
{
    await queue.Enqueue(alert);
    await queue.Enqueue(alert, visibilityWindow: TimeSpan.FromHours(1));
    await queue.EnqueueRange(alerts);
}
```

Or through `IEventDispatcher`:
```csharp
await _eventDispatcher.RaiseEventAsync(new CampaignCreatedEvent(campaign.Id));
await _eventDispatcher.RaiseEventAsync(evt, visibilityTimeout: TimeSpan.FromMinutes(5));
```


## Outbox

`queue.Enqueue(...)` and `RaiseEventAsync(...)` write immediately, on the worker's own connection.
```csharp
_db.Alerts.Add(alert);
await _db.SaveChangesAsync();
await _queue.Enqueue(new Alert(alert.Id));   // if this fails, the business row is saved and nothing is sent
```

Use `AddAndEnqueue` if you want Outbox behavior.

### Requirements

Your `DbContext` must target the same database and more restrictively be on the same DbContext as the worker store.
```csharp
services.AddWorkerHost(options => options.UseStoreRelational<BankingDbContext>(builder => builder.UseSqlServer(connectionString)));
```

### 1. Implement `ITaskDbContext` and `ILockDbContext` on your `DbContext`

```csharp
public class BankingDbContext : DbContext, ITaskDbContext, ILockDbContext
{
    public BankingDbContext(DbContextOptions<BankingDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);
        builder.ApplyWorkerConfiguration(Database.ProviderName); <-- this is required
    }
}
```

### 2. Register it as the store

```csharp
services.AddWorkerHost(options => options.UseStoreRelational<BankingDbContext>(builder => builder.UseSqlServer(connectionString)))
    .AddJob<OrderCreatedHandler>()
        .WithQueueTrigger<OrderCreatedEvent>(options => options.QueueName = "order-created");
```

### 3. Use it

```csharp
public class OrdersService(BankingDbContext dbContext)
{
    public async Task Create(Order order, CancellationToken cancellationToken) {
        dbContext.AddAndEnqueue(order, new OrderCreatedEvent(order.Id));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

Delayed publishing works as with normal `Enqueue`:
```csharp
dbContext.AddAndEnqueue(order, new OrderCreatedEvent(order.Id), DateTime.UtcNow.AddHours(24));
dbContext.AddAndEnqueue(order, new OrderCreatedEvent(order.Id), TimeSpan.FromHours(24));
dbContext.EnqueueRange(orders.Select(x => new OrderCreatedEvent(x.Id)));
```

### Different queues in one transaction
Only events of one message queue type are supported in one `AddAndEnqueueRange` call. If you want multiple commands published, specify them separately, like so:
```csharp
dbContext.Orders.Add(order);
dbContext.Enqueue(new ProcessOrderCommand(order.Id));
dbContext.Enqueue(new CreateAuditEntryCommand(order));
dbContext.Enqueue(new NotifyUserCommand(order.Id), DateTime.UtcNow.AddMinutes(1));
await dbContext.SaveChangesAsync(cancellationToken);
```

### When you own the transaction

Nothing special is needed — the staged messages are written by your `SaveChangesAsync()` and commit or roll
back with your transaction:

```csharp
await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

dbContext.AddAndEnqueue(order, new OrderCreatedEvent(order.Id));
await dbContext.SaveChangesAsync(cancellationToken);
await someOtherService.DoWork(cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

Note: Any `IMessageQueue<T>` inside a transaction will be committed/rolled back with the transaction.