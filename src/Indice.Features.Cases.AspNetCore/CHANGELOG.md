﻿# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [7.31.4] - 2024-10-15
### Added
 - Added mb-4 to the every element of the case details
 
### Fixed
 - "Choose file" button now respects readonly option set in layout

## [7.31.3] - 2024-10-15
### Fixed
 - Remove attachment button is now shown only when readonly is not true

## [7.31.2] - 2024-10-11
### Added
- `downloadToDisk` flag on layout file fields. Added proper handling of filenames. Now a file can be either opened in the browser or downloaded to the disk.
- `AddWorkflowAuthoriationPolicy` with a policy name `WorkflowPolicy`, to enforce authorization for elsa api and elsa razor pages.

### Action Required
- You need to add the `AddWorkflowAuthoriationPolicy` to your `AuthorizationConfig` file.

### Fixed
 - You can now edit attachments, regardless if your case is draft or not.
 - You can override an already uploaded attachment. The previous attachement is not removed (yet). 

## [7.31.1] - 2024-10-04
### Added
- Support for filter with user group extractd from claims.

### Changed
- You can now define get all rules for a specific `CaseId`.

### Fixed
- Query issue when both case & checkpoint were specfied but filtered only with one of them.
- Cases Search query & enchancments

## [7.28.5-beta01] - 2024-09-25
### Changed
- AccessRule for cases to enhance the existing member access functionality.
####  More specifically:
- You can now define access to a case based on aof rules that can be based on CaseType or CheckPoint or CaseId or CheckPoint & CaseId 
for a member based on either Role or  GroupId or UserId

### Migrations
Add 3 more columns to `CaseType` table.
```sql
--Optional Step fro back Up
--SELECT * INTO [case].[Member_Backup] FROM [case].[Member]
--Rename table
EXEC sp_rename 'case.Member', 'AccessRule';
--ADD New Columns
ALTER TABLE [case].[AccessRule]
ADD  
	R_CaseId uniqueIdentifier NULL,
	M_GroupId nvarchar(64)  NULL,
	M_UserId nvarchar(64)  NULL,
	AccessLevel INT NOT NULL DEFAULT(0),
	CreatedDate DateTimeOffset(7) NOT NULL
		CONSTRAINT D_caseMember_CreatedOn 
		DEFAULT (GETUTCDATE())--Optional Default-Constraint.
--Alter Existing Columns Names
EXEC sp_rename '[case].[AccessRule].RoleName',  'M_Role', 'COLUMN';
EXEC sp_rename '[case].[AccessRule].[CaseTypeId]',  'R_CaseTypeId', 'COLUMN';
EXEC sp_rename '[case].[AccessRule].[CheckpointTypeId]',  'R_CheckpointTypeId', 'COLUMN';
--Alter Existing Columns allow nulls
ALTER TABLE [case].AccessRule ALTER COLUMN R_CaseTypeId uniqueidentifier NULL
ALTER TABLE [case].AccessRule ALTER COLUMN R_CheckpointTypeId uniqueidentifier NULL
--Initialise Access level and fix data
UPDATE [case].AccessRule SET [R_CaseTypeId] = NULL, [AccessLevel] = 1

```


## [7.28.3] - 2024-08-30
### Added
- Added `GridColumnConfig` property to CaseType, you can change your `lib-list-view` to display custom columns.

#### More specifically:
You can edit a case type from the UI and put a json formatted string like the one below
Where `title` is the property name of the "cases" object in your translation file (for example: el.json)
Where `itemProperty` is the data location relative to the item in your `items` object returned from `GetCases`
Meaning `checkpointType.code` will fetch => `items[i].checkpointType.code`

```json
[
    {
        "title": "checkpointType",
        "itemProperty": "checkpointType.code"
    },
    {
        "title": "TaxId",
        "itemProperty": "metadata.TaxId"
    }
]
```

## [7.28.2] - 2024-08-29
### Added
- Filter AdminCaseTypes to ignore sending case types when `CanCreate` is `null`, preventing the creation of case types that shouldn't be created from admin role in cases-bo.

## [7.28.1] - 2024-08-01
### Added
- `AdminAddComment` to AdminCasesController, so you post comments to a case.

## [7.25.1] - 2024-07-22
### Added
- `PatchCaseMetadata` to AdminCasesController, so you can now update a case's metadata.
- `PatchCaseMetadata` to the IAdminCaseService, so you can now update a case's metadata from your code.

## [7.23.2] - 2024-06-05
### Added
- `IsMenuItem` property to CaseType, you can now have all your cases displayed in a separate category as a menu item based on their case type
- `GridFilterConfig` property to CaseType, you can add a case type specific filter to your searchOptions dropdown.

> For example, you can edit a case type from the UI and put a `SearchOption` json formatted string like so:
```
[
    {
        "field": "RequestId",
        "name": "REQUEST ID",
        "dataType": "string"
    }
]
```

### Migrations
Add 3 more columns to `CaseType` table.
```sql
ALTER TABLE [case].[CaseType]
    ADD IsMenuItem bit NOT NULL DEFAULT 0,
    GridFilterConfig nvarchar(MAX) NULL,
    GridColumnConfig nvarchar(MAX) NULL;
```

## [7.23.0] - 2024-06-03
### Changed
- Allow `SystemClient` users to `GetCaseActions`.

## [7.22.1] - 2024-04-23
### Added
- `Data` & `IncludeData` filter property to `GetCasesListFilter` for `IAdminCaseService.GetCases` method.

## [7.21.1] - 2024-03-14
### Bugfix
- `HttpEndpointWithValidation` : Ignore validation if JSON Schema is null. Null check HTTP Body if there is a Schema to validate it with.

## [7.21.0] - 2024-03-13
### Added
- `HttpEndpointWithValidation` Activity that validates body with the provided json schema.
### Bugfix
- Ηardcoded reference `<PackageReference Include="MediatR" Version="12.2.0" />` to resolve keyed service exceptions in `net8`.

## [7.18.3] - 2024-01-22
### Added
`PermittedAttachmentFileExtensions` config property in `CasesApiOptions` (base, `AdminCasesApiOptions` or `MyCasesApiOptions`) which specifies the permitted file extensions to check when uploading an attachment to an existing case. Can be configured via `appsettings` level.

#### Default values:
```
".pdf", ".jpeg", ".jpg", ".tif", ".tiff"
```

### Changed
- Replaced hardcoded file extension checks in `UploadAdminCaseAttachment` of `AdminCasesController` to use the new `PermittedAttachmentFileExtensions`.
- **Breaking change:** Added file extension checks in `UploadCaseAttachment` of `MyCasesController` to use the new `PermittedAttachmentFileExtensions`.
- If no `PermittedAttachmentFileExtensions` are provided in project level, the default values will be used.
- In case of failure, `400 Bad Request` will be returned.

#### Action required
If you want to set dedicated file extension rules on your project, make sure you have the following example configuration in the options of your preference (`CasesApiOptions`, `AdminCasesApiOptions` or/and `MyCasesApiOptions`)
### AdminCasesApiOptions Example
```json
{
    "AdminCasesApiOptions": {
      "PermittedAttachmentFileExtensions": [ ".pdf", ".txt" ]
    }
}
```

## [7.18.2] - 2024-01-18
### Bugfix
- Fix startup crashing when case management did not have `Elsa:Server:BaseUrl` and `Elsa:Server:BasePath` app settings configured.

## [7.18.1] - 2024-01-11
### Added
- `AuthenticationBasedHttpEndpointAuthorizationHandler` for Elsa HttpActivities, so the Authorization Policies can work.
Configuration sample with new handler
```json
{
    "Elsa":{
        "Server": {
            "BaseUrl": "https://localhost:2000", // the base url of the CaseApi
            "BasePath": "/external-activities" // the path that the http activities will use by-default 
    }
}
```
### Changed
- Default behaviour for Elsa CleanUp options set to `true`. Check version `7.7.0` for more.

#### Action required
If you want the default retention policy to be ignored, make sure you have the following configuration
```json
{
    "Elsa":{
        "CleanUpOptions": {
            "Enabled": false
        }
    }
}
```

## [Unreleased]
### Added
- `GetCasesListFilter.Data` parameter, to filter admin cases by CaseData.

## [7.14.1] - 2023-12-22
### Bugfix
- GetMyCases now properly paginates when there is CaseData filter.

## [7.7.1] - 2023-11-17
### Added
- `IncludeData` to MyCases endpoint to provide the client with case data. 
### Bugfix
- `SystemUser` now correctly gets the scope as initialized from `AdminCaseApiOptions`

## [7.7.0] - 2023-11-01
### Changed
- Elsa update from `2.11.0` to `2.13.0`
### Added
- Add `AddRetentionServices` feature for Elsa Workflows with app settings configuration.
> To override the default specification for workflow retention selection, implement the `IRetentionSpecificationFilter` and pass it as a parameter to `AddWorkflow`.
> More: https://github.com/elsa-workflows/elsa-core/blob/master/src/modules/retention/Elsa.Retention/Options/CleanupOptions.cs

> Configuration example
```json
{
    "Elsa":{
        "CleanUpOptions": {
            "Enabled": true,
            "BatchSize": 100,
            "TimeToLiveInDays": 30,
            "SweepIntervalInHours": 4
        }
    }
}
```

## [7.4.0] - 2023-08-29
### Added
- Add `int? ReferenceNumber` to `Case` as an optional feature with an auto-incremented sequence if enabled. 
### Migrations
Add `ReferenceNumberSequence` sequence and `ReferenceNumber` column to `Case` table using the following script.

Change the database and the schema according to your configuration.
```sql
-- The script will run against the database we want it to.
USE [Cases]

-- If we search for our sequence and it retunrs 0
-- we create our sequence
IF (SELECT COUNT(*) FROM [sys].[sequences] WHERE [name] = 'ReferenceNumberSequence') = 0
    CREATE SEQUENCE [case].[ReferenceNumberSequence] AS int
        START WITH 1 INCREMENT
        BY 1
        NO MINVALUE
        NO MAXVALUE
        NO CYCLE;
GO

-- If the column length returns NULL it means the column does not exist.
-- so we alter the table and add it.
IF COL_LENGTH('case.Case', 'ReferenceNumber') IS NULL
    ALTER TABLE [case].[Case]
    ADD [ReferenceNumber] int NULL;
GO

```

## [7.3.10] - 2023-08-10
### Bugfix
- MyCase list filter now supports `from` and `to` parameters that can return results for same-day filters.

## [7.3.6] - 2023-07-06
### Changed
- Automatically remove `AssignedTo` when the case moves to Completed status.
### Bugfix
- Completely remove obsolete `CasePartial.Status`. It was generating false values to `GetCaseById` operation.

## [7.3.4] - 2023-07-03
### Added
- "not-equals" and "contains" operators to GetCases list filters.
- `CheckpointType.Status` to `CasePartial`.
### Changed
- General refactoring in case filtering based on role/member.
### Removed
- `CaseStatus` from `CasePartial`. 

## [7.3.2] - 2023-06-29
### Added
- `CheckpointType` Translations
### Migrations
- Integrations at `My Endpoint` that uses `CasePartial` with direct access to `string CheckpointTypeCode` will have to use `CheckpointType` instead.

## [7.2.2] - 2023-06-06
### Bugfix
- While sending a Case Message from `SendMessageActivity` the authorization provider is being successfully called.
- Rename `RoleCaseTypeService` to `MemberAuthorizationService`.

## [7.1.7] - 2023-05-23
### Added
- `GetMyCases` now returns case metadata.
### Changed
- Elsa updates to `2.11.1`.
- JsonSchema.Net to `4.1.1`.
- The `CaseApiOptions` are now separated for each endpoint group. Use `MyCasesApiOptions` or `AdminCasesApiOptions` accordingly.
### Bugfix
- `GroupName` and `ApiPrefix` can be configured properly for each endpoint group (admin or my-cases). Resolves #225.
### Migrations
- at `AddCasesApiEndpoints` Replace `CasesApiOptions` with `MyCasesApiOptions`.
- at `AddAdminCasesApiEndpoints` Replace `CasesApiOptions` with `AdminCasesApiOptions`.

## [7.1.6] - 2023-05-09
### Changed
- `IncludeDrafts` option to return draft cases.

## [7.1.1] - 2023-04-19
### Added
- `GroupName` option to configure swagger client.
- `FindSubjectIdOrClientId` logic to handle "my" cases when initialized from proxy apis where subject does not exist.

## [7.0.5] - 2023-03-30
### Added
- `checkpointsThatAllowDownload` as a case type Config option
### Changed
- `CaseDownloadedEvent` signature

## [6.11.3] - 2023-03-17
### Fixed
- The MyGetCases function correctly returns the translated rejection reason instead of the text

## [6.11.2] - 2023-03-15
### Changed
- Entity `CaseType.Config` now has `MAX` length 

### Migrations
```sql
ALTER TABLE [case].[Casetype] 
    ALTER COLUMN [Config] NVARCHAR(MAX) NULL;
 ```
##  [6.10.6] - 2023-03-17
### Fixed
- The GetCases function correctly returns the translated rejection reason instead of the text.

##  [6.10.5] - 2023-03-02
### Fixed
- Admin Report service bug.

##  [6.10.4] - 2023-03-02
### Changed
- Case type form for create/update is simplified. Only case type entity is being edited.

## [6.10.3] - 2023-03-01
### Added
- `PdfOptions` as a case type Config option

## [6.10.1] - 2023-02-15
### Added
- Reports controller to handle UI dashboard stats

## [6.9.1] - 2023-02-07
### Changed
- Notification Subscriptions are now per case type
### Fixed
- Case Type Translation bug for admin users
### Migrations
```sql
ALTER TABLE [case].[NotificationSubscription]
    ADD [CaseTypeId] UNIQUEIDENTIFIER NOT NULL;
```
Every existing record in [case].[NotificationSubscription] should be replaced with x new ones, where x is the number of case types that the record's subscriber can see!

## [6.7.1] - 2023-02-03
### Changed
- Entity `CaseType.Description` now has a `256` length 

### Migrations
```sql
ALTER TABLE [case].[Casetype] 
    ALTER COLUMN [Description] NVARCHAR(256) NULL;
 ```

## [6.6.1] - 2023-01-31
### Changed
- `CaseDetailsActivity` returns data as object instead of json string. You need to change all activities where case data is json parsed, for example
  ```js
  // Change from this
  var caseData = JSON.parse(activities.GetCaseDetails.Output().Data);
  // to this
  var caseData = activities.GetCaseDetails.Output().Data;
  ```

## [6.6.0] - 2023-01-30
- Jumped to 6.6.0 to match other Indice Packages

## [6.5.0] - 2023-01-24
Major refactor - a lot of breaking changes!
### Added
- `Checkpoint` logic for BO users. This is a performance optimization
- `DataId` and `PublicDataId` logic for admin and my-case cases.
- New workflow activity `AssignCaseToUserActivity`
- Typed predefined workflow outcome names (eg `"Failed"`)
- Two properties at `CasePartial` model, `CreatedByEmail` and `CreatedByName`
### Changed
- All `string` caseData request are now `dynamic` and removed the need for json parse/stringify to the clients
- Entity `RoleCaseType` to `Member`
- Entity `CaseTypeCategory` to `Category`
- Entity `CaseTypeNotificationSubscription` to `NotificationSubscription`
- Naming for DbModels & Dtos (eg `CaseDetails` -> `Case`) 
- Signature for `IAdminCaseService.AssignCase`
### Removed
- Support for net3.1

### Migrations 
Update checkpoint fk with the most recent checkpoint id
```sql
UPDATE c
SET CheckpointId = B.Id
FROM [case].[Case] c
INNER JOIN (
    SELECT *
    FROM (
        SELECT Id, CaseId, ROW_NUMBER() OVER (PARTITION BY [CaseId] ORDER BY CreatedByWhen DESC) AS Rn
        FROM [case].[Checkpoint]
    ) A
    WHERE A.Rn = 1 
) AS B 
    ON c.Id = B.CaseId
```

Rename table / IX / PK / FK
```sql
-- RoleCaseType to Member
exec sp_rename N'[case].RoleCaseType', N'Member'
exec sp_rename N'[case].[Member].IX_RoleCaseType_CaseTypeId', N'IX_Member_CaseTypeId', N'INDEX';  
exec sp_rename N'[case].[Member].IX_RoleCaseType_CheckpointTypeId', N'IX_Member_CheckpointTypeId', N'INDEX';
exec sp_rename N'[case].[Member].PK_RoleCaseType', N'PK_Member', N'INDEX';
exec sp_rename N'[case].FK_RoleCaseType_CaseType_CaseTypeId', N'FK_Member_CaseType_CaseTypeId'
exec sp_rename N'[case].FK_RoleCaseType_CheckpointType_CheckpointTypeId', N'FK_Member_CheckpointType_CheckpointTypeId';

-- CaseTypeCategory to Category
exec sp_rename N'[case].CaseTypeCategory', N'Category'
exec sp_rename N'[case].[Category].PK_CaseTypeCategory', N'PK_Category', N'INDEX';  

-- CaseTypeNotificationSubscription to NotificationSubscription
exec sp_rename N'[case].CaseTypeNotificationSubscription', N'NotificationSubscription'
exec sp_rename N'[case].[NotificationSubscription].IX_CaseTypeNotificationSubscription_Email', N'IX_NotificationSubscription_Email', N'INDEX';  
exec sp_rename N'[case].[NotificationSubscription].PK_CaseTypeNotificationSubscription', N'PK_NotificationSubscription', N'INDEX';  
```

Update dataId and publicDataId FK with most recent versions, for each case
```sql
UPDATE c
SET DataId = [Data].Id, -- most recent data version
    PublicDataId =  [PublicData].Id -- most recet customer data version
FROM [case].[Case] c
LEFT JOIN (
    SELECT *
    FROM (
        SELECT Id, CaseId, ROW_NUMBER() OVER (PARTITION BY [CaseId] ORDER BY CreatedByWhen DESC) AS Rn
        FROM [case].[CaseData]
    ) A
    WHERE A.Rn = 1 
) AS [Data] 
    ON c.Id = [Data].CaseId
LEFT JOIN (
    SELECT *
    FROM (
        SELECT Id, CaseId, CreatedbyId, ROW_NUMBER() OVER (PARTITION BY [CaseId], [CreatedById] ORDER BY CreatedByWhen DESC) AS Rn
        FROM [case].[CaseData]		
    ) A	
) AS [PublicData] 
    ON c.Id = [PublicData].CaseId 
        AND [PublicData].CreatedById = c.CustomerUserId 
        AND [PublicData].Rn = 1
```

Elsa migrations
```sql
UPDATE [Elsa].[WorkflowInstances]
SET [data] = REPLACE([data], N'Indice.Features.Cases.Models.Responses.CaseDetails', N'Indice.Features.Cases.Models.Responses.Case')
WHERE PATINDEX(N'%Indice.Features.Cases.Models.Responses.CaseDetails%', [data]) > 0
```

## [6.4.1] - 2023-01-10
### Fixed
- Properly throw exceptions at `StartWorkflowHandler`
### Changed 
- The GetLookup action method: searchValues parameter is now options

## [6.4.0] - 2022-12-22
Jumped to 6.4.0 to match other Indice Packages
### Added
- Support for net7

## [6.2.0] - 2022-12-13
### Changed 
- `CasePublicStatus` changed to `CaseStatus`. We are not using the word `Public` anymore. The checkpoint types have the flag `Public` for this reason.
### Removed 
- `Rejected` option from public status. The correct way for `Rejected` is a checkpoint type with `Status = Completed` and `Code = Rejected`.
- The logic for checkpoint type code `caseTypeCode:Name` (eg "LoanApplication:Rejected). Now we're using just the "Rejected".
### Migrations
```sql
UPDATE [case].[CheckpointType]
SET Code = RIGHT(Code, LEN(Code) - CHARINDEX(':', Code))
```
```sql
sp_rename 'case.CheckpointType.PublicStatus', 'Status', 'column'
```

## [6.1.0] - 2022-12-09
### Added
- New entity `DbCaseTypeCategory`
- New navigation property `CaseType.Category`
- Property `Order` to `CaseType`
### Migration
```sql
CREATE TABLE [case].[Category](
    [Id] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](128) NULL,
    [Description] [nvarchar](512) NULL,
    [Order] [int] NULL,
    [Translations] [nvarchar](max) NULL,
 CONSTRAINT [PK_Category] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
```

### Removed 
- Property `Category` from `CaseType` entity
