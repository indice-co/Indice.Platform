# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [8.63.0]
### Added
- The admin UI dashboard and the `riskLevel` filter (`riskLevel::eq::High`, `riskLevel::neq::Low`) on the risk results list (`/api/risk-results`).

### Changed
- Improved the performance of the admin UI dashboard counters and of the default (newest first) ordering of the events and results lists. These queries filter and sort by `CreatedAt`, which had no index, so they scanned the whole `RiskEvent` and `RiskResult` tables. Two new indexes cover them:
  - `IX_RiskEvent_CreatedAt` on `[RiskEvent]([CreatedAt])`.
  - `IX_RiskResult_CreatedAt` on `[RiskResult]([CreatedAt])` with `[RiskLevel]` included, so the per risk level counts are answered from the index alone.
- The indexes are part of the EF Core mapping (`RiskDbContext`) for both SQL Server and PostgreSQL, so new databases created by `EnsureCreated()` get them automatically. **Existing databases** need the script below.

SQL Server: run this migration script to update the database.<br/>
**Caution!** Update the table schema if it is not the default `[dbo]`. Building the indexes on large tables takes time and blocks writes to them, so run it off-peak (or add `WITH (ONLINE = ON)` where the edition supports it: Enterprise, Developer or Azure SQL).
```tsql
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[RiskEvent]')
      AND name = N'IX_RiskEvent_CreatedAt'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RiskEvent_CreatedAt]
        ON [dbo].[RiskEvent]([CreatedAt] ASC);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[RiskResult]')
      AND name = N'IX_RiskResult_CreatedAt'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RiskResult_CreatedAt]
        ON [dbo].[RiskResult]([CreatedAt] ASC)
        INCLUDE ([RiskLevel]);
END;
GO
```

PostgreSQL: run this migration script to update the database.<br/>
**Caution!** Update the table schema if it is not the default `public`.
```sql
-- Run outside of a transaction block (CREATE INDEX CONCURRENTLY does not block writes).
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_RiskEvent_CreatedAt"
    ON public."RiskEvent" ("CreatedAt");

CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_RiskResult_CreatedAt"
    ON public."RiskResult" ("CreatedAt")
    INCLUDE ("RiskLevel");

ANALYZE public."RiskEvent";
ANALYZE public."RiskResult";
```
> If a concurrent build fails it leaves an invalid index behind (`SELECT indexrelid::regclass FROM pg_index WHERE NOT indisvalid;`). Drop it with `DROP INDEX CONCURRENTLY` and run the script again, because `IF NOT EXISTS` skips invalid indexes too.