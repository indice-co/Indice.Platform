# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
