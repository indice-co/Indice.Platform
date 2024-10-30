# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

