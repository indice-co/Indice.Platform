# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [6.4.0] - 2022-12-22
User Device refactor - database breaking changes!
### Changed
- Added extra columns on UserDevice table

### Migrations 
Update auth.UserDevice with the new columns
```sql
ALTER TABLE [auth].[UserDevice] 
ADD [PnsHandle] nvarchar(512) null
    ,[Tags] nvarchar(max) null
    ,[RequiresPassword] bit not null default (0)
    ,[IsTrusted] bit not null default (0)
    ,[TrustActivationDate] datetimeoffset(7) null
    ,[Blocked] bit not null default (0)
    ,[ClientType] int null
    ,[MfaSessionExpirationDate] datetimeoffset(7) null 
```
Update the default value of the ClientType column. For the ClientType enum check the [ClientRequests](./Features/IdentityServerApi/Models/Requests/ClientRequests.cs) class.

```sql
UPDATE [auth].[UserDevice]
SET [ClientType] = 1
```