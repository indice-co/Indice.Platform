# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [8.64.0]
### Added
- Run now for scheduled tasks: a worker picks up requests left in the `$run-now:{taskId}` queue (e.g. by the workers dashboard) and runs the task once, even when it is disabled. Relational store only. Configure with `WorkerHostOptions.RunNowPollingInterval` (default 15 seconds, `null` to disable) and `WorkerHostOptions.RunNowExpiration` (default 10 minutes).

### Changed
- The queue table index on `QueueName` is replaced by one on `(QueueName, Date)` that includes `State` and `DequeueCount` (SQL Server). It matches the dequeue query and lets monitoring count messages without reading the payloads. **Add a migration** to the context that holds the worker tables, or run the script below on existing databases:
  - `IX_QMessage_QueueName` on `[work].[QMessage]([QueueName])` is dropped.
  - `IX_QMessage_QueueName_Date` on `[work].[QMessage]([QueueName], [Date])` is added, with `[State]` and `[DequeueCount]` included on SQL Server. PostgreSQL gets the two-column index without them.

SQL Server: run this migration script to update the database.<br/>
**Caution!** Building the index on a large queue table takes time and blocks writes to it, so run it off-peak (or add `WITH (ONLINE = ON)` where the edition supports it: Enterprise, Developer or Azure SQL).
```tsql
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[work].[QMessage]')
      AND name = N'IX_QMessage_QueueName_Date'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_QMessage_QueueName_Date]
        ON [work].[QMessage]([QueueName] ASC, [Date] ASC)
        INCLUDE ([State], [DequeueCount]);
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[work].[QMessage]')
      AND name = N'IX_QMessage_QueueName'
)
BEGIN
    DROP INDEX [IX_QMessage_QueueName] ON [work].[QMessage];
END;
GO
```

PostgreSQL: run this migration script to update the database.
```sql
-- Run outside of a transaction block (CREATE/DROP INDEX CONCURRENTLY does not block writes).
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_QMessage_QueueName_Date"
    ON work."QMessage" ("QueueName", "Date");

DROP INDEX CONCURRENTLY IF EXISTS work."IX_QMessage_QueueName";

ANALYZE work."QMessage";
```
> If a concurrent build fails it leaves an invalid index behind (`SELECT indexrelid::regclass FROM pg_index WHERE NOT indisvalid;`). Drop it with `DROP INDEX CONCURRENTLY` and run the script again, because `IF NOT EXISTS` skips invalid indexes too.

## ## [8.60.0] - 2026-10-07
### Fixed
- Fixed an EF Core multiple-operation error caused by exclusive runs in singleton jobs.

## [8.55.0] - 2026-09-01
### Added
- Added transactional outbox support by using `AddAndEnqueue()`, `AddAndEnqueueRange()`, `Enqueue()` and `EnqueueRange()` on your own context.

### Fixed
- `EnqueueRange` no longer re-sends the first 1000 items on every batch after the first.
- `UseStoreRelational<TContext>()` with a custom context no longer constructs a second instance of it when resolving `ITaskDbContext`.

### Changed
- Integrator's dbContext now has the Indice.Hosting migrations applied correctly.
- Queue commands are now inside the transaction of the integrator's context when there is one (e.g., with `BeginTransactionAsync()`).
If you used `UseStoreRelational<TContext>()` with a custom context and published inside a custom transaction expecting outbox working:
```csharp
await using (var transaction = await _integratorDbContext.Database.BeginTransactionAsync()) {
            _integratorDbContext.Add(new TestEntity { Id = _businessId });
            await _queue.Enqueue(new TestEvent(_businessId));
            await _integratorDbContext.SaveChangesAsync();
            await transaction.CommitAsync(); // or RollbackAsync()
        }
```
this now works transactionally as well.

## [8.29.0] - 2025-12-15
### Added
Added PostgreSQL-specific handling for the RowVersion concurrency token using the bytea column type with a custom default value expression. Also updated both SQL Server and PostgreSQL dequeue queries to explicitly list all returned columns instead of using wildcards.
- Added DbQMessagePostgreSQLMap to configure PostgreSQL-specific RowVersion handling with bytea type and default value generation
- TaskDbContext now conditionally applies PostgreSQL mapping when using Npgsql provider
- Updated SQL Server and PostgreSQL dequeue queries to explicitly specify all returned columns

## [8.0.0] - 2025-03-06
### Added
- Added the ability to configure the `MaxRetryCount` for a failed message when using Queue Triggers to specify the number of times the message is re-enqueued before being moved to the poison queue.
- Added the ability to configure `WaitJobsToCompleteOnShutdown` on `AddWorkerHost` to specify whether the server should wait for all jobs to complete before shutting down.
- Added support for `ScheduledTaskStoreInMemory`.

### Fixed
- Trying to resolve an unregistered service dependency from the DI in a job will now throw an exception instead of returning null. Make sure no unregistered dependencies are used in your Jobs.

### Changed
- Default retry times for failed messages in Queue Triggers now defaults to **3** instead of **5** as a more sensible default. Clients that used the previous value as a feature should use the new `RetryCount` option.
- Cancellation Token will be correctly passed to Handlers during Host Shutdown.