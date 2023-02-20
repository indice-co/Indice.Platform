# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- case verification endpoint
- `RequiresQrCode` as a case type Config option
- `BeSystemClient` auth policy

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
### Removed 
- Property `Category` from `CaseType` entity
