# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
