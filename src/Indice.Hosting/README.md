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

Use `SaveAndEnqueueAsync` if you want Outbox behavior.

### Requirements

Your `DbContext` must target the same database as the worker store.

### Normal use

```csharp
public class OrdersService(BankingDbContext dbContext, IMessageQueue<OrderCreatedEvent> queue)
{
    public async Task Create(Order order, CancellationToken cancellationToken) {
        dbContext.Order.Add(order);
        await dbContext.SaveAndEnqueueAsync(queue, new OrderCreatedEvent(order.Id), cancellationToken: cancellationToken);
    }
}
```

Multiple events of the same type as well as Delayed publishing as with normal `Enqueue`:
```csharp
await dbContext.SaveAndEnqueueAsync(queue, new OrderCreatedEvent(order.Id), DateTime.UtcNow.AddHours(24), cancellationToken);
await dbContext.SaveAndEnqueueAsync(queue, new OrderCreatedEvent(order.Id), TimeSpan.FromHours(24), cancellationToken);
await dbContext.SaveAndEnqueueAsync(queue, orders.Select(x => new OrderCreatedEvent(x.Id)), cancellationToken: cancellationToken);
```

### When the Integrator owns the transaction
If a transaction is already open, `SaveAndEnqueueAsync` joins it and **does not commit** — you are responsible for that:

```csharp
await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

dbContext.Orders.Add(order);
await dbContext.SaveAndEnqueueAsync(queue, new OrderCreatedEvent(alert.Id), cancellationToken: cancellationToken);
await someOtherService.DoWork(cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

Or with different queues:
```csharp
await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
await dbContext.SaveAndEnqueueAsync(orderQueue, new OrderCreatedEvent(order.Id), cancellationToken: cancellationToken);
await dbContext.SaveAndEnqueueAsync(notifyUserQueue, new NotifyUserEvent(order.Id), cancellationToken: cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

### With `OutboxBatch`

The same thing in one call, when you do not manage the transaction yourself:

```csharp
dbContext.Orders.Add(order);
await dbContext.SaveAndEnqueueAsync(batch => batch
    .Add(orderQueue, new OrderCreatedEvent(order.Id))
    .Add(auditQueue, new AuditEntryCreatedEvent(order))
    .Add(notifyUserQueue, new NotifyUserEvent(alert.Id), DateTime.UtcNow.AddMinutes(1)),
    cancellationToken: cancellationToken);
```

**Keep transactions short.** The message insert holds row locks until commit.