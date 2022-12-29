# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed
- Properly throw exceptions at `StartWorkflowHandler`
### Changed 
- The GetLookup action method: SearchValues parameter is now a model

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
