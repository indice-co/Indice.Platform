# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [8.1.7] - 2025-07-24
### Added support to persist communication preferences for users.
The following migration script is needed to add the `CommunicationPreference` and `CommunicationPreferenceMessageType` tables.
```sql
CREATE TABLE [#schema#].[CommunicationPreference] (
    [Id]                    UNIQUEIDENTIFIER   NOT NULL,
    [RecipientId]           NVARCHAR (64)      NOT NULL,
    [Locale]                NVARCHAR (16)      NULL,
    [ConsentCommercial]     BIT                NOT NULL,
    [ConsentCommercialDate] DATETIMEOFFSET (7) NULL,
    CONSTRAINT [PK_CommunicationPreference] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_CommunicationPreference_RecipientId]
    ON [#schema#].[CommunicationPreference]([RecipientId] ASC);
GO


CREATE TABLE [#schema#].[CommunicationPreferenceMessageType] (
    [CommunicationPreferenceId] UNIQUEIDENTIFIER NOT NULL,
    [TypeId]                    UNIQUEIDENTIFIER NOT NULL,
    [CommunicationPreferences]  TINYINT          DEFAULT (CONVERT([tinyint],(0))) NOT NULL,
    CONSTRAINT [PK_CommunicationPreferenceMessageType] PRIMARY KEY CLUSTERED ([CommunicationPreferenceId] ASC, [TypeId] ASC),
    CONSTRAINT [FK_CommunicationPreferenceMessageType_CommunicationPreference_CommunicationPreferenceId] FOREIGN KEY ([CommunicationPreferenceId]) REFERENCES [#schema#].[CommunicationPreference] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CommunicationPreferenceMessageType_MessageType_TypeId] FOREIGN KEY ([TypeId]) REFERENCES [#schema#].[MessageType] ([Id]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_CommunicationPreferenceMessageType_TypeId]
    ON [#schema#].[CommunicationPreferenceMessageType]([TypeId] ASC);
GO

```

In case that you have used communication preferences in your project, you need to run the following migration script to populate the `CommunicationPreference` and `CommunicationPreferenceMessageType` tables.
```sql

    INSERT INTO [#schema#].[CommunicationPreference]
           ([Id]
           ,[RecipientId]
           ,[Locale]
           ,[ConsentCommercial])
    SELECT NEWID(),  RecipientId, Locale, ConsentCommercial
    FROM [#schema#].[Contact] AS CT
    WHERE RecipientId IS NOT null
    AND NOT EXISTS (SELECT TOP 1 1 FROM  [#schema#].[CommunicationPreference] WHERE RecipientId = ct.RecipientId)


    INSERT INTO [#schema#].[CommunicationPreferenceMessageType]
           ([CommunicationPreferenceId]
           ,[TypeId]
           ,[CommunicationPreferences])
    SELECT cp.ID, MT.ID, CT.CommunicationPreferences
    FROM [#schema#].[Contact] AS CT
    INNER JOIN [#schema#].[CommunicationPreference] as cp
    ON cp.[RecipientId] = CT.RecipientId
    CROSS JOIN [#schema#].[MessageType] as MT
    WHERE CT.RecipientId IS NOT null
    AND NOT EXISTS (
    SELECT TOP 1 1 FROM  [#schema#].[CommunicationPreferenceMessageType] WHERE [CommunicationPreferenceId] = cp.ID AND [TypeId] = MT.ID)
```

The last step is to drop the prefernece columns from the `Contact` table.
```sql
ALTER TABLE [cmp].[Contact] 
	DROP COLUMN [CommunicationPreferences], 
	COLUMN [ConsentCommercial], 
	COLUMN [Locale];
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
