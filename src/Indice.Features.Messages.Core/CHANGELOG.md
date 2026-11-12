# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [8.65.0]
### Added `ReceiptId` column to message events
`MessageEvent.ReceiptId` stores the message id returned by the sms or email provider when a message is sent (`Sent` events).
Provider webhook events (delivered, opened, bounced etc.) carry the same id, so they are matched to the sent message through it and
stored with its campaign, contact and message. The webhook events also keep the provider id in `ReceiptId`.
Events stored before this change have no `ReceiptId`.

Run the following migration script. It can safely run more than once.<br/>
**Caution!** Update table schema if not [cmp]. Building the index on a large table takes time and blocks writes to it, so run it off-peak (or add `WITH (ONLINE = ON)` where the edition supports it).
```tsql
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[cmp].[MessageEvent]')
      AND name = N'ReceiptId'
)
BEGIN
    ALTER TABLE [cmp].[MessageEvent]
        ADD [ReceiptId] NVARCHAR(128) NULL;
END;
GO

-- Webhook events are matched to the sent message by receipt id and event type.
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[cmp].[MessageEvent]')
      AND name = N'IX_MessageEvent_ReceiptId_Type'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MessageEvent_ReceiptId_Type]
        ON [cmp].[MessageEvent]([ReceiptId] ASC, [Type] ASC);
END;
GO
```

PostgreSQL (11 or later)
```sql
ALTER TABLE cmp."MessageEvent" ADD COLUMN IF NOT EXISTS "ReceiptId" character varying(128) NULL;

-- Webhook events are matched to the sent message by receipt id and event type.
CREATE INDEX IF NOT EXISTS "IX_MessageEvent_ReceiptId_Type" ON cmp."MessageEvent" ("ReceiptId", "Type");
```

### Added `Orphan` flag to message events
Provider webhook events that cannot be matched to a sent campaign message are stored with an empty (`Guid.Empty`) `CampaignId` and `ContactId`.
The new `MessageEvent.Orphan` column marks these events, and they are excluded from the dashboard and the analytics
(delivery, recipient, channel and message type metrics, event series). They are still returned by the events list.
The column is also included in `IX_MessageEvent_CampaignId_Type`, so the analytics keep reading only the index.

Run the following migration script. It can safely run more than once.<br/>
**Caution!** Update table schema if not [cmp]. Rebuilding the index on a large table takes time and blocks writes to it, so run it off-peak (or add `ONLINE = ON` where the edition supports it).
```tsql
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[cmp].[MessageEvent]')
      AND name = N'Orphan'
)
BEGIN
    ALTER TABLE [cmp].[MessageEvent]
        ADD [Orphan] BIT NOT NULL
            CONSTRAINT [DF_MessageEvent_Orphan] DEFAULT (0);
END;
GO

-- Add the column to the included columns of the campaign index (or create the index if missing).
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic ON ic.[object_id] = i.[object_id] AND ic.[index_id] = i.[index_id]
    INNER JOIN sys.columns c ON c.[object_id] = ic.[object_id] AND c.[column_id] = ic.[column_id]
    WHERE i.[object_id] = OBJECT_ID(N'[cmp].[MessageEvent]')
      AND i.[name] = N'IX_MessageEvent_CampaignId_Type'
      AND c.[name] = N'Orphan'
)
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'[cmp].[MessageEvent]')
          AND name = N'IX_MessageEvent_CampaignId_Type'
    )
        CREATE NONCLUSTERED INDEX [IX_MessageEvent_CampaignId_Type]
            ON [cmp].[MessageEvent]([CampaignId] ASC, [Type] ASC)
            INCLUDE ([Channel], [MessageId], [ContactId], [Success], [Orphan])
            WITH (DROP_EXISTING = ON);
    ELSE
        CREATE NONCLUSTERED INDEX [IX_MessageEvent_CampaignId_Type]
            ON [cmp].[MessageEvent]([CampaignId] ASC, [Type] ASC)
            INCLUDE ([Channel], [MessageId], [ContactId], [Success], [Orphan]);
END;
GO
```

PostgreSQL (11 or later)
```sql
ALTER TABLE cmp."MessageEvent" ADD COLUMN IF NOT EXISTS "Orphan" boolean NOT NULL DEFAULT FALSE;

-- Included columns cannot be altered, so the campaign index is created again with the new column.
DROP INDEX IF EXISTS cmp."IX_MessageEvent_CampaignId_Type";
CREATE INDEX "IX_MessageEvent_CampaignId_Type" ON cmp."MessageEvent" ("CampaignId", "Type") INCLUDE ("Channel", "MessageId", "ContactId", "Success", "Orphan");
```


## [8.64.0]
### Added real time inbox notifications
`IInboxNotifier` is invoked whenever an inbox message is created: per recipient when a campaign is resolved, once for everybody for global campaigns.
The default implementation does nothing. Call `UseInboxSignalRNotifications()` on the worker (`AddMessageFunctions` / `AddMessageJobs`) or api (`AddMessaging`) options
to push an `InboxMessageNotification` (method `inboxMessageCreated`) to the recipient through the SignalR proxy (`ConnectionStrings:SignalR`).
Inbox clients connect through the `MapSignalRProxy()` endpoints of the api; the hub (default `messaging`) must be listed in `SignalRProxy:AllowedHubs`.

```csharp
builder.AddMessageFunctions(options => options.UseInboxSignalRNotifications(o => o.HubName = "messaging"));
```

### Added recipient counts to campaigns and distribution lists
`Campaign.RecipientCount` stores how many contacts a published campaign was sent out to (null for drafts and global campaigns).
`DistributionList.ContactCount` stores how many contacts a list currently has (unsubscribed ones included) and is recounted after every membership change.
Both are returned by the campaign and distribution list endpoints.
The recipient analytics (`RecipientMetrics.Total`, campaign statistics and overview) now read `Campaign.RecipientCount` instead of counting the current list members, so a campaign reports the audience it was actually sent to. Campaigns that have not been sent to a distribution list (drafts, global campaigns) count as 0.

Run the following migration script.<br/>
**Caution!** Update table schema if not [cmp]
```tsql
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[cmp].[Campaign]')
      AND name = N'RecipientCount'
)
BEGIN
    ALTER TABLE [cmp].[Campaign]
        ADD [RecipientCount] INT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[cmp].[DistributionList]')
      AND name = N'ContactCount'
)
BEGIN
    ALTER TABLE [cmp].[DistributionList]
        ADD [ContactCount] INT NOT NULL
            CONSTRAINT [DF_DistributionList_ContactCount] DEFAULT (0);
END;
GO

-- Backfill the current size of every list.
UPDATE dl
SET [ContactCount] = (
    SELECT COUNT(*)
    FROM [cmp].[DistributionListContact] dlc
    WHERE dlc.[DistributionListId] = dl.[Id]
)
FROM [cmp].[DistributionList] dl;
GO

-- Backfill published, non-global campaigns from the current size of their list.
-- Exact for system generated lists; approximate for shared lists that changed after the send.
UPDATE c
SET [RecipientCount] = dl.[ContactCount]
FROM [cmp].[Campaign] c
INNER JOIN [cmp].[DistributionList] dl ON dl.[Id] = c.[DistributionListId]
WHERE c.[Published] = 1
  AND c.[IsGlobal] = 0
  AND c.[RecipientCount] IS NULL;
GO
```

### Faster analytics overview and user inbox
The user inbox (`my/messages`) no longer scans every campaign: it starts from the recipient's own messages (plus the global campaigns), so recipients
with few or no messages get their inbox and unread count in milliseconds instead of seconds. On SQL Server the inbox query is tagged and gets
`OPTION (RECOMPILE, USE HINT('DISABLE_OPTIMIZER_ROWGOAL'))` (`QueryHintsInterceptor`, registered by `CampaignsDbContext`), so a plan built for one recipient
is not reused for a very different one. Other providers (PostgreSQL) run the query unchanged.
The analytics overview computes its metrics concurrently, and the active campaigns, recipient and message type metrics use fewer, cheaper queries.

New indexes (`IX_Message_RecipientId_CampaignId` replaces `IX_Message_RecipientId`):
- `IX_Campaign_Published_IsGlobal` on `Campaign (Published, IsGlobal) INCLUDE (TypeId, RecipientCount)`
- `IX_Campaign_From_To` on `Campaign (From, To) INCLUDE (Published)`
- `IX_Message_RecipientId_CampaignId` on `Message (RecipientId, CampaignId) INCLUDE (IsDeleted, IsRead)`

Run the following migration script. Both scripts can safely run more than once.<br/>
**Caution!** Update table schema if not [cmp]

SQL Server
```tsql
-- Creating the indexes blocks writes to the table while they build. On Enterprise edition, Azure SQL Database and Managed Instance
-- add WITH (ONLINE = ON) to each CREATE INDEX to keep the tables writable.
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[cmp].[Campaign]')
      AND name = N'IX_Campaign_Published_IsGlobal'
)
BEGIN
    CREATE INDEX [IX_Campaign_Published_IsGlobal] ON [cmp].[Campaign] ([Published], [IsGlobal]) INCLUDE ([TypeId], [RecipientCount]);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[cmp].[Campaign]')
      AND name = N'IX_Campaign_From_To'
)
BEGIN
    CREATE INDEX [IX_Campaign_From_To] ON [cmp].[Campaign] ([From], [To]) INCLUDE ([Published]);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[cmp].[Message]')
      AND name = N'IX_Message_RecipientId_CampaignId'
)
BEGIN
    CREATE INDEX [IX_Message_RecipientId_CampaignId] ON [cmp].[Message] ([RecipientId], [CampaignId]) INCLUDE ([IsDeleted], [IsRead]);
END;
GO

-- Superseded by IX_Message_RecipientId_CampaignId, dropped only after the new index exists.
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[cmp].[Message]')
      AND name = N'IX_Message_RecipientId'
)
BEGIN
    DROP INDEX [IX_Message_RecipientId] ON [cmp].[Message];
END;
GO
```

PostgreSQL (11 or later)
```sql
-- Creating the indexes blocks writes to the table while they build. To keep the tables writable use CREATE INDEX CONCURRENTLY IF NOT EXISTS
-- (outside of a transaction). If a concurrent build fails it leaves an invalid index behind: drop it before running the script again.
CREATE INDEX IF NOT EXISTS "IX_Campaign_Published_IsGlobal" ON cmp."Campaign" ("Published", "IsGlobal") INCLUDE ("TypeId", "RecipientCount");

CREATE INDEX IF NOT EXISTS "IX_Campaign_From_To" ON cmp."Campaign" ("From", "To") INCLUDE ("Published");

CREATE INDEX IF NOT EXISTS "IX_Message_RecipientId_CampaignId" ON cmp."Message" ("RecipientId", "CampaignId") INCLUDE ("IsDeleted", "IsRead");

-- Superseded by IX_Message_RecipientId_CampaignId, dropped only after the new index exists.
DROP INDEX IF EXISTS cmp."IX_Message_RecipientId";
```

## [8.62.0]
### Added flag for system generated distribution lists

Run the following migration script.<br/>
**Caution!** Update table schema if not [cmp]
```tsql
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[cmp].[DistributionList]')
      AND name = N'SystemGenerated'
)
BEGIN
    ALTER TABLE [cmp].[DistributionList]
        ADD [SystemGenerated] BIT NOT NULL
            CONSTRAINT [DF_DistributionList_SystemGenerated] DEFAULT (0);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[cmp].[DistributionList]')
      AND name = N'IX_DistributionList_SystemGenerated'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_DistributionList_SystemGenerated]
        ON [cmp].[DistributionList]([SystemGenerated] ASC);
END;
GO

UPDATE [cmp].[DistributionList]
SET [SystemGenerated] = 1
WHERE [CreatedBy] = 'system';
GO
```

### Added message event indexes for the analytics and the events list
The dashboard overview, the campaign reports and the events list aggregate or page the `MessageEvent` table, which had no index for them.

Run the following migration script.<br/>
**Caution!** Update table schema if not [cmp]. Building the indexes on a large table takes time and blocks writes to it, so run it off-peak (or add `WITH (ONLINE = ON)` where the edition supports it).
```tsql
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[cmp].[MessageEvent]')
      AND name = N'IX_MessageEvent_CreatedOn'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MessageEvent_CreatedOn]
        ON [cmp].[MessageEvent]([CreatedOn] ASC);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[cmp].[MessageEvent]')
      AND name = N'IX_MessageEvent_CampaignId_Type'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MessageEvent_CampaignId_Type]
        ON [cmp].[MessageEvent]([CampaignId] ASC, [Type] ASC)
        INCLUDE ([Channel], [MessageId], [ContactId], [Success]);
END;
GO
```

### Added the `concat` Handlebars helper
Message templates (and the partials/layouts they use) can join values into one string, e.g. to build a link passed to a partial:
```handlebars
{{> text_link url=(concat data.uiBaseUrl "/my-profile/buying/" data.propertyInfo.code "/documents") text="My documents"}}
```
Missing values render as empty. The helper is registered on every server render and in the admin UI preview. Templates that use it fail to render on servers running an older version, so deploy this version before authoring them.

## [8.47.0] - 2026-05-13
### Added partial template functionality

To categorize the new template types a new column must be added. This column is not nullable, defaults to 0 (Full template) and all the existing templates should be categorized as Full.
<br>
Example script:
```sql
ALTER TABLE [cmp].[Template]
ADD [Type] TINYINT NOT NULL 
CONSTRAINT DF_Template_TemplateType DEFAULT 0;
```
**Note:** This example assumes SQLServer.

## [8.31.0] - 2025-01-07
### Added Messaging Database Cleanup Job
The following configuration should be added in the appsettings.json and local.settings.json to determine the frequency of the clean up job:
``` json
"MessageJobsOptions:DatabaseCleanUpCronExpression": "0 0 2 * * *"
```
**Note:** This configuration is required for Azure Worker deployments. For self-hosted workers, 
this value is used as the default if not specified.

## [8.21.0] - 2025-10-27
### Added New column Recipient in MessageEvent table
Keep track of the actual recipient (email, phone number, etc) in the MessageEvent table for easier querying and reporting.

```sql
-- 1) Add new columns to [cmp].[MessageEvent]
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns 
    WHERE Name = N'Recipient' AND Object_ID = Object_ID(N'[cmp].[MessageEvent]')
)
BEGIN
    ALTER TABLE [cmp].[MessageEvent]
    ADD Recipient NVARCHAR(128),
        Title NVARCHAR(128),
        Success BIT;
END
GO

-- 2) Update existing records to populate the new Recipient, Title and Success columns
UPDATE [cmp].[MessageEvent]
SET Recipient = 
    CASE Events.Channel
         WHEN 'SMS' THEN ct.PhoneNumber
         WHEN 'Email' THEN ct.Email
         ELSE COALESCE(ct.RecipientId, '')
    END,
    Success = 1,
    Title  =   CASE 
         WHEN Events.Channel = 'SMS' AND msg.Content IS NOT NULL THEN COALESCE(JSON_VALUE(msg.Content, '$.sms.title'), JSON_VALUE(msg.Content, '$.SMS.title'))
         WHEN Events.Channel = 'Email' AND msg.Content IS NOT NULL  THEN COALESCE(JSON_VALUE(msg.Content, '$.email.title'), JSON_VALUE(msg.Content, '$.Email.title'))
         WHEN Events.Channel = 'Inbox' AND msg.Content IS NOT NULL THEN COALESCE(JSON_VALUE(msg.Content, '$.inbox.title'), JSON_VALUE(msg.Content, '$.Inbox.title'))
         WHEN msg.Content IS NOT NULL THEN COALESCE(JSON_VALUE(msg.Content, '$.pushNotification.title'), JSON_VALUE(msg.Content, '$.PushNotification.title'))
         ELSE '(deleted)'
    END
FROM  [cmp].[MessageEvent] as Events
INNER JOIN [cmp].Contact as ct
 ON Events.ContactId = ct.Id
LEFT JOIN [cmp].Message as msg
 ON msg.Id = Events.MessageId
```

## [8.17.3] - 2025-09-30
### Added New column
```sql
ALTER TABLE [cmp].[Contact] 
    ADD [LastResolutionDate]     [datetimeoffset](7)      NULL
GO
```


## [8.16.0] - 2025-09-19

### Added New column
```sql
ALTER TABLE [cmp].[Contact] 
    ADD [Resolved]     bit      NULL
GO
CREATE NONCLUSTERED INDEX [IX_Contact_RecipientId_Resolved] 
    ON [cmp].[Contact] ([RecipientId] ASC, [Resolved] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_Campaign_CreatedAt]
    ON [cmp].[Campaign] ([CreatedAt] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_MessageEvent_Type]
    ON [cmp].[MessageEvent] ([Type] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_MessageEvent_Channel]
    ON [cmp].[MessageEvent] ([Channel] ASC)
GO
```

## [8.1.10] - 2025-08-05

### Added support alias in Distribution list
```sql
ALTER TABLE [msg].[DistributionList] 
    ADD [Alias]     NVARCHAR (64)      NULL
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_DistributionList_Alias]
    ON [msg].[DistributionList]([Alias] ASC) WHERE ([Alias] IS NOT NULL);
GO
```

### Added support to persist communication preferences for users.
Add Message type in template table for linking templates to message types and eventually preferences.
```sql
ALTER TABLE [#schema#].[Template]
	ADD [MessageTypeId]  UNIQUEIDENTIFIER  
ALTER TABLE [#schema#].[Template] WITH NOCHECK
    ADD CONSTRAINT [FK_Template_MessageType_MessageTypeId] FOREIGN KEY ([MessageTypeId]) REFERENCES [msg].[MessageType] ([Id]);
ALTER TABLE [#schema#].[Template] WITH CHECK CHECK CONSTRAINT [FK_Template_MessageType_MessageTypeId];
```

The following migration script is needed to add the `ContactPreference` and `ContactCommunicationOption` tables.
```sql
CREATE TABLE [#schema#].[ContactPreference] (
    [Id]                    UNIQUEIDENTIFIER   NOT NULL,
    [RecipientId]           NVARCHAR (64)      NOT NULL,
    [Locale]                NVARCHAR (16)      NULL,
    [ConsentCommercial]     BIT                NOT NULL,
    [ConsentCommercialDate] DATETIMEOFFSET (7) NULL,
    [DefaultChannels]       TINYINT            NULL,
    [UpdatedAt]             DATETIMEOFFSET (7) NULL,
    CONSTRAINT [PK_ContactPreference] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_ContactPreference_RecipientId]
    ON [#schema#].[ContactPreference]([RecipientId] ASC);
GO

CREATE TABLE [#schema#].[ContactCommunicationOption] (
    [ContactPreferenceId] UNIQUEIDENTIFIER   NOT NULL,
    [MessageTypeId]       UNIQUEIDENTIFIER   NOT NULL,
    [Channels]            TINYINT            DEFAULT (CONVERT([tinyint],(0))) NOT NULL,
    [UpdatedAt]           DATETIMEOFFSET (7) NULL,
    CONSTRAINT [PK_ContactCommunicationOption] PRIMARY KEY CLUSTERED ([ContactPreferenceId] ASC, [MessageTypeId] ASC),
    CONSTRAINT [FK_ContactCommunicationOption_ContactPreference_ContactPreferenceId] FOREIGN KEY ([ContactPreferenceId]) REFERENCES [#schema#].[ContactPreference] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ContactCommunicationOption_MessageType_MessageTypeId] FOREIGN KEY ([MessageTypeId]) REFERENCES [#schema#].[MessageType] ([Id]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_ContactCommunicationOption_MessageTypeId]
    ON [#schema#].[ContactCommunicationOption]([MessageTypeId] ASC);
GO

```

In case that you have used communication preferences in your project, you need to run the following migration script to populate the `CommunicationPreference` and `CommunicationPreferenceMessageType` tables.
```sql

CREATE TABLE #TempContactPreference
(
    [RecipientId] nvarchar(64) NOT NULL,
    [Locale] nvarchar(16) NULL,
    [ConsentCommercial] bit NOT NULL DEFAULT(0),
    [DefaultChannels] TINYINT NULL
);

INSERT INTO #TempContactPreference
    ([RecipientId]
    ,[Locale]
    ,[ConsentCommercial]
    ,[DefaultChannels])
SELECT RecipientId, Locale, ConsentCommercial,CommunicationPreferences
FROM (
    SELECT RecipientId, Locale, ConsentCommercial,CommunicationPreferences,
        row_number() over (partition by RecipientId order by [UpdatedAt] desc) as rn
    FROM [msg].[Contact]
    WHERE NULLIF(RecipientId,'') IS NOT NULL
) t
WHERE rn = 1 


INSERT INTO [msg].[ContactPreference]
    ([Id]
    ,[RecipientId]
    ,[Locale]
    ,[ConsentCommercial])
SELECT NEWID(),  RecipientId, Locale, ConsentCommercial
FROM  #TempContactPreference AS CT
WHERE NOT EXISTS (SELECT TOP 1 1 FROM  [msg].[ContactPreference] WHERE RecipientId = ct.RecipientId)


DROP TABLE #TempContactPreference    

```

The last step is to drop the prefernece columns from the `Contact` table.
```sql
ALTER TABLE [#schema#].[Contact] DROP COLUMN [CommunicationPreferences], COLUMN [ConsentCommercial], COLUMN [Locale];
```

## [8.1.0] - 2025-06-15
### For performance reasons, the following indexes were added to the `media` schema in cases db.
```sql

CREATE NONCLUSTERED INDEX [IX_MediaFile_FolderId]
    ON [media].[MediaFile]([FolderId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_MediaFile_Name]
    ON [media].[MediaFile]([Name] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_MediaFile_Path]
    ON [media].[MediaFile]([Path] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_MediaFolder_Name]
    ON [media].[MediaFolder]([Name] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_MediaFolder_ParentId]
    ON [media].[MediaFolder]([ParentId] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_MediaFolder_Path]
    ON [media].[MediaFolder]([Path] ASC);
GO

```
### For performance reasons, the following indexes were added to the `messaging` schema in cases db.
```sql

CREATE NONCLUSTERED INDEX [IX_Campaign_AttachmentId]
    ON [#Schema#].[Campaign]([AttachmentId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Campaign_DistributionListId]
    ON [#Schema#].[Campaign]([DistributionListId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Campaign_TypeId]
    ON [#Schema#].[Campaign]([TypeId] ASC);
GO


CREATE UNIQUE NONCLUSTERED INDEX [IX_Contact_RecipientId]
    ON [#Schema#].[Contact]([RecipientId] ASC) WHERE ([RecipientId] IS NOT NULL);
GO


CREATE UNIQUE NONCLUSTERED INDEX [IX_Contact_RecipientId]
    ON [#Schema#].[Contact]([RecipientId] ASC) WHERE ([RecipientId] IS NOT NULL);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_DistributionList_Name]
    ON [#Schema#].[DistributionList]([Name] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_DistributionListContact_DistributionListId]
    ON [#Schema#].[DistributionListContact]([DistributionListId] ASC);
GO


CREATE NONCLUSTERED INDEX [IX_Hit_CampaignId]
    ON [#Schema#].[Hit]([CampaignId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Message_CampaignId]
    ON [#Schema#].[Message]([CampaignId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Message_RecipientId]
    ON [#Schema#].[Message]([RecipientId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_MessageSender_Sender]
    ON [#Schema#].[MessageSender]([Sender] ASC);
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_MessageType_Alias]
    ON [#Schema#].[MessageType]([Alias] ASC) WHERE ([Alias] IS NOT NULL);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_MessageType_Name]
    ON [#Schema#].[MessageType]([Name] ASC);
GO


CREATE UNIQUE NONCLUSTERED INDEX [IX_Template_Alias]
    ON [#Schema#].[Template]([Alias] ASC) WHERE ([Alias] IS NOT NULL);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Template_Name]
    ON [#Schema#].[Template]([Name] ASC);
GO


```


## [8.0.0-rc32] - 2025-05-25
- Added support for logging message events.
```sql		
CREATE TABLE [#Schema#].[MessageEvent](
	[Id] [uniqueidentifier] NOT NULL,
	[CampaignId] [uniqueidentifier] NOT NULL,
	[ContactId] [uniqueidentifier] NOT NULL,
	[MessageId] [uniqueidentifier] NULL,
	[Type] [nvarchar](64) NOT NULL,
	[Channel] [nvarchar](64) NOT NULL,
	[CreatedOn] [datetimeoffset](7) NOT NULL,
 CONSTRAINT [PK_MessageEvent] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
```

## [8.0.0] - 2024-12-03
- Added support for persisting User communication channel preferences, locale and Consent.
- Campaign and Template define whether user communication preferences must be ingored if needed. 
- Send messages respects user communication preferences unless campaign specifies otherwise

```sql		
ALTER TABLE [#Schema#].[Campaign]
ADD IgnoreUserPreferences BIT DEFAULT(0) NOT NULL;
GO
ALTER TABLE [#Schema#].[Contact]
ADD 
	CommunicationPreferences TINYINT DEFAULT(0) NOT NULL,
	ConsentCommercial		 BIT DEFAULT(0) NOT NULL,
	Locale					 VARCHAR(16);
GO
ALTER TABLE [#Schema#].[DistributionListContact]
ADD Unsubscribed BIT DEFAULT(0) NOT NULL;	
GO
ALTER TABLE [#Schema#].[Template]
ADD IgnoreUserPreferences BIT DEFAULT(0) NOT NULL;
GO
ALTER TABLE [#Schema#].[MessageType]
ADD Classification TINYINT DEFAULT(0) NOT NULL;		
GO
 ```		



## [7.27.0] - 2024-07-26
### Added

- Added support for persisting sample data for facilitating template rendering on message templates.

 ```sql
ALTER TABLE [cmp].[Template]
ADD [Data] [nvarchar](max) NULL
GO
 ```

## [7.23.0] - 2024-05-16
### Added
- New column `MediaBaseHref` in `DbCampaign`
```sql
ALTER TABLE [cmp].[Campaign]
ADD [MediaBaseHref] [nvarchar](1024) NULL
```

## [7.4.4] - 2023-10-04
### Added
- ContactRetainPeriodInDays option to keep in sync a contact with the identity system. 
  After the configured period of time the system patches and updates the contact with the latest values.

## [7.4.1] - 2023-09-22
### Changed
- CampaignId is returned in PushNotification data in property "messageId". 
  Intentionally added for naming consistency. external MessageId == internal CampaignId.

## [7.3.8] - 2023-08-07
### Added
- Message Id is included in PushNotification data
- Inbox service enhanced to return other channels also

## [7.3.7] - 2023-08-02
### Added
- New entity `DbMessageSender`
### Migration
```sql
CREATE TABLE [dbo].[MessageSender](
	[Id] [uniqueidentifier] NOT NULL,
	[Sender] [nvarchar](max) NULL,
	[DisplayName] [nvarchar](max) NULL,
	[Kind] [tinyint] NOT NULL,
	[IsDefault] [bit] NOT NULL,
	[CreatedBy] [nvarchar](max) NULL,
	[CreatedAt] [datetimeoffset](7) NOT NULL,
	[UpdatedBy] [nvarchar](max) NULL,
	[UpdatedAt] [datetimeoffset](7) NULL,
 CONSTRAINT [PK_MessageSender] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
```
