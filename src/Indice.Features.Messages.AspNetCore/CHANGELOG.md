# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [7.4.4] - 2023-10-05
### Added
- Media Library support for storing files only for .Net7 projects.
Setup :
1. Add the required services with the desired configuration
```cs 
CampaignOptionsBase.UseMediaLibrary(Action<MediaApiOptions>? configureAction = null)
```
2. Add a policy named **Indice.Features.Media.AspNetCore.MediaLibraryApi.Policies.BeMediaLibraryManager**
3. Configure the swagger docs
```cs
options.AddDoc(Indice.Features.Media.AspNetCore.MediaLibraryApi.Scope, "Media Library API", "API for managing media library in the backoffice tool.");
```
4. Configure the swagger UI
```cs
options.SwaggerEndpoint($"/swagger/{Indice.Features.Media.AspNetCore.MediaLibraryApi.Scope}/swagger.json", Indice.Features.Media.AspNetCore.MediaLibraryApi.Scope);
```
5. Map the Media Api Endpoints
```cs
app.MapMediaLibrary();
```
6. Setup Messages UI to enable the Media Library feature
```cs
CampaignUIOptions.EnableMediaLibrary = true
```
7. Create the corresponding media tables
```sql
CREATE TABLE [media].[MediaFolder](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](128) NOT NULL,
	[Description] [nvarchar](512) NULL,
	[ParentId] [uniqueidentifier] NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreatedBy] [nvarchar](128) NOT NULL,
	[CreatedAt] [datetimeoffset](7) NOT NULL,
	[UpdatedBy] [nvarchar](128) NULL,
	[UpdatedAt] [datetimeoffset](7) NULL,
 CONSTRAINT [PK_MediaFolder] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [media].[MediaFolder]  WITH CHECK ADD  CONSTRAINT [FK_MediaFolder_MediaFolder_ParentId] FOREIGN KEY([ParentId])
REFERENCES [media].[MediaFolder] ([Id])
GO

ALTER TABLE [media].[MediaFolder] CHECK CONSTRAINT [FK_MediaFolder_MediaFolder_ParentId]
GO

CREATE TABLE [media].[MediaFile](
	[Id] [uniqueidentifier] NOT NULL,
	[Guid] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](256) NOT NULL,
	[Description] [nvarchar](512) NULL,
	[FileExtension] [nvarchar](8) NOT NULL,
	[ContentType] [nvarchar](256) NOT NULL,
	[ContentLength] [int] NOT NULL,
	[Data] [image] NULL,
	[FolderId] [uniqueidentifier] NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreatedBy] [nvarchar](128) NOT NULL,
	[CreatedAt] [datetimeoffset](7) NOT NULL,
	[UpdatedBy] [nvarchar](128) NULL,
	[UpdatedAt] [datetimeoffset](7) NULL,
 CONSTRAINT [PK_MediaFile] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [media].[MediaFile]  WITH CHECK ADD  CONSTRAINT [FK_MediaFile_MediaFolder_FolderId] FOREIGN KEY([FolderId])
REFERENCES [media].[MediaFolder] ([Id])
ON DELETE SET NULL
GO

ALTER TABLE [media].[MediaFile] CHECK CONSTRAINT [FK_MediaFile_MediaFolder_FolderId]
GO

CREATE TABLE [media].[MediaSetting](
	[Key] [nvarchar](128) NOT NULL,
	[Value] [nvarchar](max) NULL,
	[CreatedBy] [nvarchar](128) NOT NULL,
	[CreatedAt] [datetimeoffset](7) NOT NULL,
	[UpdatedBy] [nvarchar](128) NULL,
	[UpdatedAt] [datetimeoffset](7) NULL,
 CONSTRAINT [PK_MediaSetting] PRIMARY KEY CLUSTERED 
(
	[Key] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
```

- Rich text editor for Campaign content. Enable/Disable the feature using the following option in CampaignsUIOptions
```cs
CampaignUIOptions.EnableRichTextEditor = true
```

## [7.3.8] - 2023-08-07
### Added
- Inbox controller renamed to MyMessagesController to handle more channels than Inbox. 
- GetMessageById accepts a query parameter with the desired channel.
