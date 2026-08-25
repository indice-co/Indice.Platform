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
            options.Description = "A scheduled task that runs an etl pipeline evry day at 2am.";
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
    public async Task Process(Dbcontext dbContext, ILogger<EtlPipelineHandler> logger, CancellationToken cancellationToken) {
        ...
    }
}
```

### Scheduled jobs with state

`WithScheduleTrigger<TState>` gives the handler a persisted state between runs - e.g. watermark for etl.

### Scheduled jobs - one instance only

If you deploy to different machines, you can set `Singleton = true` per scheduled job to run only one instance of a job.

---

## Poison Queue
The messages exceeding the `MaxRetryCount` are moved to the poison queue. The integrator is responsible for processing them if needed.

## Publishing messages

Inject `IMessageQueue<T>` for any type registered with `WithQueueTrigger<T>`:

```csharp
public class AlertService(IMessageQueue<Alert> queue)
{
    await queue.Enqueue(alert);
    await queue.Enqueue(alert, enqueueAt: TimeSpan.FromHours(1));
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

### 1. Implement `ITaskDbContext` on your `DbContext`

```csharp
public class BankingDbContext : DbContext, ITaskDbContext
{
    public BankingDbContext(DbContextOptions<BankingDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);
        builder.ApplyWorkerStoreConfiguration(Database.ProviderName); <-- this is required
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

Note: Any IMessageQueue<T> inside a transaction will be committed/rollbacked with the transaction.