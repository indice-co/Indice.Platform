# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [7.24.0] - 2024-06-26
### Added
- New column `Path` in `DbMediaFile`
- New column `Path` in `DbMediaFolder`
```sql
TRUNCATE TABLE [media].[MediaFile]
TRUNCATE TABLE [media].[MediaFolder]
GO
ALTER TABLE [media].[MediaFile]
ADD [Path] [nvarchar](1024) NOT NULL
GO

CREATE NONCLUSTERED INDEX [IX_MediaFile_FolderId]
    ON [media].[MediaFile]([FolderId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_MediaFile_Name]
    ON [media].[MediaFile]([Name] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_MediaFile_Path]
    ON [media].[MediaFile]([Path] ASC);
GO
ALTER TABLE [media].[MediaFolder]
ADD [Path] [nvarchar](1024) NOT NULL
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