# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
Major refactor - a lot of breaking changes!
### Added
- `Checkpoint` logic for BO users. This is a performance optimization
- `DataId` and `PublicDataId` logic for admin and my-case cases.
### Changed
- All `string` caseData request are now `dynamic` and removed the need for json parse/stringify to the clients
- Entity `RoleCaseType` to `Member`
- Entity `CaseTypeCategory` to `Category`
- Entity `CaseTypeNotificationSubscription` to `NotificationSubscription`
- Naming for DbModels & Dtos (eg `CaseDetails` -> `Case`) 

### Migrations 
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

```sql
exec sp_rename '[case].RoleCaseType', 'Member'
exec sp_rename '[case].CaseTypeCategory', 'Category'
exec sp_rename '[case].CaseTypeNotificationSubscription', 'NotificationSubscription'
```

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
