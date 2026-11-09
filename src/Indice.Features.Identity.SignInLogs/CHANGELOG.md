# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [8.62.0]
### Changed
- Improved the performance of the sign in log widgets on the Admin UI dashboard with three index changes on `[auth].[SignInLog]`:
  - `IX_SignInLog_CreatedAt` now INCLUDEs `Succeeded` and `SubjectId`. This covers the daily sign in series (`/api/sign-in-logs/series`) and the failed sign ins of the last 24 hours.
  - New `IX_SignInLog_CreatedAt_Geo`, a filtered (`CountryIsoCode IS NOT NULL`) covering index for the sign in locations map (`/api/sign-in-logs/locations`).
  - New `IX_SignInLog_Review` for the "marked for review" count. It is intentionally not filtered, because the flag is sent as a query parameter.
- The indexes are part of the EF Core mapping (`DbSignInLogEntryMap`) for both SQL Server and PostgreSQL. The filter is generated in the dialect of the configured provider. New databases created by `EnsureCreated()` get them automatically. **Existing databases** need the scripts below.

SQL Server: run this migration script to update the database (it is safe to run more than once)
```tsql
-- Filtered indexes require these SET options (sqlcmd defaults QUOTED_IDENTIFIER to OFF).
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1) Make [IX_SignInLog_CreatedAt] covering for the daily series and the failed sign in counts.
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes AS i
    JOIN sys.index_columns AS ic ON ic.[object_id] = i.[object_id] AND ic.index_id = i.index_id AND ic.is_included_column = 1
    JOIN sys.columns AS c ON c.[object_id] = ic.[object_id] AND c.column_id = ic.column_id
    WHERE i.[object_id] = OBJECT_ID(N'[auth].[SignInLog]') AND i.[name] = N'IX_SignInLog_CreatedAt' AND c.[name] = N'SubjectId'
)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_SignInLog_CreatedAt' AND [object_id] = OBJECT_ID(N'[auth].[SignInLog]'))
        CREATE NONCLUSTERED INDEX [IX_SignInLog_CreatedAt]
            ON [auth].[SignInLog]([CreatedAt] ASC)
            INCLUDE ([Succeeded], [SubjectId])
            WITH (DROP_EXISTING = ON);
    ELSE
        CREATE NONCLUSTERED INDEX [IX_SignInLog_CreatedAt]
            ON [auth].[SignInLog]([CreatedAt] ASC)
            INCLUDE ([Succeeded], [SubjectId]);
END
GO

-- 2) Filtered covering index for the sign in locations map.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_SignInLog_CreatedAt_Geo' AND [object_id] = OBJECT_ID(N'[auth].[SignInLog]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SignInLog_CreatedAt_Geo]
        ON [auth].[SignInLog]([CreatedAt] ASC)
        INCLUDE ([CountryIsoCode], [Location], [Coordinates])
        WHERE [CountryIsoCode] IS NOT NULL;
END
GO

-- 3) Index for the "marked for review" count.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_SignInLog_Review' AND [object_id] = OBJECT_ID(N'[auth].[SignInLog]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SignInLog_Review]
        ON [auth].[SignInLog]([Review] ASC);
END
GO
```
> `SignInLog` tables tend to be large. Run the script off-peak, or add `ONLINE = ON` to the `WITH` options on Enterprise, Developer or Azure SQL.

PostgreSQL: run this migration script to update the database
```sql
-- Run outside of a transaction block (CREATE/DROP INDEX CONCURRENTLY does not block writes).

-- 1) Make "IX_SignInLog_CreatedAt" covering: build the new index, drop the old one and take its name.
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_SignInLog_CreatedAt_new"
    ON auth."SignInLog" ("CreatedAt")
    INCLUDE ("Succeeded", "SubjectId");
DROP INDEX CONCURRENTLY IF EXISTS auth."IX_SignInLog_CreatedAt";
ALTER INDEX IF EXISTS auth."IX_SignInLog_CreatedAt_new" RENAME TO "IX_SignInLog_CreatedAt";

-- 2) Partial covering index for the sign in locations map.
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_SignInLog_CreatedAt_Geo"
    ON auth."SignInLog" ("CreatedAt")
    INCLUDE ("CountryIsoCode", "Location", "Coordinates")
    WHERE "CountryIsoCode" IS NOT NULL;

-- 3) Index for the "marked for review" count.
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_SignInLog_Review"
    ON auth."SignInLog" ("Review");

ANALYZE auth."SignInLog";
```
> Step 1 always rebuilds `IX_SignInLog_CreatedAt`, so run it only once. If a concurrent build fails it leaves an invalid index behind (`SELECT indexrelid::regclass FROM pg_index WHERE NOT indisvalid;`). Drop it with `DROP INDEX CONCURRENTLY` and run the statement again.
> The scripts use the default `auth` schema. Adjust it if `SignInLogOptions.DatabaseSchema` is configured differently.

### Fixed
- The sign in log cleanup (`ISignInLogStore.Cleanup`) now works on PostgreSQL. It used `EF.Functions.DateDiffDay`, which only SQL Server can translate. The query is now chosen by `Database.ProviderName`: SQL Server keeps `DATEDIFF(day, ...)`, and PostgreSQL compares `CreatedAt` with a UTC midnight cutoff. Both delete the same rows.

## [7.11.8] - 2023-12-11
### Added
- Created new field called **EventType** on **SignInLog** table. In order to create the new table run the following script:
```sql
CREATE TABLE [auth].[SignInLog] (
    [Id]              UNIQUEIDENTIFIER   NOT NULL,
    [CreatedAt]       DATETIMEOFFSET (7) NOT NULL,
    [EventType]       INT                NOT NULL,
    [ActionName]      NVARCHAR (256)     NULL,
    [ApplicationId]   NVARCHAR (128)     NULL,
    [ApplicationName] NVARCHAR (512)     NULL,
    [SubjectId]       NVARCHAR (128)     NULL,
    [SubjectName]     NVARCHAR (512)     NULL,
    [ResourceId]      NVARCHAR (128)     NULL,
    [ResourceType]    NVARCHAR (64)      NULL,
    [Description]     NVARCHAR (2048)    NULL,
    [Succeeded]       BIT                NOT NULL,
    [IpAddress]       NVARCHAR (128)     NULL,
    [RequestId]       NVARCHAR (128)     NULL,
    [Location]        NVARCHAR (512)     NULL,
    [SessionId]       NVARCHAR (128)     NULL,
    [SignInType]      INT                NULL,
    [Review]          BIT                NOT NULL,
    [CountryIsoCode]  NVARCHAR (8)       NULL,
    [DeviceId]        NVARCHAR (128)     NULL,
    [Coordinates]     [sys].[geography]  NULL,
    [GrantType]       NVARCHAR (32)      NULL,
    [ExtraData]       NVARCHAR (MAX)     NULL,
    CONSTRAINT [PK_SignInLog] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_SignInLog_ApplicationId]
    ON [auth].[SignInLog]([ApplicationId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_SignInLog_CreatedAt]
    ON [auth].[SignInLog]([CreatedAt] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_SignInLog_EventType]
    ON [auth].[SignInLog]([EventType] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_SignInLog_SessionId]
    ON [auth].[SignInLog]([SessionId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_SignInLog_SubjectId]
    ON [auth].[SignInLog]([SubjectId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_SignInLog_SubjectName]
    ON [auth].[SignInLog]([SubjectName] ASC);
GO
```

If you need to delete the existing table please run the following command:
```sql
DROP TABLE IF EXISTS [auth].[SignInLog];
```

- Added the ability to issue an `id_token` in the context of a `password` grant token request. To enable this feature add the following option in the `AddExtendedResourceOwnerPasswordValidator` extension method:
```csharp
services.AddIdentityServer
        .AddExtendedResourceOwnerPasswordValidator(options => options.IncludeIdToken = true)
        /* rest of the configuration */
```
