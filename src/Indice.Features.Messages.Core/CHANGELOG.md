# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
  Intentioanally added for naming consistency. external MessageId == internal CampaignId.

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
