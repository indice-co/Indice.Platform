# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [8.1.0] - Unreleased
### Added 
- This release includes an upgrade to **Duende IdentityServer7**. (_This only applies when running on .NET 9.0 or greater._)

### Migrations
1. **Duende Schema**:  To apply the necessary schema changes, run the following SQL script before starting the new version:

   [Duende SQL migration script](https://github.com/indice-co/Indice.Platform/blob/develop/docs/migrate-idsrv-4-to-7.sql)
1. **Identity Schema**:  For aspent core Identity database schema on EFCore 9.0 there are some indexes to apply.
   
   ```sql
   CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex]
       ON [auth].[Role]([NormalizedName] ASC) WHERE ([NormalizedName] IS NOT NULL);
   GO
      
   CREATE NONCLUSTERED INDEX [IX_RoleClaim_RoleId]
       ON [auth].[RoleClaim]([RoleId] ASC);
   GO
   
   CREATE NONCLUSTERED INDEX [EmailIndex]
       ON [auth].[User]([NormalizedEmail] ASC);
   GO
   
   CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex]
       ON [auth].[User]([NormalizedUserName] ASC) WHERE ([NormalizedUserName] IS NOT NULL);
   GO
   
   CREATE NONCLUSTERED INDEX [IX_UserClaim_UserId]
       ON [auth].[UserClaim]([UserId] ASC);
   GO
   
   CREATE NONCLUSTERED INDEX [IX_UserDevice_DeviceId]
       ON [auth].[UserDevice]([DeviceId] ASC);
   GO
   
   CREATE NONCLUSTERED INDEX [IX_UserDevice_UserId]
       ON [auth].[UserDevice]([UserId] ASC);
   GO
   
   CREATE NONCLUSTERED INDEX [IX_UserLogin_UserId]
       ON [auth].[UserLogin]([UserId] ASC);
   GO
   
   CREATE NONCLUSTERED INDEX [IX_UserPassword_UserId]
       ON [auth].[UserPassword]([UserId] ASC);
   GO
   
   CREATE NONCLUSTERED INDEX [IX_UserRole_RoleId]
       ON [auth].[UserRole]([RoleId] ASC);
   GO

   ```

## [8.0.0] - 2025-06-01

### Changed Validation Rule codes 

|Old Code|New Code|
|--|---|
|PasswordContainsUserName|PasswordIdenticalToUserName|
|PasswordHistory | PasswordRecentlyUsed|
|PasswordIsBlacklisted | PasswordIsCommon|
|PasswordContainsNonUnicodeCharacters | PasswordHasNonLatinChars|
|PasswordContainsNotAllowedCharacters | PasswordContainsNotAllowedChars|

## [8.0.0-rc26] - 2025-04-22
### Added 
- Support for confugurable per user Two Factor enforcement policy.
- Removed session state dependency from the library. So unless your app requires session then you must remove `app.UseSession()`.

### Migrations
Add new column in user table.

```sql
ALTER TABLE [auth].[User]
ADD TwoFactorPolicy smallint NULL;
GO
```

## [7.35.0] - 2024-10-30
### Added 
- Support for profile image upload
- New configuration key to manage backing store of images `IdentityOptions:User:StorePictureAsClaim` defaults to `false`. 
  By default pictures go to the new database table under the `[auth]` schema. 
  Only set to true if cannot upgrade the database and must make use of the new feature.

### Migrations
Add new table called `[auth].[UserPicture]` as backing store for profile images..
```sql
CREATE TABLE [auth].[UserPicture] (
    [Id]            UNIQUEIDENTIFIER   NOT NULL,
    [UserId]        NVARCHAR (450)     NOT NULL,
    [PictureKey]    NVARCHAR (64)      NOT NULL,
    [ContentType]   NVARCHAR (256)     NOT NULL,
    [ContentLength] INT                NOT NULL,
    [Data]          VARBINARY (MAX)    NOT NULL,
    [CreatedDate]   DATETIMEOFFSET (7) NOT NULL,
    [LoginProvider] NVARCHAR (MAX)     NULL,
    CONSTRAINT [PK_UserPicture] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserPicture_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [auth].[User] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_UserPicture_PictureKey]
    ON [auth].[UserPicture]([PictureKey] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_UserPicture_UserId]
    ON [auth].[UserPicture]([UserId] ASC);

GO
```
 
## [7.4.0] - 2023-09-19
### Added
- Infrastructure that can detect impossible travel logins

