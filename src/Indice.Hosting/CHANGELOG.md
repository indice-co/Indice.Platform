# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased
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